using System.Text;
using NSubstitute;

namespace Open.BlobStorageAdapter.Tests;

/// <summary>
/// Tests for the <see cref="Sha256HashProvider"/> class.
/// </summary>
public class Sha256HashProviderTests
{
    private Sha256HashProvider _hashProvider = null!;
    
    // Known hash test values
    private static readonly byte[] EmptyData = Array.Empty<byte>();
    private static readonly string EmptyDataHash = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";
    
    private static readonly string SimpleDataText = "test data";
    private static readonly byte[] SimpleData = Encoding.UTF8.GetBytes(SimpleDataText);
    private static readonly string SimpleDataHash = "916f0027a575074ce72a331777c3478d6513f786a591bd892da1a577bf2335f9";
    
    private const int LargeDataSize = 1024 * 1024; // 1MB

    [Before(Test)]
    public void Setup()
    {
        _hashProvider = new Sha256HashProvider();
    }

    [Test]
    public async Task ComputeHash_ReturnsCorrectHash_ForEmptyData()
    {
        // Act
        var hash = _hashProvider.ComputeHash(EmptyData);
        
        // Assert
        await Assert.That(hash).IsEqualTo(EmptyDataHash);
    }

    [Test]
    public async Task ComputeHash_ReturnsCorrectHash_ForSimpleData()
    {
        // Act
        var hash = _hashProvider.ComputeHash(SimpleData);
        
        // Assert
        await Assert.That(hash).IsEqualTo(SimpleDataHash);
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
        var largeData = new byte[LargeDataSize];
        new Random(42).NextBytes(largeData); // Fill with random data
        
        // Act - should not throw exceptions or have memory issues
        var hash = _hashProvider.ComputeHash(largeData);
        
        // Assert
        await Assert.That(hash).IsNotNull();
        await Assert.That(hash.Length).IsEqualTo(64); // SHA-256 hash is 64 hex chars
    }
}