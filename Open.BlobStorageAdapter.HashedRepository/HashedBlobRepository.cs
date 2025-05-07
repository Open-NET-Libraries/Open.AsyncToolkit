using System.Buffers;
using System.Collections.Frozen;

namespace Open.BlobStorageAdapter;

/// <summary>
/// An opinionated means of storing blobs in a blob store
/// that ensures any written blobs are hashed to prevent duplicates.
/// Any matching blobs stored will result in the same ID returned.
/// </summary>
public class HashedBlobRepository(
	IBlobRepo<Guid> blobStore,
	IAsyncDictionary<string, IReadOnlyCollection<Guid>> hashMap,
	IHashProvider hashProvider)
	: IIdempotentRepository<Guid>
{
	public async ValueTask<Stream> Get(
		Guid key, CancellationToken cancellationToken = default)
		=> await blobStore.ReadAsync(key, cancellationToken).ConfigureAwait(false)
		?? throw new KeyNotFoundException($"Key [{key}] not found.");

	public ValueTask<Guid> Put(
		ReadOnlyMemory<byte> data,
		CancellationToken cancellationToken = default)
		=> hashMap.Lease(
			hashProvider.ComputeHash(data.Span),
			cancellationToken,
			async (entry, ct) =>
			{
				ct.ThrowIfCancellationRequested();

				var guids = await entry.Read() ?? FrozenSet<Guid>.Empty;
				if (guids.Count > 0)
				{
					IMemoryOwner<byte>? lease = null;
					try
					{
						foreach (var guid in guids)
						{
							ct.ThrowIfCancellationRequested();

							// Verify if the existing entry is a match.
							using var e = await blobStore.ReadAsync(guid, ct)
								.ConfigureAwait(false);

							// Should never happen, but we'll check anyway and move on.
							if (e is null)
								continue;

							// Obvious quick check.
							if (e.Length != data.Length)
								continue;

							// Since the change of a hash being the same, it's okay to load the entire item to verify its contents.
							// This is a bit of a performance hit, but it's the only way to be 100% sure.
							lease ??= MemoryPool<byte>.Shared.Rent(data.Length);
							if (await IsSame(data, e, lease.Memory).ConfigureAwait(false))
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

				ct.ThrowIfCancellationRequested();

				var newGuid = Guid.NewGuid();
				guids = guids.Append(newGuid).ToFrozenSet();
				await entry.CreateOrUpdate(guids).ConfigureAwait(false);

				return newGuid;
			});

	private static async ValueTask<bool> IsSame(ReadOnlyMemory<byte> data, Stream stream, Memory<byte> buffer)
	{
		int totalRead = 0;
		while (totalRead < data.Length)
		{
			int toRead = Math.Min(buffer.Length, data.Length - totalRead);
			int bytesRead = await stream.ReadAsync(buffer[..toRead]).ConfigureAwait(false);

			if (bytesRead == 0)
				return false;

			if (!buffer.Span[..bytesRead].SequenceEqual(data.Span.Slice(totalRead, bytesRead)))
				return false;

			totalRead += bytesRead;
		}

		return true;
	}
}
