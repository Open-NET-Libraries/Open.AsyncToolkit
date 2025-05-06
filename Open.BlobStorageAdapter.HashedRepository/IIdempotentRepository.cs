namespace Open.BlobStorageAdapter;

public interface IIdempotentRepository<TKey>
	where TKey : notnull
{
	ValueTask<Stream> Get(TKey key, CancellationToken cancellationToken = default);

	ValueTask<TKey> Put(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default);
}
