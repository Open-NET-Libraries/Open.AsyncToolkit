using Open.BlobStorageAdapter.AsyncItem;

namespace Open.BlobStorageAdapter;

/// <summary>
/// Defines operations for deleting binary data (blobs) using string keys.
/// </summary>
/// <remarks>
/// This is a convenience interface that specifies <see langword="string"/> as the key type.
/// </remarks>
public interface IDeleteBlobs
	: IDeleteAsync<string>;
