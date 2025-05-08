namespace Open.BlobStorageAdapter;

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
