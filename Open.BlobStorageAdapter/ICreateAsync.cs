namespace Open.BlobStorageAdapter;

/// <summary>
/// Defines operations for creating items with a generic key type.
/// </summary>
/// <typeparam name="TKey">
/// The type of key used to identify items.
/// </typeparam>
/// <typeparam name="TValue">The type of values to store.</typeparam>
public interface ICreateAsync<in TKey, in TValue>
	where TKey : notnull
{
	/// <summary>
	/// Creates a new item with the specified key if it does not already exist.
	/// </summary>
	/// <param name="key">The key identifying the item.</param>
	/// <param name="value">The value to store.</param>
	/// <param name="cancellationToken">An optional token to monitor for cancellation requests.</param>
	/// <returns>
	/// <see langword="true"/> if the item was created;
	/// otherwise <see langword="false"/>.
	/// </returns>
	ValueTask<bool> CreateAsync(
		TKey key,
		TValue value,
		CancellationToken cancellationToken = default);
}
