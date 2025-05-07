namespace Open.BlobStorageAdapter;

/// <summary>
/// Represents an entry in an asynchronous dictionary,
/// providing operations on a specific key.
/// </summary>
/// <typeparam name="TKey">The type of key.</typeparam>
/// <typeparam name="TValue">The type of value.</typeparam>
public record AsyncDictionaryEntry<TKey, TValue>
	: IAsyncDictionaryEntry<TKey, TValue>
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
