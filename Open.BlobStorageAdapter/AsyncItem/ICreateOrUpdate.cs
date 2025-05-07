namespace Open.BlobStorageAdapter.AsyncItem;

/// <summary>
/// Defines operations for creating or updating items with a generic key type.
/// </summary>
/// <typeparam name="TKey">
/// The type of key used to identify items.
/// </typeparam>
/// <typeparam name="TValue">The type of values to store.</typeparam>
public interface ICreateOrUpdate<TKey, TValue>
	where TKey : notnull
{
	/// <summary>
	/// Creates a new item or updates an existing item with the specified key.
	/// </summary>
	/// <param name="key">The key identifying the item.</param>
	/// <param name="value">The value to store.</param>
	/// <param name="cancellationToken">An optional token to monitor for cancellation requests.</param>
	/// <returns>
	/// <see langword="true"/> if the item was created or updated;
	/// otherwise <see langword="false"/>.
	/// </returns>
	ValueTask<bool> CreateOrUpdateAsync(
		TKey key,
		TValue value,
		CancellationToken cancellationToken = default);
}
