namespace Open.BlobStorageAdapter;

/// <summary>
/// Represents a single blob in a blob store with operations to manipulate it.
/// </summary>
/// <typeparam name="TKey">The type of key used to identify this blob.</typeparam>
public sealed record Blob<TKey>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="Blob{TKey}"/> class.
	/// </summary>
	/// <param name="key">The key that identifies this blob.</param>
	/// <param name="container">The blob store that contains this blob.</param>
	/// <exception cref="ArgumentNullException">Thrown if key is null.</exception>
	internal Blob(
		TKey key,
		IBlobStore<TKey> container)
	{
		_key = key;
		_container = container;
	}

	private readonly TKey _key;
	private readonly IBlobStore<TKey> _container;

	/// <summary>
	/// Checks if this blob exists in the store.
	/// </summary>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <returns>A task that returns true if the blob exists; otherwise, false.</returns>
	public ValueTask<bool> ExistsAsync(
		CancellationToken cancellationToken = default)
		=> _container.ExistsAsync(_key, cancellationToken);

	/// <summary>
	/// Reads and processes the content of this blob.
	/// </summary>
	/// <typeparam name="T">The type of result returned by the read handler.</typeparam>
	/// <param name="readHandler">A function that processes the blob's content stream and returns a result.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <returns>A task that returns the result of the read handler.</returns>
	public async ValueTask<T> ReadAsync<T>(
		Func<Stream?, ValueTask<T>> readHandler,
		CancellationToken cancellationToken = default)
	{
#if NETSTANDARD2_0
#else
            await
#endif
		using var stream = await _container
			.ReadAsync(_key, cancellationToken)
			.ConfigureAwait(false);

		return await readHandler(stream)
			.ConfigureAwait(false);
	}

	/// <summary>
	/// Writes content to this blob.
	/// </summary>
	/// <param name="writeHandler">A function that writes content to the provided stream.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	public ValueTask WriteAsync(
		Func<Stream, CancellationToken, ValueTask> writeHandler,
		CancellationToken cancellationToken = default)
		=> _container.WriteAsync(_key, writeHandler, cancellationToken);

	/// <summary>
	/// Deletes this blob from the store.
	/// </summary>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <returns>A task that returns true if the blob was deleted; otherwise, false.</returns>
	public ValueTask<bool> DeleteAsync(
		CancellationToken cancellationToken = default)
		=> _container.DeleteAsync(_key, cancellationToken);
}
