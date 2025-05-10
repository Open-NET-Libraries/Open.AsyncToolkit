using System.Collections.Concurrent;

namespace Open.AsyncToolkit.BlobStorage;

/// <summary>
/// Implements an in-memory <see cref="IBlobStore{TKey}"/>
/// using a <see cref="ConcurrentDictionary{TKey, TValue}"/>
/// as the underlying storage mechanism.
/// </summary>
/// <typeparam name="TKey">The type of keys used to identify blobs in the store.</typeparam>
public class MemoryBlobStore<TKey>
	: IBlobStore<TKey>
	where TKey : notnull
{
	// We use a byte array because MemoryStreams can read from them directly.
	private readonly ConcurrentDictionary<TKey, byte[]> _store = new();

	/// <inheritdoc />
	public ValueTask<bool> ExistsAsync(TKey key, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		return new ValueTask<bool>(_store.ContainsKey(key));
	}

	/// <inheritdoc />
	public ValueTask<TryReadResult<ReadOnlyMemory<byte>>> TryReadBytesAsync(TKey key, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		return _store.TryGetValue(key, out byte[]? data)
			? new ValueTask<TryReadResult<ReadOnlyMemory<byte>>>(TryReadResult.Success<ReadOnlyMemory<byte>>(data))
			: new ValueTask<TryReadResult<ReadOnlyMemory<byte>>>(TryReadResult.NotFound<ReadOnlyMemory<byte>>());
	}

	/// <inheritdoc />
	public ValueTask<TryReadResult<Stream>> TryReadAsync(TKey key, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		return _store.TryGetValue(key, out byte[]? data)
			? new ValueTask<TryReadResult<Stream>>(TryReadResult.Success<Stream>(new MemoryStream(data, 0, data.Length, false, false)))
			: new ValueTask<TryReadResult<Stream>>(TryReadResult.NotFound<Stream>());
	}

	/// <inheritdoc />
	public ValueTask<bool> DeleteAsync(TKey key, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		return new ValueTask<bool>(_store.TryRemove(key, out _));
	}

	/// <inheritdoc />
	public async ValueTask<bool> CreateAsync(
		TKey key,
		Func<Stream, CancellationToken, ValueTask> writeHandler,
		CancellationToken cancellationToken = default)
	{
		if (writeHandler is null) throw new ArgumentNullException(nameof(writeHandler));
		cancellationToken.ThrowIfCancellationRequested();

		if (_store.ContainsKey(key))
			return false;

		using var ms = new MemoryStream();
		await writeHandler(ms, cancellationToken).ConfigureAwait(false);
		byte[] data = ms.ToArray();
		return _store.TryAdd(key, data);
	}

	/// <inheritdoc />
	public async ValueTask<bool> CreateOrUpdateAsync(
		TKey key,
		Func<Stream, CancellationToken, ValueTask> writeHandler,
		CancellationToken cancellationToken = default)
	{
		if (writeHandler == null) throw new ArgumentNullException(nameof(writeHandler));
		cancellationToken.ThrowIfCancellationRequested();

		using var ms = new MemoryStream();
		await writeHandler(ms, cancellationToken).ConfigureAwait(false);
		byte[] data = ms.ToArray();
		_store[key] = data;
		return true;
	}

	/// <inheritdoc />
	public async ValueTask<bool> UpdateAsync(
		TKey key,
		Func<Stream, CancellationToken, ValueTask> writeHandler,
		CancellationToken cancellationToken = default)
	{
		if (writeHandler == null) throw new ArgumentNullException(nameof(writeHandler));
		cancellationToken.ThrowIfCancellationRequested();

		if (!_store.ContainsKey(key))
			return false;

		using var ms = new MemoryStream();
		await writeHandler(ms, cancellationToken).ConfigureAwait(false);
		byte[] data = ms.ToArray();
		_store[key] = data;
		return true;
	}
}

/// <summary>
/// Implements an in-memory <see cref="IBlobStore"/>
/// using a <see cref="ConcurrentDictionary{TKey, TValue}"/>
/// as the underlying storage mechanism.
/// </summary>
/// <remarks>
/// <inheritdoc cref="ICreateBlobs" path="/remarks"/>
/// </remarks>
public sealed class MemoryBlobStore
	: MemoryBlobStore<string>, IBlobStore;
