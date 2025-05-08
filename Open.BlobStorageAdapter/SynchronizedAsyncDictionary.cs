using Open.Disposable;
using System.Diagnostics;

namespace Open.BlobStorageAdapter;

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

		public void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="SynchronizedAsyncDictionary{TKey, TValue}"/> class
	/// that wraps the specified <see cref="IAsyncDictionary{TKey, TValue}"/> implementation.
	/// </summary>
	/// <param name="innerDictionary">The underlying dictionary to provide synchronized access to.</param>
	/// <param name="semaphorePool">The object pool for recycling SemaphoreSlim instances.</param>
	private SynchronizedAsyncDictionary(
		IAsyncDictionary<TKey, TValue> innerDictionary,
		IObjectPool<Lease>? semaphorePool)
	{
		_innerDictionary = innerDictionary
			?? throw new ArgumentNullException(nameof(innerDictionary));

		_leasePool = semaphorePool
			?? (_ownedPool = InterlockedArrayObjectPool.CreateAutoDisposal(() => new Lease()));
	}

	public SynchronizedAsyncDictionary(
		IAsyncDictionary<TKey, TValue> innerDictionary)
		: this(innerDictionary, null) { }

	/// <inheritdoc />
	public ValueTask<bool> ExistsAsync(TKey key, CancellationToken cancellationToken)
		=> _innerDictionary.ExistsAsync(key, cancellationToken);

	/// <inheritdoc />
	public ValueTask<TryReadResult<TValue>> TryReadAsync(TKey key, CancellationToken cancellationToken)
		=> _innerDictionary.TryReadAsync(key, cancellationToken);

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
		if (_disposeState != 0)
			throw new ObjectDisposedException(nameof(SynchronizedAsyncDictionary<,>));

		cancellationToken.ThrowIfCancellationRequested();

		SemaphoreSlim semaphore;
		Lease? lease;
		// Get or create a synchronization lock for this key
		_dictionaryLock.EnterUpgradeableReadLock();
		try
		{
			if (!_leases.TryGetValue(key, out lease))
			{
				_dictionaryLock.EnterWriteLock();
				try
				{
					// Double check after acquiring write lock
					if (!_leases.TryGetValue(key, out lease))
					{
						// Create a new semaphore entry
						lease = _leasePool.Take();
						_leases.Add(key, lease);
					}
				}
				finally
				{
					_dictionaryLock.ExitWriteLock();
				}
			}

			// Track that we're starting an operation
			Interlocked.Increment(ref lease.ActiveLeaseRequests);
			semaphore = lease.Semaphore;
		}
		finally
		{
			_dictionaryLock.ExitUpgradeableReadLock();
		}

		T result;

		try
		{
			// Wait for exclusive access to the key
			await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

			try
			{
				// Execute the operation with exclusive access
				var entry = _innerDictionary[key];
				result = await operation(entry, cancellationToken).ConfigureAwait(false);
			}
			finally
			{
				// Release the lock
				semaphore.Release();
			}
		}
		finally
		{           // Operation completed, check if we can remove the semaphore
			_dictionaryLock.EnterUpgradeableReadLock();
			try
			{
				if (_leases.TryGetValue(key, out lease))
				{
					// Decrement active operations count
					int remainingOperations = Interlocked.Decrement(ref lease.ActiveLeaseRequests);                    // If this was the last operation and semaphore is available, we can remove it
					if (remainingOperations == 0 && lease.Semaphore.CurrentCount == 1)
					{
						_dictionaryLock.EnterWriteLock();
						try
						{
							// Double check after acquiring write lock
							if (_leases.TryGetValue(key, out lease) &&
								lease.ActiveLeaseRequests == 0 &&
								lease.Semaphore.CurrentCount == 1)
							{
								// Remove the lease and return the semaphore to the pool while holding
								// the write lock to ensure complete thread safety
								_leases.Remove(key);
								_leasePool.Give(lease);
							}
						}
						finally
						{
							_dictionaryLock.ExitWriteLock();
						}
					}
				}
			}
			finally
			{
				_dictionaryLock.ExitUpgradeableReadLock();
			}
		}

		return result;
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
	/// Disposes the synchronization resources used by this dictionary.
	/// </summary>
	/// <param name="disposing">Whether this is being called from the Dispose method.</param>
	protected virtual void Dispose(bool disposing)
	{
		if (Interlocked.CompareExchange(ref _disposeState, 1, 0) != 0)
			return;

		if (disposing)
		{
			using var pool = _ownedPool;
			using var _ = _dictionaryLock;

			// Return all semaphores to the pool
			_dictionaryLock.EnterWriteLock();
			try
			{
				foreach (var lease in _leases.Values)
				{
					// If the semaphore is not release, we have a problem and need to dispose of it to prevent issues.
					int activeCount = lease.ActiveLeaseRequests;
					Debug.Assert(activeCount != 0, "Lease still active before disposal.");

					int currentCount = lease.Semaphore.CurrentCount;
					Debug.Assert(currentCount == 1, "Semaphore not released before disposal.");

					if (activeCount != 0 || currentCount != 0)
					{
						try { lease.Dispose(); }
						catch { }

						continue;
					}

					if (_ownedPool is null) _leasePool.Give(lease);
				}

				_leases.Clear();
			}
			finally
			{
				_dictionaryLock.ExitWriteLock();
			}
		}

		Interlocked.CompareExchange(ref _disposeState, 2, 1);
	}
}