namespace Open.BlobStorageAdapter;
public sealed record Blob<TKey>
{
	internal Blob(
		TKey key,
		IBlobStore<TKey> container)
	{
		_key = key;
		_container = container;
	}

	private readonly TKey _key;

	private readonly IBlobStore<TKey> _container;

	public ValueTask<bool> ExistsAsync(
		CancellationToken cancellationToken = default)
		=> _container.ExistsAsync(_key, cancellationToken);

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

	public ValueTask WriteAsync(
		Func<Stream, CancellationToken, ValueTask> writeHandler,
		CancellationToken cancellationToken = default)
		=> _container.WriteAsync(_key, writeHandler, cancellationToken);

	public ValueTask<bool> DeleteAsync(
		CancellationToken cancellationToken = default)
		=> _container.DeleteAsync(_key, cancellationToken);
}
