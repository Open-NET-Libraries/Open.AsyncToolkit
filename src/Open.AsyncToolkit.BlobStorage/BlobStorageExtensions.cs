#if NETSTANDARD2_0
using System.Runtime.InteropServices;
#endif

namespace Open.AsyncToolkit.BlobStorage;

/// <summary>
/// Provides extension methods for working with blob storage.
/// </summary>
public static class BlobStorageExtensions
{
	/// <exception cref="ArgumentNullException">If <paramref name="target"/> is <see langword="null"/>.</exception>
	/// <inheritdoc cref="ICreateBlobs{TKey}.CreateAsync(TKey, CancellationToken, Func{Stream, CancellationToken, ValueTask})"/>
	public static ValueTask<bool> CreateAsync<TKey>(
		this ICreateBlobs<TKey> target,
		TKey key, Func<Stream, ValueTask> writeHandler)
		where TKey : notnull
	{
		if (target is null) throw new ArgumentNullException(nameof(target));
		return target.CreateAsync(key, CancellationToken.None,
			(stream, _) => writeHandler(stream));
	}

	/// <exception cref="ArgumentNullException">If <paramref name="target"/> is <see langword="null"/>.</exception>
	/// <inheritdoc cref="IUpdateBlobs{TKey}.UpdateAsync(TKey, CancellationToken, Func{Stream, CancellationToken, ValueTask})"/>
	public static ValueTask<bool> UpdateAsync<TKey>(
		this IUpdateBlobs<TKey> target,
		TKey key, Func<Stream, ValueTask> writeHandler)
		where TKey : notnull
	{
		if (target is null) throw new ArgumentNullException(nameof(target));
		return target.UpdateAsync(key, CancellationToken.None,
			(stream, _) => writeHandler(stream));
	}

	/// <exception cref="ArgumentNullException">If <paramref name="target"/> is <see langword="null"/>.</exception>
	/// <inheritdoc cref="ICreateAndUpdateBlobs{TKey}.CreateOrUpdateAsync(TKey, CancellationToken, Func{Stream, CancellationToken, ValueTask})"/>/>
	public static ValueTask<bool> CreateOrUpdateAsync<TKey>(
		this ICreateAndUpdateBlobs<TKey> target,
		TKey key, Func<Stream, ValueTask> writeHandler)
		where TKey : notnull
	{
		if (target is null) throw new ArgumentNullException(nameof(target));
		return target.CreateOrUpdateAsync(key, CancellationToken.None,
			(stream, _) => writeHandler(stream));
	}

	/// <summary>
	/// Retrieves the blob data for the specified key. If the blob does not exist, it creates it using the provided write handler.
	/// </summary>
	/// <exception cref="IOException">If the blob cannot be read after creation.</exception>
	/// <inheritdoc cref="ICreateAndUpdateBlobs{TKey}.CreateOrUpdateAsync(TKey, CancellationToken, Func{Stream, CancellationToken, ValueTask})"/>/>
	public static async ValueTask<ReadOnlyMemory<byte>> GetOrCreateAsync<TKey>(
		this IBlobRepo<TKey> target,
		TKey key, CancellationToken cancellationToken,
		Func<Stream, CancellationToken, ValueTask> writeHandler)
		where TKey : notnull
	{
		if (target is null) throw new ArgumentNullException(nameof(target));
		var tryReadResult = await target.TryReadBytesAsync(key, cancellationToken).ConfigureAwait(false);
		if (tryReadResult.Success) return tryReadResult.Value;

		await target.CreateAsync(key, cancellationToken, writeHandler).ConfigureAwait(false);
		tryReadResult = await target.TryReadBytesAsync(key, cancellationToken).ConfigureAwait(false);
		if (!tryReadResult.Success) throw new IOException("Failed to read the blob after creating it.");
		return tryReadResult.Value;
	}

	/// <inheritdoc cref="GetOrCreateAsync{TKey}(IBlobRepo{TKey}, TKey, CancellationToken, Func{Stream, CancellationToken, ValueTask})"/>/>
	public static async ValueTask<ReadOnlyMemory<byte>> GetOrCreateAsync<TKey>(
		this IBlobRepo<TKey> target,
		TKey key, Func<Stream, ValueTask> writeHandler)
		where TKey : notnull
	{
		if (target is null) throw new ArgumentNullException(nameof(target));
		return await target.GetOrCreateAsync(key, CancellationToken.None, (stream, _) => writeHandler(stream)).ConfigureAwait(false);
	}

#if NETSTANDARD2_0
	// A shim function for .NET Standard 2.0 that reads from a Stream into a Memory<byte> with a Cancellation token
	internal static async ValueTask<int> ReadAsync(this Stream stream,
		Memory<byte> buffer,
		CancellationToken cancellationToken = default)
	{
		if (stream is null) throw new ArgumentNullException(nameof(stream));
		if (buffer.IsEmpty) return 0;

		cancellationToken.ThrowIfCancellationRequested();

		// Get a handle to the underlying memory without copying
		if (MemoryMarshal.TryGetArray(buffer, out ArraySegment<byte> segment))
		{
			// Use the array segment directly to avoid allocation
			return await stream.ReadAsync(segment.Array, segment.Offset, segment.Count, cancellationToken)
				.ConfigureAwait(false);
		}
		else
		{
			// Fall-back for when we can't get direct access to the underlying memory
			byte[] tempBuffer = new byte[buffer.Length];
			int bytesRead = await stream.ReadAsync(tempBuffer, 0, tempBuffer.Length, cancellationToken)
				.ConfigureAwait(false);

			if (bytesRead > 0)
			{
				// Copy the read bytes into the provided buffer
				new ReadOnlySpan<byte>(tempBuffer, 0, bytesRead).CopyTo(buffer.Span);
			}

			return bytesRead;
		}
	}

	// A shim function for .NET Standard 2.0 that uses a Stream and takes a ReadOnlyMemory<byte> with a Cancellation token and does what later versions do.
	internal static async ValueTask WriteAsync(this Stream stream,
		ReadOnlyMemory<byte> data,
		CancellationToken cancellationToken = default)
	{
		if (stream is null) throw new ArgumentNullException(nameof(stream));
		if (data.IsEmpty) return;

		cancellationToken.ThrowIfCancellationRequested();
                // Write to the stream manually instead of using the modern methods.

		// Get a handle to the underlying memory without copying
		if (MemoryMarshal.TryGetArray(data, out ArraySegment<byte> segment))
		{
			// Use the array segment directly to avoid allocation
			await stream.WriteAsync(segment.Array, segment.Offset, segment.Count, cancellationToken)
				.ConfigureAwait(false);
		}
		else
		{
			// Fall-back for when we can't get direct access to the underlying memory
			byte[] buffer = data.ToArray();
			await stream.WriteAsync(buffer, 0, buffer.Length, cancellationToken)
				.ConfigureAwait(false);
		}
	}
#endif
}
