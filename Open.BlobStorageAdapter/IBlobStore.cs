namespace Open.BlobStorageAdapter;

public interface IBlobStore<TKey>
{
	ValueTask<bool> ExistsAsync(
		TKey key,
		CancellationToken cancellationToken = default);

	ValueTask<Stream?> ReadAsync(
		TKey key,
		CancellationToken cancellationToken = default);

	ValueTask WriteAsync(
		TKey key,
		Func<Stream, CancellationToken, ValueTask> writeHandler,
		CancellationToken cancellationToken = default);

	ValueTask<bool> DeleteAsync(
		TKey key,
		CancellationToken cancellationToken = default);
}

public interface IBlobStore : IBlobStore<string>;
