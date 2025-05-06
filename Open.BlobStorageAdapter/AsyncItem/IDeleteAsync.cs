namespace Open.BlobStorageAdapter.AsyncItem;
public interface IDeleteAsync<TKey>
	where TKey : notnull
{
	/// <summary>
	/// Deletes a blob with the specified key from the store.
	/// </summary>
	/// <param name="key">The key identifying the blob.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <returns>A task that returns true if the blob was deleted; otherwise, false.</returns>
	ValueTask<bool> DeleteAsync(
		TKey key,
		CancellationToken cancellationToken = default);
}
