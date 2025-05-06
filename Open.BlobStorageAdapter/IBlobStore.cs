namespace Open.BlobStorageAdapter;

/// <summary>
/// Defines operations for storing and retrieving binary data (blobs) with a generic key type.
/// </summary>
/// <typeparam name="TKey">The type of key used to identify blobs in the store.</typeparam>
public interface IBlobStore<TKey>
{
	/// <summary>
	/// Checks if a blob with the specified key exists in the store.
	/// </summary>
	/// <param name="key">The key identifying the blob.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <returns>A task that returns true if the blob exists; otherwise, false.</returns>
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
	ValueTask<Stream?> ReadAsync(
		TKey key,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Writes a blob to the store with the specified key.
	/// </summary>
	/// <param name="key">The key identifying the blob.</param>
	/// <param name="writeHandler">A function that writes content to the provided stream.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	ValueTask WriteAsync(
		TKey key,
		Func<Stream, CancellationToken, ValueTask> writeHandler,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Deletes a blob with the specified key from the store.
	/// </summary>
	/// <param name="key">The key identifying the blob.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <returns>A task that returns true if the blob was deleted; otherwise, false.</returns>
	ValueTask<bool> DeleteAsync(
		TKey key,
		CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines operations for storing and retrieving binary data (blobs) using string keys.
/// This is a convenience interface that specifies string as the key type.
/// </summary>
public interface IBlobStore : IBlobStore<string>;
