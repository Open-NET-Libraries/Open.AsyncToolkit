using System.Text;
using NSubstitute;

namespace Open.BlobStorageAdapter.Tests;

/// <summary>
/// Tests for the <see cref="Sha256HashProvider"/> class.
/// </summary>
public class Sha256HashProviderTests
{
    private Sha256HashProvider _hashProvider = null!;

    [Before(Test)]
    public void Setup()
    {
        _hashProvider = new Sha256HashProvider();
    }

    [Test]
    public async Task ComputeHash_ReturnsCorrectHash_ForEmptyData()
    {
        // Arrange
        var emptyData = Array.Empty<byte>();
        
        // Expected hash for empty data (SHA-256)
        var expectedHash = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";
        
        // Act
        var hash = _hashProvider.ComputeHash(emptyData);
        
        // Assert
        await Assert.That(hash).IsEqualTo(expectedHash);
    }

    [Test]
    public async Task ComputeHash_ReturnsCorrectHash_ForSimpleData()
    {
        // Arrange
        var data = Encoding.UTF8.GetBytes("test data");
        
        // Expected hash for "test data" (SHA-256)
        var expectedHash = "916f0027a575074ce72a331777c3478d6513f786a591bd892da1a577bf2335f9";
        
        // Act
        var hash = _hashProvider.ComputeHash(data);
        
        // Assert
        await Assert.That(hash).IsEqualTo(expectedHash);
    }

    [Test]
    public async Task ComputeHash_ReturnsConsistentHash_ForSameData()
    {
        // Arrange
        var data1 = Encoding.UTF8.GetBytes("consistent data");
        var data2 = Encoding.UTF8.GetBytes("consistent data");
        
        // Act
        var hash1 = _hashProvider.ComputeHash(data1);
        var hash2 = _hashProvider.ComputeHash(data2);
        
        // Assert
        await Assert.That(hash1).IsEqualTo(hash2);
    }

    [Test]
    public async Task ComputeHash_ReturnsDifferentHash_ForDifferentData()
    {
        // Arrange
        var data1 = Encoding.UTF8.GetBytes("data1");
        var data2 = Encoding.UTF8.GetBytes("data2");
        
        // Act
        var hash1 = _hashProvider.ComputeHash(data1);
        var hash2 = _hashProvider.ComputeHash(data2);
        
        // Assert
        await Assert.That(hash1).IsNotEqualTo(hash2);
    }

    [Test]
    public async Task ComputeHash_HandlesLargeData()
    {
        // Arrange
        var largeData = new byte[1024 * 1024]; // 1MB
        new Random(42).NextBytes(largeData); // Fill with random data
        
        // Act - should not throw exceptions or have memory issues
        var hash = _hashProvider.ComputeHash(largeData);
        
        // Assert
        await Assert.That(hash).IsNotNull();
        await Assert.That(hash.Length).IsEqualTo(64); // SHA-256 hash is 64 hex chars
    }
}