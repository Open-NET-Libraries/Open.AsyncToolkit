namespace Open.BlobStorageAdapter.AsyncItem;

/// <summary>
/// Defines operations for updating items with a generic key type.
/// </summary>
/// <typeparam name="TKey">
/// The type of key used to identify items.
/// </typeparam>
/// <typeparam name="TValue">The type of values to store.</typeparam>
public interface IUpdateAsync<in TKey, in TValue>
	where TKey : notnull
{
	/// <summary>
	/// Updates an existing item with the specified key.
	/// </summary>
	/// <param name="key">The key identifying the item to update.</param>
	/// <param name="value">The new value to store.</param>
	/// <param name="cancellationToken">An optional token to monitor for cancellation requests.</param>
	/// <returns>
	/// <see langword="true"/> if the item was updated;
	/// otherwise <see langword="false"/>.
	/// </returns>
	ValueTask<bool> UpdateAsync(
		TKey key,
		TValue value,
		CancellationToken cancellationToken = default);
}
