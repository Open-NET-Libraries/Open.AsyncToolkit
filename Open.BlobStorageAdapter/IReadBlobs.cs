namespace Open.BlobStorageAdapter;
public interface IReadBlobs<TKey>
	where TKey : notnull
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
}

/// <summary>
/// Defines operations for retrieving binary data (blobs) using string keys.
/// </summary>
/// <remarks>
/// This is a convenience interface that specifies string as the key type.
/// </remarks>
public interface IReadBlobs
	: IReadBlobs<string>;