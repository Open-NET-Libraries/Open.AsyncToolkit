using System.Diagnostics.Contracts;
using System.Text;

namespace Open.AsyncToolkit.BlobStorage.HashedRepository;

/// <summary>
/// Defines basic hashing operations for computing content-based identifiers.
/// </summary>
public interface IHashProvider
{
	/// <summary>
	/// Computes a hash from bytes.
	/// </summary>
	/// <param name="data">The data to hash.</param>
	/// <returns>A string representation of the computed hash.</returns>
	string ComputeHash(ReadOnlySpan<byte> data);
}

/// <summary>
/// Provides extension methods for <see cref="IHashProvider"/>.
/// </summary>
public static class HashProviderExtensions
{
	/// <summary>
	/// Computes a hash from character data using the specified encoding.
	/// </summary>
	/// <param name="provider">The hash provider to use.</param>
	/// <param name="data">The character data to hash.</param>
	/// <param name="encoding">
	/// The encoding to use. If <see langword="null"/>, UTF-8 encoding is used.
	/// </param>
	/// <returns>A string representation of the computed hash.</returns>
	public static string ComputeHash(this IHashProvider provider, ReadOnlySpan<char> data, Encoding? encoding = null)
	{
		if (provider is null) throw new ArgumentNullException(nameof(provider));
		Contract.EndContractBlock();

		encoding ??= Encoding.UTF8;
		// Rent a buffer from the memory pool
		int maxByteCount = encoding.GetMaxByteCount(data.Length);
		using var lease = MemoryPool<byte>.Shared.Rent(maxByteCount);
		var span = lease.Memory.Span;
#if NETSTANDARD2_0
		int byteCount = ComputeHashWithoutCopy(data, span, encoding);
#else
		int byteCount = encoding.GetBytes(data, span);
#endif

		// Compute the hash using the rented buffer
		return provider.ComputeHash(span.Slice(0, byteCount));
	}

#if NETSTANDARD2_0
	// A shim for NETSTANDARD2_0 to av
	static int ComputeHashWithoutCopy(ReadOnlySpan<char> data, Span<byte> result, Encoding encoding)
	{
		// Since NETSTANDARD2.0 doesn't have GetBytes(ReadOnlySpan<char>, Span<byte>),
		// we need to use temporary string allocation
		int byteCount;
		unsafe
		{
			fixed (char* pChars = &data.GetPinnableReference())
			fixed (byte* pBytes = &result.GetPinnableReference())
			{
				byteCount = encoding.GetBytes(pChars, data.Length, pBytes, result.Length);
			}
		}

		return byteCount;
	}
#endif
}