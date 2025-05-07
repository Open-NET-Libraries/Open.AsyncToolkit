namespace Open.BlobStorageAdapter;

/// <summary>
/// Defines operations for creating or updating binary data (blobs) 
/// with a generic key type.
/// </summary>
/// <typeparam name="TKey">
/// The type of key used to identify blobs.
/// </typeparam>
public interface ICreateAndUpdateBlobs<TKey>
	where TKey : notnull
{
    /// <summary>
    /// Creates a new blob or updates an existing blob with the specified key.
    /// </summary>
    /// <param name="key">The key identifying the blob.</param>
    /// <param name="writeHandler">A delegate that writes data to the provided stream.</param>
    /// <param name="cancellationToken">An optional token to monitor for cancellation requests.</param>
    /// <returns>
    /// <see langword="true"/> if the blob was created or updated;
    /// otherwise <see langword="false"/>.
    /// </returns>
	ValueTask<bool> CreateOrUpdateAsync(
		TKey key,
		Func<Stream, CancellationToken, ValueTask> writeHandler,
		CancellationToken cancellationToken = default);
}

/// <inheritdoc cref="ICreateAndUpdateBlobs{TKey}"/>
/// <remarks>
/// This is a convenience interface that specifies <see langword="string"/> 
/// as the key type.
/// </remarks>
public interface ICreateAndUpdateBlobs
	: ICreateAndUpdateBlobs<string>;