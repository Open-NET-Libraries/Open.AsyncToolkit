namespace Open.BlobStorageAdapter;

/// <summary>
/// Provides extension methods for blob store operations.
/// </summary>
public static class BlobStoreExtensions
{
	/// <summary>
	/// Gets a blob object that encapsulates operations on a specific blob in the store.
	/// </summary>
	/// <typeparam name="TKey">The type of key used to identify blobs in the store.</typeparam>
	/// <param name="blobStore">The blob store containing the blob.</param>
	/// <param name="key">The key identifying the blob.</param>
	/// <returns>A new <see cref="Blob{TKey}"/> instance for the specified key.</returns>
	/// <exception cref="ArgumentNullException">Thrown if key is null.</exception>
	public static Blob<TKey> GetBlob<TKey>(
		this IBlobStore<TKey> blobStore, TKey key)
		=> new(key ?? throw new ArgumentNullException(nameof(key)), blobStore);
}