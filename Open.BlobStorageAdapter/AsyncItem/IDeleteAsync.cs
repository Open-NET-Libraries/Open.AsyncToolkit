namespace Open.BlobStorageAdapter.AsyncItem;

/// <summary>
/// Defines operations for deleting items with a generic key type.
/// </summary>
/// <typeparam name="TKey">
/// The type of key used to identify items.
/// </typeparam>
public interface IDeleteAsync<in TKey>
	where TKey : notnull
{
	/// <summary>
	/// Deletes an item with the specified key.
	/// </summary>
	/// <param name="key">The key identifying the item to delete.</param>
	/// <param name="cancellationToken">An optional token to monitor for cancellation requests.</param>
	/// <returns>
	/// <see langword="true"/> if the item was deleted;
	/// otherwise <see langword="false"/>.
	/// </returns>
	ValueTask<bool> DeleteAsync(
		TKey key,
		CancellationToken cancellationToken = default);
}
