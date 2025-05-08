namespace Open.BlobStorageAdapter.AsyncItem;

/// <summary>
/// Defines operations for creating or updating items with a generic key type.
/// </summary>
/// <typeparam name="TKey">
/// The type of key used to identify items.
/// </typeparam>
/// <typeparam name="TValue">The type of values to store.</typeparam>
public interface ICreateOrUpdate<in TKey, in TValue>
	where TKey : notnull
{
	/// <summary>
	/// Creates a new item or updates an existing item with the specified key.
	/// </summary>
	/// <returns>
	/// <see langword="true"/> if the item was created or updated;
	/// otherwise <see langword="false"/>.
	/// </returns>
	/// <inheritdoc cref="ICreateAsync{TKey, TValue}.CreateAsync(TKey, TValue, CancellationToken)" />
	ValueTask<bool> CreateOrUpdateAsync(
		TKey key,
		TValue value,
		CancellationToken cancellationToken = default);
}
