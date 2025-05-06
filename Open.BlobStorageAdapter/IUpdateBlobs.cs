namespace Open.BlobStorageAdapter;

/// <summary>
/// Defines operations for storing binary data (blobs) with a generic key type.
/// </summary>
public interface IUpdateBlobs<TKey>
	where TKey : notnull
{
}

/// <summary>
/// Defines operations for storing binary data (blobs) using string keys.
/// </summary>
/// <remarks>
/// This is a convenience interface that specifies string as the key type.
/// </remarks>
public interface IUpdateBlobs
	: IUpdateBlobs<string>;