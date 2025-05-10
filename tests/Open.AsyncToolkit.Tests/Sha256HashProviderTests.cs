namespace Open.AsyncToolkit.Tests;

/// <summary>
/// Tests for the <see cref="Sha256HashProvider"/> class.
/// </summary>
internal class Sha256HashProviderTests
{
	private static readonly Sha256HashProvider HashProvider = Sha256HashProvider.Default;

	// Known hash test values
	private static readonly byte[] EmptyData = [];
	private const string EmptyDataHash = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";

	private const string SimpleDataText = "test data";
	private static readonly byte[] SimpleData = Encoding.UTF8.GetBytes(SimpleDataText);
	private const string SimpleDataHash = "916f0027a575074ce72a331777c3478d6513f786a591bd892da1a577bf2335f9";

	private const int LargeDataSize = 1024 * 1024; // 1MB

	[Test]
	public async Task ComputeHash_ReturnsCorrectHash_ForEmptyData()
	{
		// Act
		string hash = HashProvider.ComputeHash(EmptyData);

		// Assert
		await Assert.That(hash).IsEqualTo(EmptyDataHash);
	}

	[Test]
	public async Task ComputeHash_ReturnsCorrectHash_ForSimpleData()
	{
		// Act
		string hash = HashProvider.ComputeHash(SimpleData);

		// Assert
		await Assert.That(hash).IsEqualTo(SimpleDataHash);
	}

	[Test]
	public async Task ComputeHash_ReturnsConsistentHash_ForSameData()
	{
		// Arrange
		byte[] data1 = Encoding.UTF8.GetBytes("consistent data");
		byte[] data2 = Encoding.UTF8.GetBytes("consistent data");

		// Act
		string hash1 = HashProvider.ComputeHash(data1);
		string hash2 = HashProvider.ComputeHash(data2);

		// Assert
		await Assert.That(hash1).IsEqualTo(hash2);
	}

	[Test]
	public async Task ComputeHash_ReturnsDifferentHash_ForDifferentData()
	{
		// Arrange
		byte[] data1 = Encoding.UTF8.GetBytes("data1");
		byte[] data2 = Encoding.UTF8.GetBytes("data2");

		// Act
		string hash1 = HashProvider.ComputeHash(data1);
		string hash2 = HashProvider.ComputeHash(data2);

		// Assert
		await Assert.That(hash1).IsNotEqualTo(hash2);
	}

	[Test]
	public async Task ComputeHash_HandlesLargeData()
	{
		// Arrange
		byte[] largeData = new byte[LargeDataSize];
		new Random(42).NextBytes(largeData); // Fill with random data

		// Act - should not throw exceptions or have memory issues
		string hash = HashProvider.ComputeHash(largeData);

		// Assert
		await Assert.That(hash).IsNotNull();
		await Assert.That(hash.Length).IsEqualTo(64); // SHA-256 hash is 64 hex chars
	}
}