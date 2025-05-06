using System.Buffers;
using System.Text;

namespace Open.BlobStorageAdapter;

/// <summary>
/// Defines basic hashing operations for computing content-based identifiers
/// </summary>
public interface IHashProvider
{
	/// <summary>
	/// Computes a hash from bytes.
	/// </summary>
	/// <param name="data">The data to hash</param>
	/// <returns>A hash string</returns>
	string ComputeHash(ReadOnlySpan<byte> data);

	/// <summary>
	/// Computes a hash from character data using the specified encoding.
	/// </summary>
	/// <param name="data">The character data to hash</param>
	/// <param name="encoding">The encoding to use, defaults to UTF8</param>
	/// <returns>A hash string</returns>
	string ComputeHash(ReadOnlySpan<char> data, Encoding? encoding = null)
	{
		encoding ??= Encoding.UTF8;
		// Rent a buffer from the memory pool
		int maxByteCount = encoding.GetMaxByteCount(data.Length);
		using var lease = MemoryPool<byte>.Shared.Rent(maxByteCount);
		var span = lease.Memory.Span;
		int byteCount = encoding.GetBytes(data, span);

		// Compute the hash using the rented buffer
		return ComputeHash(span.Slice(0, byteCount));
	}
}