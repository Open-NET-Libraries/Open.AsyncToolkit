namespace Open.BlobStorageAdapter.AsyncItem;

public interface IUpdateAsync<TKey, TValue>
	where TKey : notnull
{   /// <summary>
	/// Writes a blob to the store with the specified key.
	/// </summary>
	/// <param name="key">The key identifying the blob.</param>
	/// <param name="overwrite">Indicates whether to overwrite an existing blob with the same key.</param>
	/// <param name="writeHandler">A function that writes content to the provided stream.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <returns><see langword="true"/> if the blob was stored; otherwise <see langword="false"/>.</returns>
	ValueTask<bool> UpdateAsync(
		TKey key,
		TValue value,
		CancellationToken cancellationToken = default);
}
