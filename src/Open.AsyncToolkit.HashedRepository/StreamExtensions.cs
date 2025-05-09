#if NETSTANDARD2_0
using System.Runtime.InteropServices;

namespace Open.AsyncToolkit.BlobStorage.HashedRepository;

/// <summary>
/// Extension methods for Stream operations.
/// </summary>
internal static class StreamExtensions
{
	/// <summary>
	/// Asynchronously reads a sequence of bytes from the current stream into a memory region.
	/// </summary>
	/// <param name="stream">The stream to read from.</param>
	/// <param name="buffer">The memory to write the data into.</param>
	/// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
	/// <returns>The number of bytes read from the stream.</returns>
	public static async ValueTask<int> ReadAsync(
		this Stream stream,
		Memory<byte> buffer,
		CancellationToken cancellationToken = default)
	{
		if (MemoryMarshal.TryGetArray(buffer, out ArraySegment<byte> arraySegment))
		{
			return await stream
				.ReadAsync(
					arraySegment.Array,
					arraySegment.Offset,
					arraySegment.Count,
					cancellationToken)
				.ConfigureAwait(false);
		}

		// Fall-back for cases where TryGetArray fails
		byte[] array = ArrayPool<byte>.Shared.Rent(buffer.Length);
		try
		{
			int bytesRead = await stream
				.ReadAsync(array, 0, buffer.Length, cancellationToken)
				.ConfigureAwait(false);

			if (bytesRead > 0)
			{
				new Span<byte>(array, 0, bytesRead).CopyTo(buffer.Span);
			}

			return bytesRead;
		}
		finally
		{
			// Return the array to the pool when done
			ArrayPool<byte>.Shared.Return(array);
		}
	}
}
#endif