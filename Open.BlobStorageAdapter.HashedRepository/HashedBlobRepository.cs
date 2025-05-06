namespace Open.BlobStorageAdapter;

/// <summary>
/// An opinionated means of storing blobs in a blob store
/// that ensures any written blobs are hashed to prevent duplicates.
/// Any matching blobs stored will result in the same ID returned.
/// </summary>
public class HashedBlobRepository(
	IBlobRepo<Guid> blobStore,
	IAsyncDictionary<string, IReadOnlySet<Guid>> hashMap,
	IHashProvider hashProvider)
	: IIdempotentRepository<Guid>
{
	public async ValueTask<Stream> Get(
		Guid key, CancellationToken cancellationToken = default)
		=> await blobStore.ReadAsync(key, cancellationToken).ConfigureAwait(false)
		?? throw new KeyNotFoundException($"Key [{key}] not found.");

	public ValueTask<Guid> Put(
		ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
		=> hashMap.Lease(
			hashProvider.ComputeHash(data.Span),
			HandlePutEntry,
			cancellationToken);

	private static async ValueTask<Guid> HandlePutEntry(
		AsyncDictionaryEntry<string, IReadOnlySet<Guid>> entry,
		CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		if (entry.Exists)
		{
			return operation(entry, cancellationToken);
		}
		return default;
	}
}
