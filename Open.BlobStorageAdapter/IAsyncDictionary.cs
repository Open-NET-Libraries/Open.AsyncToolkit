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
#pragma warning disable IDE0079 // Remove unnecessary suppression
	[System.Diagnostics.CodeAnalysis.SuppressMessage(
		"Design", "CA1068:CancellationToken parameters must come last",
		Justification = "Important for this signature.")]
#pragma warning restore IDE0079 // Remove unnecessary suppression
    /// <summary>
    /// Leases an entry for the specified key and performs an operation on it.
    /// </summary>
    /// <typeparam name="T">The type of result returned by the operation.</typeparam>
    /// <param name="key">The key identifying the entry to lease.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <param name="operation">The operation to perform on the leased entry.</param>
    /// <returns>
    /// The result of the provided operation.
    /// </returns>
	ValueTask<T> Lease<T>(
		TKey key,
		CancellationToken cancellationToken,
		Func<AsyncDictionaryEntry<TKey, TValue>, CancellationToken, ValueTask<T>> operation);
}

/// <summary>
/// Provides extension methods for <see cref="IAsyncDictionary{TKey, TValue}"/>.
/// </summary>
public static class AsyncDictionaryExtensions
{
    /// <summary>
    /// Leases an entry for the specified key and performs an operation on it 
    /// with default cancellation token.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
    /// <typeparam name="TResult">The type of result returned by the operation.</typeparam>
    /// <param name="asyncDictionary">The async dictionary.</param>
    /// <param name="key">The key identifying the entry to lease.</param>
    /// <param name="operation">The operation to perform on the leased entry.</param>
    /// <returns>
    /// The result of the provided operation.
    /// </returns>
	public static ValueTask<TResult> Lease<TKey, TValue, TResult>(
		this IAsyncDictionary<TKey, TValue> asyncDictionary,
		TKey key,
		Func<AsyncDictionaryEntry<TKey, TValue>, ValueTask<TResult>> operation)
		where TKey : notnull
		=> asyncDictionary.Lease(key, CancellationToken.None, (e, _) => operation(e));
}

/// <summary>
/// Represents an entry in an asynchronous dictionary,
/// providing operations on a specific key.
/// </summary>
/// <typeparam name="TKey">The type of key.</typeparam>
/// <typeparam name="TValue">The type of value.</typeparam>
public record AsyncDictionaryEntry<TKey, TValue>
	where TKey : notnull
{
	private readonly IAsyncDictionary<TKey, TValue> _asyncDictionary;

    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncDictionaryEntry{TKey, TValue}"/> class.
    /// </summary>
    /// <param name="key">The key for this entry.</param>
    /// <param name="asyncDictionary">The dictionary this entry belongs to.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="asyncDictionary"/> is <see langword="null"/>.
    /// </exception>
	public AsyncDictionaryEntry(
		TKey key,
		IAsyncDictionary<TKey, TValue> asyncDictionary)
	{
		Key = key;
		_asyncDictionary = asyncDictionary 
			?? throw new ArgumentNullException(nameof(asyncDictionary));
	}

    /// <summary>
    /// Gets the key for this entry.
    /// </summary>
	public TKey Key { get; }

    /// <summary>
    /// Checks if an entry with this key exists in the dictionary.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if the entry exists;
    /// otherwise <see langword="false"/>.
    /// </returns>
	public ValueTask<bool> Exists()
		=> _asyncDictionary.ExistsAsync(Key);

    /// <summary>
    /// Creates a new entry with this key and the specified value.
    /// </summary>
    /// <param name="value">The value to store.</param>
    /// <returns>
    /// <see langword="true"/> if the entry was created;
    /// otherwise <see langword="false"/>.
    /// </returns>
	public ValueTask<bool> Create(TValue value)
		=> _asyncDictionary.CreateAsync(Key, value);

    /// <summary>
    /// Creates a new entry or updates an existing entry with this key 
    /// and the specified value.
    /// </summary>
    /// <param name="value">The value to store.</param>
    /// <returns>
    /// <see langword="true"/> if the entry was created or updated;
    /// otherwise <see langword="false"/>.
    /// </returns>
	public ValueTask<bool> CreateOrUpdate(TValue value)
		=> _asyncDictionary.CreateOrUpdateAsync(Key, value);

    /// <summary>
    /// Reads the value of the entry with this key.
    /// </summary>
    /// <returns>
    /// The value of the entry,
    /// or <see langword="null"/> if the entry does not exist.
    /// </returns>
	public ValueTask<TValue?> Read()
		=> _asyncDictionary.ReadAsync(Key);

    /// <summary>
    /// Deletes the entry with this key.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if the entry was deleted;
    /// otherwise <see langword="false"/>.
    /// </returns>
	public ValueTask<bool> Delete()
		=> _asyncDictionary.DeleteAsync(Key);
}
