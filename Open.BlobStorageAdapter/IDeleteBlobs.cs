namespace Open.BlobStorageAdapter;

/// <summary>
/// Defines operations for deleting binary data (blobs) with a generic key type.
/// </summary>
public interface IDeleteBlobs<TKey>
	where TKey : notnull
{
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
/// Defines operations for deleting binary data (blobs) using string keys.
/// </summary>
/// <remarks>
/// This is a convenience interface that specifies string as the key type.
/// </remarks>
public interface IDeleteBlobs
	: IDeleteBlobs<string>;
