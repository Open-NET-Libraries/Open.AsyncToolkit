using Open.BlobStorageAdapter.AsyncItem;

namespace Open.BlobStorageAdapter;

public interface IAsyncDictionary<TKey, TValue>
	: IReadAsync<TKey, TValue>, ICreateAndUpdate<TKey, TValue>, IDeleteAsync<TKey>
	where TKey : notnull
{
#pragma warning disable IDE0079 // Remove unnecessary suppression
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1068:CancellationToken parameters must come last", Justification = "Important for this signature.")]
#pragma warning restore IDE0079 // Remove unnecessary suppression
	ValueTask<T> Lease<T>(
		TKey key,
		CancellationToken cancellationToken,
		Func<AsyncDictionaryEntry<TKey, TValue>, CancellationToken, ValueTask<T>> operation);
}

public static class AsyncDictionaryExtensions
{
	public static ValueTask<TResult> Lease<TKey, TValue, TResult>(
		this IAsyncDictionary<TKey, TValue> asyncDictionary,
		TKey key,
		Func<AsyncDictionaryEntry<TKey, TValue>, ValueTask<TResult>> operation)
		where TKey : notnull
		=> asyncDictionary.Lease(key, CancellationToken.None, (e, _) => operation(e));
}

public record AsyncDictionaryEntry<TKey, TValue>
	where TKey : notnull
{
	private readonly IAsyncDictionary<TKey, TValue> _asyncDictionary;

	public AsyncDictionaryEntry(
		TKey key,
		IAsyncDictionary<TKey, TValue> asyncDictionary)
	{
		Key = key;
		_asyncDictionary = asyncDictionary ?? throw new ArgumentNullException(nameof(asyncDictionary));
	}

	public TKey Key { get; }

	public ValueTask<bool> Exists()
		=> _asyncDictionary.ExistsAsync(Key);

	public ValueTask<bool> Create(TValue value)
		=> _asyncDictionary.CreateAsync(Key, value);

	public ValueTask<TValue?> Read()
		=> _asyncDictionary.ReadAsync(Key);

	public ValueTask<bool> Update(TValue value)
		=> _asyncDictionary.UpdateAsync(Key, value);

	public ValueTask<bool> Delete()
		=> _asyncDictionary.DeleteAsync(Key);

	public ValueTask<bool> CreateOrUpdate(TValue value)
		=> _asyncDictionary.CreateOrUpdateAsync(Key, value);

}
