namespace Open.AsyncToolkit.KeyValue;

/// <summary>
/// Represents an asynchronous dictionary that provides exclusive leased access to dictionary entries.
/// This interface extends <see cref="IReadAsync{TKey, TValue}"/> to add operations that guarantee
/// exclusive access to a specific dictionary entry for the duration of an operation, effectively
/// creating a lease on that entry. This prevents race conditions in multi-threaded environments
/// by ensuring only one operation at a time can access a given key.
/// </summary>
/// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
/// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
public interface ISynchronizedAsyncDictionary<TKey, TValue>
	: IReadAsync<TKey, TValue>
	where TKey : notnull
{
	/// <inheritdoc cref="SynchronizedAsyncDictionaryExtensions.LeaseAsync{TKey, TValue, TResult}(ISynchronizedAsyncDictionary{TKey, TValue}, TKey, Func{IAsyncDictionaryEntry{TKey, TValue}, ValueTask{TResult}})" />
#pragma warning disable IDE0079 // Remove unnecessary suppression
	[System.Diagnostics.CodeAnalysis.SuppressMessage(
		"Design", "CA1068:CancellationToken parameters must come last",
		Justification = "Important for these signatures.")]
#pragma warning restore IDE0079 // Remove unnecessary suppression
	ValueTask<T> LeaseAsync<T>(
		TKey key,
		CancellationToken cancellationToken,
		Func<IAsyncDictionaryEntry<TKey, TValue>, CancellationToken, ValueTask<T>> operation);
}
