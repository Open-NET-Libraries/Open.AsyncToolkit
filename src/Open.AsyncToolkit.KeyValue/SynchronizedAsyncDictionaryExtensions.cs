namespace Open.AsyncToolkit.KeyValue;

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
	public static SynchronizedAsyncDictionary<TKey, TValue> Synchronized<TKey, TValue>(
		this IAsyncDictionary<TKey, TValue> asyncDictionary)
		where TKey : notnull
		=> new(asyncDictionary);

	/// <summary>
	/// Leases an entry for the specified key and performs an operation on it.
	/// </summary>
	/// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
	/// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
	/// <param name="asyncDictionary">The async dictionary.</param>
	/// <param name="key">The key identifying the entry to lease.</param>
	/// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
	/// <param name="operation">The operation to perform on the leased entry.</param>

	public static async ValueTask LeaseAsync<TKey, TValue>(
		this ISynchronizedAsyncDictionary<TKey, TValue> asyncDictionary,
		TKey key,
		CancellationToken cancellationToken,
		Func<IAsyncDictionaryEntry<TKey, TValue>, CancellationToken, ValueTask> operation)
		where TKey : notnull
	{
		if (asyncDictionary is null) throw new ArgumentNullException(nameof(asyncDictionary));
		await asyncDictionary
			.LeaseAsync(
				key, cancellationToken, async (e, ct) =>
				{
					await operation(e, ct).ConfigureAwait(false);
					return true;
				})
			.ConfigureAwait(false);
	}

	/// <inheritdoc cref="LeaseAsync{TKey, TValue}(ISynchronizedAsyncDictionary{TKey, TValue}, TKey, CancellationToken, Func{IAsyncDictionaryEntry{TKey, TValue}, CancellationToken, ValueTask})"/>
	public static async ValueTask LeaseAsync<TKey, TValue>(
		this ISynchronizedAsyncDictionary<TKey, TValue> asyncDictionary,
		TKey key,
		Func<IAsyncDictionaryEntry<TKey, TValue>, ValueTask> operation)
		where TKey : notnull
	{
		if (asyncDictionary is null) throw new ArgumentNullException(nameof(asyncDictionary));
		await asyncDictionary
			.LeaseAsync(
				key, default, async (e, _) =>
				{
					await operation(e).ConfigureAwait(false);
					return true;
				})
			.ConfigureAwait(false);
	}

	/// <returns>
	/// The result of the provided operation.
	/// </returns>
	/// <inheritdoc cref="LeaseAsync{TKey, TValue}(ISynchronizedAsyncDictionary{TKey, TValue}, TKey, CancellationToken, Func{IAsyncDictionaryEntry{TKey, TValue}, CancellationToken, ValueTask})"/>
	public static ValueTask<TResult> LeaseAsync<TKey, TValue, TResult>(
		this ISynchronizedAsyncDictionary<TKey, TValue> asyncDictionary,
		TKey key,
		Func<IAsyncDictionaryEntry<TKey, TValue>, ValueTask<TResult>> operation)
		where TKey : notnull
	{
		if (asyncDictionary is null) throw new ArgumentNullException(nameof(asyncDictionary));
		return asyncDictionary
			.LeaseAsync(key, CancellationToken.None, (e, _) => operation(e));
	}

	/// <summary>
	/// Gets the value associated with the specified key, or adds a new value if the key does not exist.
	/// </summary>
	/// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
	/// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
	/// <param name="asyncDictionary">The async dictionary.</param>
	/// <param name="key">The key to get or add.</param>
	/// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
	/// <param name="valueFactory">The function to create a new value if the key does not exist.</param>
	/// <returns>The value associated with the specified key.</returns>
	/// <exception cref="ArgumentNullException">The <paramref name="asyncDictionary"/> is <see langword="null"/>.</exception>
	public static async ValueTask<TValue> GetOrAddAsync<TKey, TValue>(
		this ISynchronizedAsyncDictionary<TKey, TValue> asyncDictionary,
		TKey key,
		CancellationToken cancellationToken,
		Func<TKey, CancellationToken, ValueTask<TValue>> valueFactory)
		where TKey : notnull
	{
		if (asyncDictionary is null) throw new ArgumentNullException(nameof(asyncDictionary));
		var entry = await asyncDictionary.TryReadAsync(key, cancellationToken).ConfigureAwait(false);
		if (entry.Success) return entry.Value;

		// If the entry doesn't exist, lease it and perform the operation to create it.
		return await asyncDictionary
			.LeaseAsync(
				key, cancellationToken,
				async (e, ct) =>
				{
					var entry = await asyncDictionary.TryReadAsync(key, ct).ConfigureAwait(false);
					if (entry.Success) return entry.Value;

					var value = await valueFactory(key, ct).ConfigureAwait(false);
					bool created = await e.Create(value, ct).ConfigureAwait(false);
					Debug.Assert(created, "Failed to create the entry in the dictionary.");
					return value;
				})
			.ConfigureAwait(false);
	}
}
