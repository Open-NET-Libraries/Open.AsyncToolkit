using Open.BlobStorageAdapter.AsyncItem;
using System.Collections.Concurrent;

namespace Open.BlobStorageAdapter;

/// <summary>
/// Implements an in-memory <see cref="IAsyncDictionary{TKey, TValue}"/> using a <see cref="ConcurrentDictionary{TKey, TValue}"/>
/// as the underlying storage mechanism.
/// </summary>
/// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
/// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
public class MemoryAsyncDictionary<TKey, TValue>
	: ConcurrentDictionary<TKey, TValue>, IAsyncDictionary<TKey, TValue>
	where TKey : notnull
{
	ValueTask<bool> IReadAsync<TKey, TValue>.ExistsAsync(TKey key, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		return new ValueTask<bool>(ContainsKey(key));
	}

	ValueTask<TryReadResult<TValue>> IReadAsync<TKey, TValue>.TryReadAsync(TKey key, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		var result = TryGetValue(key, out var value)
			? TryReadResult<TValue>.Succeeded(value)
			: TryReadResult<TValue>.Failed;

		return new(result);
	}

	IAsyncDictionaryEntry<TKey, TValue> IAsyncDictionary<TKey, TValue>.this[TKey key]
		=> new AsyncDictionaryEntry<TKey, TValue>(key, this);

	ValueTask<bool> ICreateAsync<TKey, TValue>.CreateAsync(TKey key, TValue value, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		return new ValueTask<bool>(TryAdd(key, value));
	}

	ValueTask<bool> ICreateOrUpdate<TKey, TValue>.CreateOrUpdateAsync(TKey key, TValue value, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		this[key] = value;
		return new ValueTask<bool>(true);
	}

	ValueTask<bool> IDeleteAsync<TKey>.DeleteAsync(TKey key, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		return new ValueTask<bool>(TryRemove(key, out _));
	}
}
