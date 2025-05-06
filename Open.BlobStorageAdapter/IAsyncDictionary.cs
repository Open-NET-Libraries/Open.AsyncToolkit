namespace Open.BlobStorageAdapter;

public interface IAsyncDictionary<TKey, TValue>
	where TKey : notnull
{
	// An indexer that returns an AsyncDictionaryEntry<TKey, TValue>
	AsyncDictionaryEntry<TKey, TValue> this[TKey key] { get; }

	ValueTask<bool> Exists(
		TKey key,
		CancellationToken cancellationToken = default);

	ValueTask AddOrUpdate(
		TKey key,
		Func<TValue?, TValue> valueFactory,
		CancellationToken cancellationToken = default);

	ValueTask<T> Lease<T>(
		TKey key,
		Func<AsyncDictionaryEntry<TKey, TValue>, CancellationToken, ValueTask<T>> operation,
		CancellationToken cancellationToken = default);
}

public record AsyncDictionaryEntry<TKey, TValue>
	where TKey : notnull
{
	private readonly IAsyncDictionary<TKey, TValue> asyncDictionary;

	public AsyncDictionaryEntry(
		TKey key,
		IAsyncDictionary<TKey, TValue> asyncDictionary)
	{
		Key = key;
		this.asyncDictionary = asyncDictionary ?? throw new ArgumentNullException(nameof(asyncDictionary));
	}

	public TKey Key { get; }

	public

}
