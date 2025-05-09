namespace Open.AsyncToolkit.KeyValue;

/// <summary>
/// Provides synchronized access to an underlying <see cref="IAsyncDictionary{TKey, TValue}"/>.
/// This wrapper ensures exclusive leased access to dictionary entries during operations,
/// preventing race conditions in multi-threaded environments.
/// </summary>
/// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
/// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
public class SynchronizedAsyncDictionary<TKey, TValue> : ISynchronizedAsyncDictionary<TKey, TValue>
	, IDisposable
	where TKey : notnull
{
	private readonly IAsyncDictionary<TKey, TValue> _innerDictionary;
	// Main lock for dictionary access
	private readonly ReaderWriterLockSlim _dictionaryLock = new(LockRecursionPolicy.NoRecursion);

	// Dictionary of semaphores - managed with _dictionaryLock
	private readonly Dictionary<TKey, Lease> _leases = [];
	private readonly InterlockedArrayObjectPool<Lease>? _ownedPool;
	private readonly IObjectPool<Lease> _leasePool;
	private int _disposeState;

	// Track active operations for each key
	private sealed class Lease() : IDisposable
	{
		public SemaphoreSlim Semaphore = new(1, 1);
		public int ActiveLeaseRequests = 0;
		private bool disposedValue;

		private void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					Semaphore.Dispose();
				}

				disposedValue = true;
			}
		}

		public Lease Reserve()
		{
			Interlocked.Increment(ref ActiveLeaseRequests);
			return this;
		}

		public int Decrement()
			=> Interlocked.Decrement(ref ActiveLeaseRequests);

		public void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
	}

	private SynchronizedAsyncDictionary(
		IAsyncDictionary<TKey, TValue> innerDictionary,
		IObjectPool<Lease>? semaphorePool) // This could later facilitate a shared pool.
	{
		_innerDictionary = innerDictionary
			?? throw new ArgumentNullException(nameof(innerDictionary));

		_leasePool = semaphorePool
			?? (_ownedPool = InterlockedArrayObjectPool.CreateAutoDisposal(static () => new Lease()));
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="SynchronizedAsyncDictionary{TKey, TValue}"/> class
	/// that wraps the specified <see cref="IAsyncDictionary{TKey, TValue}"/> implementation.
	/// </summary>
	/// <param name="innerDictionary">The underlying dictionary to provide synchronized access to.</param>
	public SynchronizedAsyncDictionary(
		IAsyncDictionary<TKey, TValue> innerDictionary)
		: this(innerDictionary, null) { }

	private void AssertAlive()
#if NET9_0_OR_GREATER
		=> ObjectDisposedException.ThrowIf(_disposeState != 0, this);
#else
	{
		if (_disposeState != 0)
			throw new ObjectDisposedException(GetType().ToString());
	}
#endif

	/// <inheritdoc />
	public ValueTask<bool> ExistsAsync(TKey key, CancellationToken cancellationToken)
	{
		AssertAlive();
		return _innerDictionary.ExistsAsync(key, cancellationToken);
	}

	/// <inheritdoc />
	public ValueTask<TryReadResult<TValue>> TryReadAsync(TKey key, CancellationToken cancellationToken)
	{
		AssertAlive();
		return _innerDictionary.TryReadAsync(key, cancellationToken);
	}

	/// <remarks>
	/// This implementation uses a <see cref="SemaphoreSlim"/> to ensure exclusive access to each key.
	/// The semaphore is automatically acquired before the operation and released after it completes,
	/// regardless of whether the operation succeeds or throws an exception. This creates an exclusive 
	/// lease on the entry for the duration of the operation, ensuring thread safety.
	/// 
	/// When all operations on a key are completed, the semaphore is returned to the pool for reuse,
	/// which prevents unbounded growth of semaphores in the dictionary.
	/// </remarks>
	/// <inheritdoc />
	public async ValueTask<T> LeaseAsync<T>(
		TKey key,
		CancellationToken cancellationToken,
		Func<IAsyncDictionaryEntry<TKey, TValue>, CancellationToken, ValueTask<T>> operation)
	{
		AssertAlive();
		cancellationToken.ThrowIfCancellationRequested();

		Lease lease = QueueForLease(key);
		var semaphore = lease.Semaphore;

		// At this point we own a pending lease and need to be sure to decrement it when we're done.
		try
		{
			// Wait for exclusive access to the key
			await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

			try
			{
				// Execute the operation with exclusive access
				var entry = _innerDictionary[key];
				return await operation(entry, cancellationToken).ConfigureAwait(false);
			}
			finally
			{
				// Release the lock
				semaphore.Release();
			}
		}
		finally
		{
			Cleanup();
		}

		Lease QueueForLease(TKey key)
		{
			Lease? lease;
			using (var readLock = _dictionaryLock.ReadLock())
			{
				if (_leases.TryGetValue(key, out lease))
					return lease.Reserve();
			}

			// Get or create a synchronization lock for this key
			using var upgradableRead = _dictionaryLock.UpgradableReadLock();

			if (_leases.TryGetValue(key, out lease))
				return lease.Reserve();

			// Lease doesn't exist yet. Let's get a new one.
			using var writeLock = _dictionaryLock.WriteLock();

			// At this point, because of how upgradeable locks work, there should not be an existing lease.
			Debug.Assert(!_leases.ContainsKey(key));

			// Create a new semaphore entry
			lease = _leasePool.Take();

			// Preemptively reserve before adding.
			_leases.Add(key, lease.Reserve());
			return lease;
		}

		void Cleanup()
		{
			// Next step, at this point it is safe to decrement the active operations count.
			// If we are not the last operation, so we can return now.
			if (lease.Decrement() != 0) return;

			// After this, another thread may end up being responsible for cleaning up the semaphore.
			// So we get a read lock and check if we are still the lease in the dictionary and if our count is not zero, we're done.
			// If we are not the lease, we need to check if we can remove the semaphore.

			using (var readLock = _dictionaryLock.ReadLock())
			{
				if (!LeaseIsReadyForRemoval())
				{
					return;
				}
			}

			// Get or create a synchronization lock for this key
			using var upgradableRead = _dictionaryLock.UpgradableReadLock();
			if (!LeaseIsReadyForRemoval())
			{
				return;
			}

			// At this point we are sure there are no more active operations on this lease and it can be removed.
			Debug.Assert(lease.Semaphore.CurrentCount == 1, "The semaphore must not be in use.");

			using var writeLock = _dictionaryLock.WriteLock();
			// Remove the lease and return the semaphore to the pool while holding
			// the write lock to ensure complete thread safety
			bool removed = _leases.Remove(key);
			Debug.Assert(removed, "Lease should be removed from the dictionary.");
			_leasePool.Give(lease);

			bool LeaseIsReadyForRemoval()
			{
				// If there is no lease kept, we are not the active lease, or the count is not zero, we can return now.
				return _leases.TryGetValue(key, out Lease? activeLease)
					&& lease == activeLease
					&& lease.ActiveLeaseRequests == 0;
			}
		}
	}

	/// <summary>
	/// Disposes the synchronization resources used by this dictionary.
	/// </summary>
	/// <remarks>
	/// Releases all semaphores used for synchronizing access to dictionary entries.
	/// This does not dispose the underlying dictionary.
	/// </remarks>
	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	/// <summary>
	/// Handler for when the <see cref="Dispose()" /> method is invoked manually.
	/// </summary>
	protected virtual void OnDisposing()
	{
		using var pool = _ownedPool;
		using var _ = _dictionaryLock;
		using var writeLock = _dictionaryLock.WriteLock();

		foreach (var lease in _leases.Values)
		{
			// If the semaphore is not release, we have a problem and need to dispose of it to prevent issues.
			int activeCount = lease.ActiveLeaseRequests;
			Debug.Assert(activeCount != 0, "Lease still active before disposal.");

			int currentCount = lease.Semaphore.CurrentCount;
			Debug.Assert(currentCount == 1, "Semaphore not released before disposal.");

			if (activeCount != 0 || currentCount != 1)
			{
				try { lease.Dispose(); }
				catch { }

				continue;
			}

			if (_ownedPool is null) _leasePool.Give(lease);
		}

		_leases.Clear();
	}

	/// <summary>
	/// Disposes the synchronization resources used by this dictionary.
	/// </summary>
	/// <param name="disposing">Whether this is being called from the Dispose method.</param>
	protected void Dispose(bool disposing)
	{
		if (Interlocked.CompareExchange(ref _disposeState, 1, 0) != 0)
			return;

		if (disposing) OnDisposing();

		Interlocked.CompareExchange(ref _disposeState, 2, 1);
	}
}
