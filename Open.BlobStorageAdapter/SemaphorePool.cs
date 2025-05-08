using System;
using System.Threading;
using System.Threading.Tasks;

namespace Open.BlobStorageAdapter;

/// <summary>
/// Provides a pool of reusable SemaphoreSlim objects.
/// </summary>
internal sealed class SemaphorePool : IDisposable
{
    private readonly SemaphoreSlim[] _pool;
    private readonly int _maxPoolSize;
    private int _nextAvailable;
    private readonly SemaphoreSlim _poolLock = new(1, 1);
    private bool _isDisposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="SemaphorePool"/> class.
    /// </summary>
    /// <param name="poolSize">The maximum number of semaphores to keep in the pool.</param>
    public SemaphorePool(int poolSize = 32)
    {
        if (poolSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(poolSize), "Pool size must be greater than zero.");

        _maxPoolSize = poolSize;
        _pool = new SemaphoreSlim[poolSize];
        _nextAvailable = 0;

        // Pre-initialize the pool with semaphores
        for (int i = 0; i < poolSize; i++)
        {
            _pool[i] = new SemaphoreSlim(1, 1);
        }
    }

    /// <summary>
    /// Gets a semaphore from the pool or creates a new one if the pool is empty.
    /// </summary>
    /// <returns>A <see cref="SemaphoreSlim"/> instance.</returns>
    public async Task<SemaphoreSlim> RentAsync(CancellationToken cancellationToken = default)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(SemaphorePool));

        await _poolLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_nextAvailable > 0)
            {
                _nextAvailable--;
                return _pool[_nextAvailable];
            }
            
            // If pool is exhausted, create a new semaphore (not added to pool)
            return new SemaphoreSlim(1, 1);
        }
        finally
        {
            _poolLock.Release();
        }
    }

    /// <summary>
    /// Returns a semaphore to the pool if space is available, otherwise disposes it.
    /// </summary>
    /// <param name="semaphore">The semaphore to return to the pool.</param>
    public async Task ReturnAsync(SemaphoreSlim semaphore, CancellationToken cancellationToken = default)
    {
        if (semaphore == null)
            throw new ArgumentNullException(nameof(semaphore));

        if (_isDisposed)
        {
            semaphore.Dispose();
            return;
        }

        // Ensure the semaphore is in a released state
        if (semaphore.CurrentCount == 0)
        {
            semaphore.Release();
        }

        await _poolLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_nextAvailable < _maxPoolSize)
            {
                _pool[_nextAvailable] = semaphore;
                _nextAvailable++;
            }
            else
            {
                // If the pool is full, dispose the semaphore
                semaphore.Dispose();
            }
        }
        finally
        {
            _poolLock.Release();
        }
    }

    /// <summary>
    /// Disposes all semaphores in the pool.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;

        for (int i = 0; i < _nextAvailable; i++)
        {
            _pool[i].Dispose();
        }

        _poolLock.Dispose();
    }
}
