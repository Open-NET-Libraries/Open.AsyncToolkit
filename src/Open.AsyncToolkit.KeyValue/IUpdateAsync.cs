namespace Open.AsyncToolkit.KeyValue;

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
	/// <returns>
	/// <see langword="true"/> if the item was updated;
	/// otherwise <see langword="false"/>.
	/// </returns>
	/// <inheritdoc cref="ICreateAsync{TKey, TValue}.CreateAsync(TKey, TValue, CancellationToken)" />
	ValueTask<bool> UpdateAsync(
		TKey key,
		TValue value,
		CancellationToken cancellationToken = default);
}
