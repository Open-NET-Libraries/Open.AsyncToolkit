using Open.BlobStorageAdapter.AsyncItem;

namespace Open.BlobStorageAdapter;

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
}
