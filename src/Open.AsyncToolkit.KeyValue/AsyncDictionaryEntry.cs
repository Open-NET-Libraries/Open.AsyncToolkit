namespace Open.AsyncToolkit.KeyValue;

/// <summary>
/// Represents an entry in an asynchronous dictionary,
/// providing operations on a specific key.
/// </summary>
/// <typeparam name="TKey">The type of key.</typeparam>
/// <typeparam name="TValue">The type of value.</typeparam>
public record AsyncDictionaryEntry<TKey, TValue>
	: IAsyncDictionaryEntry<TKey, TValue>
	where TKey : notnull
{
	private readonly IAsyncDictionary<TKey, TValue> _asyncDictionary;

	/// <summary>
	/// Initializes a new instance of the <see cref="AsyncDictionaryEntry{TKey, TValue}"/> class.
	/// </summary>
	/// <param name="key">The key for this entry.</param>
	/// <param name="asyncDictionary">The dictionary this entry belongs to.</param>
	/// <exception cref="ArgumentNullException">
	/// Thrown when <paramref name="asyncDictionary"/> is <see langword="null"/>.
	/// </exception>
	public AsyncDictionaryEntry(
		TKey key,
		IAsyncDictionary<TKey, TValue> asyncDictionary)
	{
		Key = key;
		_asyncDictionary = asyncDictionary
			?? throw new ArgumentNullException(nameof(asyncDictionary));
	}

	/// <inheritdoc />
	public TKey Key { get; }

	/// <inheritdoc />
	public ValueTask<bool> Exists(CancellationToken cancellationToken = default)
		=> _asyncDictionary.ExistsAsync(Key, cancellationToken);

	/// <inheritdoc />
	public ValueTask<TryReadResult<TValue>> TryRead(CancellationToken cancellationToken = default)
		=> _asyncDictionary.TryReadAsync(Key, cancellationToken);

	/// <inheritdoc />
	public ValueTask<bool> Create(TValue value, CancellationToken cancellationToken = default)
		=> _asyncDictionary.CreateAsync(Key, value, cancellationToken);

	/// <inheritdoc />
	public ValueTask<bool> CreateOrUpdate(TValue value, CancellationToken cancellationToken = default)
		=> _asyncDictionary.CreateOrUpdateAsync(Key, value, cancellationToken);

	/// <inheritdoc />
	public ValueTask<bool> Delete(CancellationToken cancellationToken = default)
		=> _asyncDictionary.DeleteAsync(Key, cancellationToken);
}
