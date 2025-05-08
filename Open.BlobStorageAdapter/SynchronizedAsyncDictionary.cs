using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Open.BlobStorageAdapter;

/// <summary>
/// Provides synchronized access to an underlying <see cref="IAsyncDictionary{TKey, TValue}"/>.
/// This wrapper ensures exclusive leased access to dictionary entries during operations,
/// preventing race conditions in multi-threaded environments.
/// </summary>
/// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
/// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
public class SynchronizedAsyncDictionary<TKey, TValue>
    : ISynchronizedAsyncDictionary<TKey, TValue>
    , IDisposable 
    where TKey : notnull
{
    private readonly IAsyncDictionary<TKey, TValue> _innerDictionary;
    private readonly ConcurrentDictionary<TKey, SemaphoreSlim> _locks = new();
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="SynchronizedAsyncDictionary{TKey, TValue}"/> class
    /// that wraps the specified <see cref="IAsyncDictionary{TKey, TValue}"/> implementation.
    /// </summary>
    /// <param name="innerDictionary">The underlying dictionary to provide synchronized access to.</param>
    public SynchronizedAsyncDictionary(IAsyncDictionary<TKey, TValue> innerDictionary)
    {
        _innerDictionary = innerDictionary ?? throw new ArgumentNullException(nameof(innerDictionary));
    }

    /// <inheritdoc />
    public ValueTask<bool> ExistsAsync(TKey key, CancellationToken cancellationToken)
        => _innerDictionary.ExistsAsync(key, cancellationToken);

    /// <inheritdoc />
    public ValueTask<TryReadResult<TValue>> TryReadAsync(TKey key, CancellationToken cancellationToken)
        => _innerDictionary.TryReadAsync(key, cancellationToken);

    /// <inheritdoc />
    /// <remarks>
    /// This implementation uses a <see cref="SemaphoreSlim"/> to ensure exclusive access to each key.
    /// The semaphore is automatically acquired before the operation and released after it completes,
    /// regardless of whether the operation succeeds or throws an exception. This creates an exclusive 
    /// lease on the entry for the duration of the operation, ensuring thread safety.
    /// </remarks>
    public async ValueTask<T> LeaseAsync<T>(
        TKey key,
        CancellationToken cancellationToken,
        Func<IAsyncDictionaryEntry<TKey, TValue>, CancellationToken, ValueTask<T>> operation)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(SynchronizedAsyncDictionary<TKey, TValue>));

        cancellationToken.ThrowIfCancellationRequested();

        // Get or create a synchronization lock for this key
        var keyLock = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));

        try
        {
            // Wait for exclusive access to the key
            await keyLock.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                // Get the dictionary entry from the inner dictionary and execute the operation with exclusive access
                var entry = _innerDictionary[key];
                return await operation(entry, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                // Release the lock
                keyLock.Release();
            }
        }
        finally
        {
            // If there are no pending operations and the key doesn't exist in the dictionary,
            // we can safely remove the lock from our locks dictionary
            if (keyLock.CurrentCount == 1 && !await _innerDictionary.ExistsAsync(key, CancellationToken.None).ConfigureAwait(false))
            {
                // Try to remove the lock to prevent memory leaks
                _locks.TryRemove(key, out _);
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
    /// Disposes the synchronization resources used by this dictionary.
    /// </summary>
    /// <param name="disposing">Whether this is being called from the Dispose method.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            foreach (var lockItem in _locks.Values)
            {
                lockItem.Dispose();
            }

            _locks.Clear();
        }

        _disposed = true;
    }
}
