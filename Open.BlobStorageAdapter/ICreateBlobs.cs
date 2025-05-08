namespace Open.BlobStorageAdapter;

/// <summary>
/// Defines operations for storing binary data (blobs) 
/// with a generic key type.
/// </summary>
/// <typeparam name="TKey">
/// The type of key used to identify blobs.
/// </typeparam>
public interface ICreateBlobs<TKey>
	where TKey : notnull
{
	/// <summary>
	/// Creates a new blob with the specified key if it does not already exist.
	/// </summary>
	/// <param name="key">The key identifying the blob.</param>
	/// <param name="writeHandler">A delegate that writes data to the provided stream.</param>
	/// <param name="cancellationToken">An optional token to monitor for cancellation requests.</param>
	/// <returns>
	/// <see langword="true"/> if the blob was created;
	/// otherwise <see langword="false"/>.
	/// </returns>
	ValueTask<bool> CreateAsync(
		TKey key,
		Func<Stream, CancellationToken, ValueTask> writeHandler,
		CancellationToken cancellationToken = default);
}

/// <inheritdoc cref="ICreateBlobs{TKey}"/>
/// <remarks>
/// This is a convenience interface that specifies <see langword="string"/> 
/// as the key type.
/// </remarks>
public interface ICreateBlobs
	: ICreateBlobs<string>;