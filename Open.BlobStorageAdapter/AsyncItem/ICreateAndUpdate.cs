namespace Open.BlobStorageAdapter.AsyncItem;

public interface ICreateAndUpdate<TKey, TValue> : ICreateAsync<TKey, TValue>, IUpdateAsync<TKey, TValue>
	where TKey : notnull
{
	/// <summary>
	/// Writes a blob to the store with the specified key if it does not exist.
	/// </summary>
	/// <param name="key">The key identifying the blob.</param>
	/// <param name="value">The value of the item.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <returns><see langword="true"/> if the blob was stored; otherwise <see langword="false"/>.</returns>
	ValueTask<bool> CreateOrUpdateAsync(
		TKey key,
		TValue value,
		CancellationToken cancellationToken = default);
}
