namespace Open.BlobStorageAdapter;

/// <summary>
/// Defines operations for storing binary data (blobs) with a generic key type.
/// </summary>
public interface IWriteBlobs<TKey>
	where TKey : notnull
{   /// <summary>
	/// Writes a blob to the store with the specified key.
	/// </summary>
	/// <param name="key">The key identifying the blob.</param>
	/// <param name="overwrite">Indicates whether to overwrite an existing blob with the same key.</param>
	/// <param name="writeHandler">A function that writes content to the provided stream.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <returns><see langword="true"/> if the blob was stored; otherwise <see langword="false"/>.</returns>
	ValueTask<bool> WriteAsync(
		TKey key,
		bool overwrite,
		Func<Stream, CancellationToken, ValueTask> writeHandler,
		CancellationToken cancellationToken = default);

}

/// <summary>
/// Defines operations for storing binary data (blobs) using string keys.
/// </summary>
/// <remarks>
/// This is a convenience interface that specifies string as the key type.
/// </remarks>
public interface IWriteBlobs
	: IWriteBlobs<string>;