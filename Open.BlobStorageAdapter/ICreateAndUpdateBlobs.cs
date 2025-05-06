namespace Open.BlobStorageAdapter;
public interface ICreateAndUpdateBlobs<TKey>
	: ICreateBlobs<TKey>, IUpdateBlobs<TKey>
	where TKey : notnull
{
	ValueTask<bool> CreateOrUpdateAsync(
		TKey key,
		Func<Stream, CancellationToken, ValueTask> writeHandler,
		CancellationToken cancellationToken = default);
}

public interface ICreateAndUpdateBlobs
	: ICreateAndUpdateBlobs<string>;