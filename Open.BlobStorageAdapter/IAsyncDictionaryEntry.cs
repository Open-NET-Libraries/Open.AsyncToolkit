namespace Open.BlobStorageAdapter;

public interface IAsyncDictionaryEntry<TKey, TValue>
{
	/// <summary>
	/// Gets the key for this entry.
	/// </summary>
	TKey Key { get; }

	/// <summary>
	/// Checks if an entry with this key exists in the dictionary.
	/// </summary>
	/// <returns>
	/// <see langword="true"/> if the entry exists;
	/// otherwise <see langword="false"/>.
	/// </returns>
	ValueTask<bool> Exists();

	/// <summary>
	/// Creates a new entry with this key and the specified value.
	/// </summary>
	/// <param name="value">The value to store.</param>
	/// <returns>
	/// <see langword="true"/> if the entry was created;
	/// otherwise <see langword="false"/>.
	/// </returns>
	ValueTask<bool> Create(TValue value);

	/// <summary>
	/// Creates a new entry or updates an existing entry with this key 
	/// and the specified value.
	/// </summary>
	/// <param name="value">The value to store.</param>
	/// <returns>
	/// <see langword="true"/> if the entry was created or updated;
	/// otherwise <see langword="false"/>.
	/// </returns>
	ValueTask<bool> CreateOrUpdate(TValue value);

	/// <summary>
	/// Reads the value of the entry with this key.
	/// </summary>
	/// <returns>
	/// The value of the entry,
	/// or <see langword="null"/> if the entry does not exist.
	/// </returns>
	ValueTask<TValue?> Read();

	/// <summary>
	/// Deletes the entry with this key.
	/// </summary>
	/// <returns>
	/// <see langword="true"/> if the entry was deleted;
	/// otherwise <see langword="false"/>.
	/// </returns>
	ValueTask<bool> Delete();
}
