namespace Open.BlobStorageAdapter;

public static class BlobStoreExtensions
{
	public static Blob<TKey> GetBlob<TKey>(
		this IBlobStore<TKey> blobStore, TKey key)
		=> new(key ?? throw new ArgumentNullException(nameof(key)), blobStore);
}