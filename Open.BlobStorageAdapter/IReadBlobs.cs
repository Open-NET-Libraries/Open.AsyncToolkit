namespace Open.BlobStorageAdapter;
public interface IReadBlobs<TKey> : IReadAsync<TKey, Stream>
	where TKey : notnull;

/// <summary>
/// Defines operations for retrieving binary data (blobs) using string keys.
/// </summary>
/// <remarks>
/// This is a convenience interface that specifies string as the key type.
/// </remarks>
public interface IReadBlobs
	: IReadBlobs<string>;