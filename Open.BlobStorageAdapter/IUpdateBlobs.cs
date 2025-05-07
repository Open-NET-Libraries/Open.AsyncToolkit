namespace Open.BlobStorageAdapter;

/// <summary>
/// Defines operations for updating binary data (blobs) with a generic key type.
/// </summary>
/// <typeparam name="TKey">
/// The type of key used to identify blobs.
/// </typeparam>
public interface IUpdateBlobs<TKey>
	where TKey : notnull
{
    /// <summary>
    /// Updates an existing blob with the specified key.
    /// </summary>
    /// <param name="key">The key identifying the blob to update.</param>
    /// <param name="writeHandler">A delegate that writes data to the provided stream.</param>
    /// <param name="cancellationToken">An optional token to monitor for cancellation requests.</param>
    /// <returns>
    /// <see langword="true"/> if the blob was updated;
    /// otherwise <see langword="false"/>.
    /// </returns>
	ValueTask<bool> UpdateAsync(
		TKey key,
		Func<Stream, CancellationToken, ValueTask> writeHandler,
		CancellationToken cancellationToken = default);
}

/// <inheritdoc cref="IUpdateBlobs{TKey}"/>
/// <remarks>
/// This is a convenience interface that specifies <see langword="string"/> 
/// as the key type.
/// </remarks>
public interface IUpdateBlobs
	: IUpdateBlobs<string>;