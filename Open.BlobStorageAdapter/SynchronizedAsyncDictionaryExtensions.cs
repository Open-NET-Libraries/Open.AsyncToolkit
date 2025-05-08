namespace Open.BlobStorageAdapter;

/// <summary>
/// Provides extension methods for <see cref="ISynchronizedAsyncDictionary{TKey, TValue}"/>.
/// </summary>
#pragma warning disable IDE0079 // Remove unnecessary suppression
[System.Diagnostics.CodeAnalysis.SuppressMessage(
	"Design", "CA1068:CancellationToken parameters must come last",
	Justification = "Important for these signatures.")]
#pragma warning restore IDE0079 // Remove unnecessary suppression
public static class SynchronizedAsyncDictionaryExtensions
{
	/// <summary>
	/// Creates a synchronized wrapper around the specified async dictionary that provides exclusive leased access to dictionary entries.
	/// </summary>
	/// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
	/// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
	/// <param name="asyncDictionary">The async dictionary to wrap.</param>
	/// <returns>A new <see cref="ISynchronizedAsyncDictionary{TKey, TValue}"/> that provides synchronized access to the specified dictionary.</returns>
	public static ISynchronizedAsyncDictionary<TKey, TValue> Synchronized<TKey, TValue>(
		this IAsyncDictionary<TKey, TValue> asyncDictionary)
		where TKey : notnull
		=> new SynchronizedAsyncDictionary<TKey, TValue>(asyncDictionary);

	/// <summary>
	/// Leases an entry for the specified key and performs an operation on it.
	/// </summary>
	/// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
	/// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
	/// <typeparam name="TResult">The type of result returned by the operation.</typeparam>
	/// <param name="asyncDictionary">The async dictionary.</param>
	/// <param name="key">The key identifying the entry to lease.</param>
	/// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
	/// <param name="operation">The operation to perform on the leased entry.</param>

	public static async ValueTask LeaseAsync<TKey, TValue, TResult>(
		this ISynchronizedAsyncDictionary<TKey, TValue> asyncDictionary,
		TKey key,
		CancellationToken cancellationToken,
		Func<IAsyncDictionaryEntry<TKey, TValue>, CancellationToken, ValueTask> operation)
		where TKey : notnull
		=> await asyncDictionary
			.LeaseAsync(
				key, cancellationToken, async (e, ct) =>
				{
					await operation(e, ct);
					return true;
				})
			.ConfigureAwait(false);

	/// <inheritdoc cref="LeaseAsync{TKey, TValue, TResult}(ISynchronizedAsyncDictionary{TKey, TValue}, TKey, CancellationToken, Func{IAsyncDictionaryEntry{TKey, TValue}, CancellationToken, ValueTask})"/>
	public static async ValueTask LeaseAsync<TKey, TValue, TResult>(
		this ISynchronizedAsyncDictionary<TKey, TValue> asyncDictionary,
		TKey key,
		Func<IAsyncDictionaryEntry<TKey, TValue>, ValueTask> operation)
		where TKey : notnull
		=> await asyncDictionary
			.LeaseAsync(
				key, default, async (e, _) =>
				{
					await operation(e);
					return true;
				})
			.ConfigureAwait(false);

	/// <returns>
	/// The result of the provided operation.
	/// </returns>
	/// <inheritdoc cref="LeaseAsync{TKey, TValue, TResult}(ISynchronizedAsyncDictionary{TKey, TValue}, TKey, CancellationToken, Func{IAsyncDictionaryEntry{TKey, TValue}, CancellationToken, ValueTask})"/>
	public static ValueTask<TResult> LeaseAsync<TKey, TValue, TResult>(
		this ISynchronizedAsyncDictionary<TKey, TValue> asyncDictionary,
		TKey key,
		Func<IAsyncDictionaryEntry<TKey, TValue>, ValueTask<TResult>> operation)
		where TKey : notnull
		=> asyncDictionary
			.LeaseAsync(key, CancellationToken.None, (e, _) => operation(e));
}
