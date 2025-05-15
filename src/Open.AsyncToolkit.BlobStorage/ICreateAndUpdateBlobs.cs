namespace Open.AsyncToolkit.BlobStorage;

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
	/// <returns>
	/// <see langword="true"/> if the blob was created or updated;
	/// otherwise <see langword="false"/>.
	/// </returns>
	/// <inheritdoc cref="ICreateBlobs{TKey}.CreateAsync(TKey, CancellationToken, Func{Stream, CancellationToken, ValueTask})"/>
	ValueTask<bool> CreateOrUpdateAsync(
		TKey key,
		CancellationToken cancellationToken,
		Func<Stream, CancellationToken, ValueTask> writeHandler);

	/// <summary>
	/// Creates a new blob or updates an existing blob with the specified key.
	/// </summary>
	/// <returns>
	/// <see langword="true"/> if the blob was created or updated;
	/// otherwise <see langword="false"/>.
	/// </returns>
	/// <inheritdoc cref="ICreateBlobs{TKey}.CreateAsync(TKey, ReadOnlyMemory{byte}, CancellationToken)" />
	ValueTask<bool> CreateOrUpdateAsync(
		TKey key,
		ReadOnlyMemory<byte> data,
		CancellationToken cancellationToken = default);
}

/// <inheritdoc cref="ICreateAndUpdateBlobs{TKey}"/>
/// <remarks>
/// <inheritdoc cref="ICreateBlobs" path="/remarks"/>
/// </remarks>
public interface ICreateAndUpdateBlobs
	: ICreateAndUpdateBlobs<string>;