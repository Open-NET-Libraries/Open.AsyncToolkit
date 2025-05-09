namespace Open.AsyncToolkit.KeyValue;

/// <summary>
/// Defines a dictionary-like interface for asynchronous operations 
/// on key-value pairs.
/// </summary>
/// <typeparam name="TKey">
/// The type of keys in the dictionary.
/// </typeparam>
/// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
public interface IAsyncDictionary<TKey, TValue>
	: IReadAsync<TKey, TValue>,
	  ICreateAsync<TKey, TValue>,
	  ICreateOrUpdate<TKey, TValue>,
	  IDeleteAsync<TKey>
	where TKey : notnull
{
	/// <summary>
	/// Gets a virtual entry in the dictionary for the specified key.
	/// </summary>
	/// <param name="key">The key identifying the entry.</param>
	/// <returns>An <see cref="IAsyncDictionaryEntry{TKey, TValue}"/> representing the entry.</returns>
	/// <remarks>An entry will always be returned, but it does not mean that an actual value exists.</remarks>
	IAsyncDictionaryEntry<TKey, TValue> this[TKey key] { get; }
}
