namespace Open.BlobStorageAdapter;

/// <summary>
/// An opinionated means of storing blobs in a blob store that ensures any written blobs are hashed
/// to prevent duplicates.  Any matching blobs stored will result in the same ID returned.
/// </summary>
/// <param name="blobStore"></param>
public class HashedBlobRepository(
	IBlobRepo<Guid> blobStore,
	IBlobStore<string> hashMap,
	IHashProvider hashProvider)
{

}
