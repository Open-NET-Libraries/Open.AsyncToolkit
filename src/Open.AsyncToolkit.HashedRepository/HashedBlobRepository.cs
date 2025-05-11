namespace Open.AsyncToolkit.HashedRepository;

/// <summary>
/// An opinionated means of storing blobs in a blob store
/// that ensures any written blobs are hashed to prevent duplicates.
/// Any matching blobs stored will result in the same ID returned.
/// </summary>
public class HashedBlobRepository<TUniqueId>
	: IIdempotentRepository<TUniqueId>
	where TUniqueId : notnull
{
	private readonly IBlobRepo<TUniqueId> _blobRepo;
	private readonly ISynchronizedAsyncDictionary<string, IReadOnlyCollection<TUniqueId>> _hashMap;
	private readonly IHashProvider _hashProvider;
	private readonly Func<TUniqueId> _idProvider;

	internal HashedBlobRepository(
		IBlobRepo<TUniqueId> blobRepo,
		ISynchronizedAsyncDictionary<string, IReadOnlyCollection<TUniqueId>> hashMap,
		Func<TUniqueId> idProvider,
		IHashProvider? hashProvider)
	{
		_blobRepo = blobRepo ?? throw new ArgumentNullException(nameof(blobRepo));
		_hashMap = hashMap ?? throw new ArgumentNullException(nameof(hashMap));
		_idProvider = idProvider ?? throw new ArgumentNullException(nameof(idProvider));
		_hashProvider = hashProvider ?? Sha256HashProvider.Default;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="HashedBlobRepository{TUniqueId}"/> class.
	/// </summary>
	/// <param name="blobRepo">The blob repository to use for storage.</param>
	/// <param name="hashMap">The hash map to use for mapping hashes to IDs.</param>
	/// <param name="hashProvider">The hash provider to use for computing hashes.</param>
	/// <param name="idProvider">The unique ID provider to use for generating IDs.</param>
	/// <remarks>
	/// It is vital the <paramref name="idProvider"/> provides a unique ID every time.
	/// </remarks>
	/// <exception cref="ArgumentNullException"></exception>
	public HashedBlobRepository(
		IBlobRepo<TUniqueId> blobRepo,
		ISynchronizedAsyncDictionary<string, IReadOnlyCollection<TUniqueId>> hashMap,
		IUniqueIdProvider<TUniqueId> idProvider,
		IHashProvider? hashProvider)
		: this(blobRepo, hashMap, idProvider is null ? null! : idProvider.NewId, hashProvider)
	{
	}

	/// <inheritdoc />
	public async ValueTask<Stream> GetAsync(
		TUniqueId id,
		CancellationToken cancellationToken = default)
		=> await _blobRepo.ReadAsync(id, cancellationToken)
			.ConfigureAwait(false)
		   ?? throw new KeyNotFoundException($"ID [{id}] not found.");

	/// <inheritdoc />
	public ValueTask<TUniqueId> PutAsync(
		ReadOnlyMemory<byte> data,
		CancellationToken cancellationToken = default)
		=> _hashMap.LeaseAsync(
			_hashProvider.ComputeHash(data.Span),
			cancellationToken,
			async (entry, ct) =>
			{
				var ids = await entry.Read(ct)
					.ConfigureAwait(false)
#if NET9_0_OR_GREATER
					?? FrozenSet<TUniqueId>.Empty;
#else
					?? ImmutableHashSet<TUniqueId>.Empty;
#endif

				if (ids.Count > 0)
				{
					IMemoryOwner<byte>? lease = null;
					try
					{
						foreach (var guid in ids)
						{
							// Verify if the existing entry is a match.
							using var e = await _blobRepo.ReadAsync(guid, ct)
								.ConfigureAwait(false);

							// Should never happen, but we'll check anyway and move on.
							if (e is null)
								continue;

							// Obvious quick check.
							if (e.Length != data.Length)
								continue;

							// Since the change of a hash being the same, it's okay to load 
							// the entire item to verify its contents. This is a bit of a 
							// performance hit, but it's the only way to be 100% sure.
							lease ??= MemoryPool<byte>.Shared.Rent(data.Length);
							if (await IsSame(data, e, lease.Memory)
								.ConfigureAwait(false))
							{
								return guid; // Exact match found, return existing GUID
							}
						}
					}
					finally
					{
						lease?.Dispose(); // Dispose the lease if it was created
					}
				}

				var newId = _idProvider();
				ids = ids.Append(newId)
#if NET9_0_OR_GREATER
					.ToFrozenSet();
#else
					.ToImmutableHashSet();
#endif
				await entry.CreateOrUpdate(ids, ct).ConfigureAwait(false);

				return newId;
			});

	/// <summary>
	/// Compares a byte array with a stream's content to determine 
	/// if they are identical.
	/// </summary>
	/// <param name="data">The source data to compare.</param>
	/// <param name="stream">The stream containing data to compare.</param>
	/// <param name="buffer">A buffer to use for reading from the stream.</param>
	/// <returns>
	/// <see langword="true"/> if the data matches;
	/// otherwise <see langword="false"/>.
	/// </returns>
	private static async ValueTask<bool> IsSame(
		ReadOnlyMemory<byte> data,
		Stream stream,
		Memory<byte> buffer)
	{
		int totalRead = 0;
		while (totalRead < data.Length)
		{
			int toRead = Math.Min(buffer.Length, data.Length - totalRead);
			int bytesRead = await stream.ReadAsync(buffer.Slice(0, toRead))
				.ConfigureAwait(false);

			if (bytesRead == 0 || !buffer.Span.Slice(0, bytesRead)
				.SequenceEqual(data.Span.Slice(totalRead, bytesRead)))
			{
				return false;
			}

			totalRead += bytesRead;
		}

		return true;
	}
}

/// <inheritdoc />
/// <summary>
/// Initializes a new instance of the <see cref="HashedBlobRepository"/> class.
/// </summary>
/// <param name="blobRepo">The blob repository to use for storage.</param>
/// <param name="hashMap">The hash map to use for mapping hashes to IDs.</param>
/// <param name="hashProvider">The hash provider to use for computing hashes.</param>
public class HashedBlobRepository(
	IBlobRepo<string> blobRepo,
	ISynchronizedAsyncDictionary<string, IReadOnlyCollection<string>> hashMap,
	IHashProvider? hashProvider)
	: HashedBlobRepository<string>(
	  blobRepo, hashMap,
	  GuidStringProvider.Instance,
	  hashProvider)
{
	/// <summary>
	/// Creates a new instance of the <see cref="HashedBlobRepository{TUniqueId}"/> class.
	/// </summary>
	public static HashedBlobRepository<TUniqueId> Create<TUniqueId>(
		IBlobRepo<TUniqueId> blobRepo,
		ISynchronizedAsyncDictionary<string, IReadOnlyCollection<TUniqueId>> hashMap,
		Func<TUniqueId> idProvider,
		IHashProvider? hashProvider = null)
		where TUniqueId : notnull
		=> new(blobRepo, hashMap, idProvider, hashProvider);

	/// <summary>
	/// Creates a new instance of the <see cref="HashedBlobRepository"/> class.
	/// </summary>
	/// <remarks>Uses a string as the underlying ID generated from a new GUID.</remarks>
	public static HashedBlobRepository Create(
		IBlobRepo<string> blobRepo,
		ISynchronizedAsyncDictionary<string, IReadOnlyCollection<string>> hashMap,
		IHashProvider? hashProvider = null)
		=> new(blobRepo, hashMap, hashProvider);
}