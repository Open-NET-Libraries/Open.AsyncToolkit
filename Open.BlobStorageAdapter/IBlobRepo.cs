namespace Open.BlobStorageAdapter;

/// <summary>
/// Defines operations for storing and retrieving binary data (blobs) with a generic key type.
/// </summary>
/// <typeparam name="TKey">The type of key used to identify blobs in the store.</typeparam>
public interface IBlobRepo<TKey>
	: IReadBlobs<TKey>, ICreateBlobs<TKey>
	where TKey : notnull;

public interface IMutableBlobRepo<TKey>
	: ICreateAndUpdateBlobs<TKey>, IReadBlobs<TKey>
	where TKey : notnull;

/// <summary>
/// Defines operations for storing and retrieving binary data (blobs) using string keys.
/// </summary>
/// <remarks>
/// This is a convenience interface that specifies string as the key type.
/// </remarks>
public interface IBlobRepo
	: IBlobStore<string>, ICreateBlobs, IReadBlobs;
