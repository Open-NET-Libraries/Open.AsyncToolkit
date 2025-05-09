namespace Open.AsyncToolkit.KeyValue;

public interface IAsyncDictionaryEntry<TKey, TValue>
{
	/// <summary>
	/// Gets the key for this entry.
	/// </summary>
	TKey Key { get; }

	/// <inheritdoc cref="IReadAsync{TKey, TValue}.ExistsAsync(TKey, CancellationToken)(TKey, CancellationToken)"/>
	ValueTask<bool> Exists(CancellationToken cancellationToken = default);

	/// <inheritdoc cref="ICreateOrUpdate{TKey, TValue}.CreateOrUpdateAsync(TKey, TValue, CancellationToken)"/>
	ValueTask<bool> Create(TValue value, CancellationToken cancellationToken = default);

	/// <inheritdoc cref="ICreateOrUpdate{TKey, TValue}.CreateOrUpdateAsync(TKey, TValue, CancellationToken)"/>
	ValueTask<bool> CreateOrUpdate(TValue value, CancellationToken cancellationToken = default);

	/// <inheritdoc cref="IReadAsync{TKey, TValue}.TryReadAsync(TKey, CancellationToken)"/>
	ValueTask<TryReadResult<TValue>> TryRead(CancellationToken cancellationToken = default);

	/// <inheritdoc cref="IDeleteAsync{TKey}.DeleteAsync(TKey, CancellationToken)"/>
	ValueTask<bool> Delete(CancellationToken cancellationToken = default);
}

public static class AsyncDictionaryEntryExtensions
{
	/// <inheritdoc cref="ReadAsyncExtensions.ReadAsync{TKey, TValue}(IReadAsync{TKey, TValue}, TKey, CancellationToken)"/>
	public static async ValueTask<TValue?> Read<TKey, TValue>(
		this IAsyncDictionaryEntry<TKey, TValue> source,
		CancellationToken cancellationToken = default)
		where TKey : notnull
		where TValue : class
	{
		var result = await source.TryRead(cancellationToken).ConfigureAwait(false);
		return result.Success ? result.Value : null;
	}
}