using Open.BlobStorageAdapter.AsyncItem;

namespace Open.BlobStorageAdapter;

/// <summary>
/// Defines operations for retrieving binary data (blobs) with a generic key type.
/// </summary>
/// <typeparam name="TKey">The type of key used to identify blobs. Must be non-null.</typeparam>
public interface IReadBlobs<TKey> : IReadAsync<TKey, Stream>
	where TKey : notnull;

/// <summary>
/// Defines operations for retrieving binary data (blobs) using string keys.
/// </summary>
/// <remarks>
/// This is a convenience interface that specifies <see langword="string"/> as the key type.
/// </remarks>
public interface IReadBlobs
	: IReadBlobs<string>;