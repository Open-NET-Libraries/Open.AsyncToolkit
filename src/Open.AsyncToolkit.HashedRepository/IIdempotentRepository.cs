namespace Open.AsyncToolkit.HashedRepository;

/// <summary>
/// Defines operations for an idempotent repository where content is identified 
/// by its value rather than by an arbitrary key.
/// </summary>
/// <typeparam name="TKey">
/// The type of content-based key generated for the data.
/// </typeparam>
public interface IIdempotentRepository<TKey>
	where TKey : notnull
{
	/// <summary>
	/// Retrieves content from the repository using the specified content-based key.
	/// </summary>
	/// <param name="key">The content-based key identifying the data.</param>
	/// <param name="cancellationToken">An optional token to monitor for cancellation requests.</param>
	/// <returns>
	/// A stream containing the content.
	/// The caller is responsible for disposing the returned stream.
	/// </returns>
	/// <exception cref="KeyNotFoundException">
	/// Thrown when the specified key is not found in the repository.
	/// </exception>
	ValueTask<Stream> GetAsync(
		TKey key,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Stores content in the repository and returns a content-based key 
	/// that can be used to retrieve it.
	/// </summary>
	/// <param name="data">The data to store.</param>
	/// <param name="cancellationToken">An optional token to monitor for cancellation requests.</param>
	/// <returns>
	/// The content-based key that can be used to retrieve the data.
	/// </returns>
	ValueTask<TKey> PutAsync(
		ReadOnlyMemory<byte> data,
		CancellationToken cancellationToken = default);
}
