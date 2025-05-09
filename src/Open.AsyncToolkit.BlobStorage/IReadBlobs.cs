namespace Open.AsyncToolkit.BlobStorage;

/// <summary>
/// Defines operations for retrieving binary data (blobs) with a generic key type.
/// </summary>
/// <inheritdoc cref="ICreateBlobs{TKey}"/>
public interface IReadBlobs<TKey> : IReadAsync<TKey, Stream>
	where TKey : notnull;

/// <summary>
/// Defines operations for retrieving binary data (blobs) using string keys.
/// </summary>
/// <remarks>
/// <inheritdoc cref="ICreateBlobs" path="/remarks"/>
/// </remarks>
public interface IReadBlobs
	: IReadBlobs<string>;