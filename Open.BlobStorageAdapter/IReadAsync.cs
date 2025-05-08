
namespace Open.BlobStorageAdapter;

/// <summary>
/// Defines operations for reading items with a generic key type.
/// </summary>
/// <typeparam name="TKey">
/// The type of key used to identify items.
/// </typeparam>
/// <typeparam name="TValue">The type of values stored.</typeparam>
public interface IReadAsync<in TKey, TValue>
	where TKey : notnull
{
	/// <summary>
	/// Checks if an entry with the specified key exists.
	/// </summary>
	/// <param name="cancellationToken">An optional token to monitor for cancellation requests.</param>
	/// <returns>
	/// <see langword="true"/> if the entry exists;
	/// otherwise <see langword="false"/>.
	/// </returns>
	ValueTask<bool> ExistsAsync(
		TKey key,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Tries to retrieve an item with the specified key.
	/// </summary>
	/// <returns>
	/// A <see cref="TryReadResult{TValue}" /> containing:<br/>
	/// - <c>Success</c>: A <see langword="boolean" /> indicating whether the operation succeeded.<br/>
	/// - <c>Value</c>: The retrieved <typeparamref name="TValue"/> if successful;
	/// otherwise, if the value is requested it will throw an <see cref="InvalidOperationException"/>.
	/// </returns>
	/// <inheritdoc cref="ExistsAsync(TKey, CancellationToken)"/>
	ValueTask<TryReadResult<TValue>> TryReadAsync(
		TKey key,
		CancellationToken cancellationToken = default);
}

public static class ReadAsyncExtensions
{
	/// <summary>
	/// Retrieves an item with the specified key.
	/// </summary>
	/// <returns>
	/// The <typeparamref name="TValue"/> if it exists;
	/// otherwise <see langword="null"/> if the item does not exist.
	/// </returns>
	/// <remarks>
	/// Only available for reference types.
	/// </remarks>
	/// <inheritdoc cref="IReadAsync{TKey, TValue}.ExistsAsync(TKey, CancellationToken)"/>
	public static async ValueTask<TValue?> ReadAsync<TKey, TValue>(
		this IReadAsync<TKey, TValue> source,
		TKey key,
		CancellationToken cancellationToken = default)
		where TKey : notnull
		where TValue : class
	{
		var result = await source.TryReadAsync(key, cancellationToken).ConfigureAwait(false);
		return result.Success ? result.Value : null;
	}
}