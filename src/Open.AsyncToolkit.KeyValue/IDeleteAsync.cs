namespace Open.AsyncToolkit.KeyValue;

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
	/// <returns>
	/// <see langword="true"/> if the item was deleted;
	/// otherwise <see langword="false"/>.
	/// </returns>
	/// <inheritdoc cref="ICreateAsync{TKey, TValue}.CreateAsync(TKey, TValue, CancellationToken)" />
	ValueTask<bool> DeleteAsync(
		TKey key,
		CancellationToken cancellationToken = default);
}
