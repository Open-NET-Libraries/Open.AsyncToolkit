namespace Open.AsyncToolkit.BlobStorage;

/// <summary>
/// Defines operations for storing and retrieving binary data (blobs) 
/// with a generic key type.
/// </summary>
/// <typeparam name="TKey">
/// The type of key used to identify blobs in the store.
/// </typeparam>
public interface IBlobStore<TKey>
	: IBlobRepo<TKey>,
	  IMutableBlobRepo<TKey>,
	  IUpdateBlobs<TKey>,
	  IDeleteAsync<TKey>
	where TKey : notnull;

/// <remarks>
/// This is a convenience interface that specifies <see langword="string"/> 
/// as the key type.
/// </remarks>
/// <inheritdoc cref="IBlobStore{TKey}"/>
public interface IBlobStore
	: IBlobStore<string>,
	  IBlobRepo,
	  ICreateAndUpdateBlobs,
	  IDeleteBlobs;
