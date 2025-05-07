namespace Open.BlobStorageAdapter.AsyncItem;

/// <summary>
/// Defines operations for reading items with a generic key type.
/// </summary>
/// <typeparam name="TKey">
/// The type of key used to identify items.
/// </typeparam>
/// <typeparam name="TValue">The type of values stored.</typeparam>
public interface IReadAsync<in TKey, TValue>
{
	/// <summary>
	/// Checks if an entry with the specified key exists.
	/// </summary>
	/// <param name="key">The key identifying the entry.</param>
	/// <param name="cancellationToken">An optional token to monitor for cancellation requests.</param>
	/// <returns>
	/// <see langword="true"/> if the entry exists;
	/// otherwise <see langword="false"/>.
	/// </returns>
	ValueTask<bool> ExistsAsync(
		TKey key,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Retrieves an item with the specified key.
	/// </summary>
	/// <param name="key">The key identifying the item.</param>
	/// <param name="cancellationToken">An optional token to monitor for cancellation requests.</param>
	/// <returns>
	/// The item's value,
	/// or <see langword="null"/> if the item does not exist.
	/// When the value is a disposable type,
	/// the caller is responsible for disposing it.
	/// </returns>
	ValueTask<TValue?> ReadAsync(
		TKey key,
		CancellationToken cancellationToken = default);
}