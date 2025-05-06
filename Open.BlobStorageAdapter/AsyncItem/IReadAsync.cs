namespace Open.BlobStorageAdapter;
public interface IReadAsync<TKey, TValue>
{
	/// <summary>
	/// Checks if a entry with the specified key exists.
	/// </summary>
	/// <param name="key">The key identifying the entry.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <returns>A task that returns <see langword="true"/> if the entry exists; otherwise, <see langword="false"/>.</returns>
	ValueTask<bool> ExistsAsync(
		TKey key,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Retrieves a blob from the store with the specified key.
	/// </summary>
	/// <param name="key">The key identifying the blob.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <returns>
	/// A task that returns a stream containing the blob's content, or null if the blob does not exist.
	/// The caller is responsible for disposing the returned stream.
	/// </returns>
	ValueTask<TValue?> ReadAsync(
		TKey key,
		CancellationToken cancellationToken = default);
}