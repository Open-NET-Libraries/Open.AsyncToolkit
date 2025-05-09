using System.Security.Cryptography;

namespace Open.AsyncToolkit.BlobStorage.HashedRepository;

/// <summary>
/// Implementation of <see cref="IHashProvider"/> using SHA-256 algorithm.
/// </summary>
public class Sha256HashProvider : IHashProvider
{
	/// <inheritdoc />
	public string ComputeHash(ReadOnlySpan<byte> data)
	{
		Span<byte> hashBytes = stackalloc byte[32]; // SHA256 produces 32 bytes

#if NET9_0_OR_GREATER
		SHA256.HashData(data, hashBytes);
#else
		using (var sha256 = SHA256.Create())
		{
			// For .NET Standard 2.0, we need to use ComputeHash with a byte array
			byte[] dataArray = data.ToArray();
			byte[] hashArray = sha256.ComputeHash(dataArray);
			hashArray.CopyTo(hashBytes);
		}
#endif

		// Each byte becomes 2 hex chars, so we need a 64-char buffer
#if NETSTANDARD2_0
		char[] hexChars = new char[64];
#else
		Span<char> hexChars = stackalloc char[64];
#endif

		for (int i = 0; i < hashBytes.Length; i++)
		{
			// Convert each byte to its 2-character hex representation
			byte b = hashBytes[i];
			hexChars[i * 2] = GetHexChar(b >> 4);
			hexChars[i * 2 + 1] = GetHexChar(b & 0xF);
		}

		return new string(hexChars);
	}

	/// <summary>
	/// Converts a value 0-15 to its hexadecimal character representation.
	/// </summary>
	/// <param name="value">The value to convert (must be 0-15).</param>
	/// <returns>A hexadecimal character ('0'-'9' or 'a'-'f').</returns>
	private static char GetHexChar(int value)
		=> (char)(value < 10 ? '0' + value : 'a' + (value - 10));

	/// <summary>
	/// Gets the default singleton instance of <see cref="Sha256HashProvider"/>.
	/// </summary>
	public static Sha256HashProvider Default { get; } = new();
}