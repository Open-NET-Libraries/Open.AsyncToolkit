namespace Open.BlobStorageAdapter.AsyncItem;

public interface ICreateAsync<TKey, TValue>
	where TKey : notnull
{
	/// <summary>
	/// Writes a blob to the store with the specified key if it does not exist.
	/// </summary>
	/// <param name="key">The key identifying the blob.</param>
	/// <param name="writeHandler">A function that writes content to the provided stream.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <returns><see langword="true"/> if the blob was stored; otherwise <see langword="false"/>.</returns>
	ValueTask<bool> CreateAsync(
		TKey key,
		Func<Stream, CancellationToken, ValueTask> writeHandler,
		CancellationToken cancellationToken = default);
}
