namespace Open.AsyncToolkit.BlobStorage;

/// <summary>
/// Defines operations for deleting binary data (blobs) using string keys.
/// </summary>
/// <remarks>
/// <inheritdoc cref="ICreateBlobs" path="/remarks"/>
/// </remarks>
public interface IDeleteBlobs
	: IDeleteAsync<string>;
