namespace Open.AsyncToolkit.BlobStorage.HashedRepository;

/// <summary>
/// An opinionated means of storing blobs in a blob store
/// that ensures any written blobs are hashed to prevent duplicates.
/// Any matching blobs stored will result in the same ID returned.
/// </summary>
public class HashedBlobRepository(
	IBlobRepo<Guid> blobRepo,
	ISynchronizedAsyncDictionary<string, IReadOnlyCollection<Guid>> hashMap,
	IHashProvider hashProvider)
	: IIdempotentRepository<Guid>
{
	/// <inheritdoc />
	public async ValueTask<Stream> GetAsync(
		Guid key,
		CancellationToken cancellationToken = default)
		=> await blobRepo.ReadAsync(key, cancellationToken)
			.ConfigureAwait(false)
		   ?? throw new KeyNotFoundException($"Key [{key}] not found.");

	/// <inheritdoc />
	public ValueTask<Guid> PutAsync(
		ReadOnlyMemory<byte> data,
		CancellationToken cancellationToken = default)
		=> hashMap.LeaseAsync(
			hashProvider.ComputeHash(data.Span),
			cancellationToken,
			async (entry, ct) =>
			{
				var guids = await entry.Read(ct)
					.ConfigureAwait(false)
#if NET9_0_OR_GREATER
					?? FrozenSet<Guid>.Empty;
#else
					?? ImmutableHashSet<Guid>.Empty;
#endif

				if (guids.Count > 0)
				{
					IMemoryOwner<byte>? lease = null;
					try
					{
						foreach (var guid in guids)
						{
							// Verify if the existing entry is a match.
							using var e = await blobRepo.ReadAsync(guid, ct)
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

				var newGuid = Guid.NewGuid();
				guids = guids.Append(newGuid)
#if NET9_0_OR_GREATER
					.ToFrozenSet();
#else
					.ToImmutableHashSet();
#endif
				await entry.CreateOrUpdate(guids, ct).ConfigureAwait(false);

				return newGuid;
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
