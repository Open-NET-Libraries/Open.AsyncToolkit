namespace Open.BlobStorageAdapter;

/// <summary>
/// Defines operations for updating binary data (blobs) with a generic key type.
/// </summary>
/// <inheritdoc cref="ICreateBlobs{TKey}"/>
public interface IUpdateBlobs<TKey>
	where TKey : notnull
{
	/// <summary>
	/// Updates an existing blob with the specified key.
	/// </summary>
	/// <returns>
	/// <see langword="true"/> if the blob was updated;
	/// otherwise <see langword="false"/>.
	/// </returns>
	/// <inheritdoc cref="ICreateBlobs{TKey}.CreateAsync(TKey, Func{Stream, CancellationToken, ValueTask}, CancellationToken)"/>
	ValueTask<bool> UpdateAsync(
		TKey key,
		Func<Stream, CancellationToken, ValueTask> writeHandler,
		CancellationToken cancellationToken = default);
}

/// <inheritdoc cref="IUpdateBlobs{TKey}"/>
/// <remarks>
/// <inheritdoc cref="ICreateBlobs" path="/remarks"/>
/// </remarks>
public interface IUpdateBlobs
	: IUpdateBlobs<string>;