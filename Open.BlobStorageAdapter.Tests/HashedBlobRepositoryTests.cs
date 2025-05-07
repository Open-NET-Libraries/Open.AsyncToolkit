using System.Buffers;
using System.Collections.Frozen;
using System.Text;
using NSubstitute;

namespace Open.BlobStorageAdapter.Tests;

/// <summary>
/// Tests for the <see cref="HashedBlobRepository"/> class.
/// </summary>
public class HashedBlobRepositoryTests
{
    private IBlobRepo<Guid> _blobRepo = null!;
    private IAsyncDictionary<string, IReadOnlyCollection<Guid>> _hashMap = null!;
    private IHashProvider _hashProvider = null!;
    private HashedBlobRepository _repository = null!;
    private AsyncDictionaryEntry<string, IReadOnlyCollection<Guid>> _asyncDictionaryEntry = null!;
    private string _testHash = null!;

    [Before(Test)]
    public void Setup()
    {
        // Create the mock dependencies using NSubstitute
        _blobRepo = Substitute.For<IBlobRepo<Guid>>();
        _hashMap = Substitute.For<IAsyncDictionary<string, IReadOnlyCollection<Guid>>>();
        
        // Use the real Sha256HashProvider instead of mocking
        _hashProvider = new Sha256HashProvider();
        
        // Use the real hash provider to generate a test hash
        var testData = Encoding.UTF8.GetBytes("test content");
        _testHash = _hashProvider.ComputeHash(testData);
        
        // Set up the AsyncDictionaryEntry mock
        _asyncDictionaryEntry = Substitute.For<AsyncDictionaryEntry<string, IReadOnlyCollection<Guid>>>(
            _testHash, _hashMap);
        
        // Set up Lease method to invoke the callback with the entry
        _hashMap.Lease(
            Arg.Any<string>(), 
            Arg.Any<CancellationToken>(), 
            Arg.Any<Func<AsyncDictionaryEntry<string, IReadOnlyCollection<Guid>>, CancellationToken, ValueTask<Guid>>>())
            .Returns(callInfo => 
            {
                var callback = callInfo.ArgAt<Func<AsyncDictionaryEntry<string, IReadOnlyCollection<Guid>>, CancellationToken, ValueTask<Guid>>>(2);
                return callback(_asyncDictionaryEntry, callInfo.ArgAt<CancellationToken>(1));
            });

        // Initialize the repository with the mocks
        _repository = new HashedBlobRepository(_blobRepo, _hashMap, _hashProvider);
    }

    #region Get Tests

    [Test]
    public async Task Get_ReturnsBlobStream_WhenBlobExists()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var testStream = new MemoryStream(Encoding.UTF8.GetBytes("test content"));
        
        _blobRepo.ReadAsync(guid, Arg.Any<CancellationToken>())
            .Returns(testStream);

        // Act
        var result = await _repository.Get(guid);

        // Assert
        await Assert.That(result).IsNotNull();
        
        // Verify that ReadAsync was called with the correct GUID
        await _blobRepo.Received(1).ReadAsync(guid, Arg.Any<CancellationToken>());
        
        // Verify the returned stream content
        using var streamReader = new StreamReader(result);
        var content = await streamReader.ReadToEndAsync();
        await Assert.That(content).IsEqualTo("test content");
    }

    [Test]
    public async Task Get_ThrowsKeyNotFoundException_WhenBlobDoesNotExist()
    {
        // Arrange
        var guid = Guid.NewGuid();
        
        _blobRepo.ReadAsync(guid, Arg.Any<CancellationToken>())
            .Returns((Stream?)null);

        // Act & Assert
        await ((Func<Task>)(async () => await _repository.Get(guid)))
            .ThrowsAsync<KeyNotFoundException>();
        
        // Verify that ReadAsync was called with the correct GUID
        await _blobRepo.Received(1).ReadAsync(guid, Arg.Any<CancellationToken>());
    }

    #endregion

    #region Put Tests

    [Test]
    public async Task Put_ReturnsExistingGuid_WhenExactMatchExists()
    {
        // Arrange
        var existingGuid = Guid.NewGuid();
        var testData = Encoding.UTF8.GetBytes("test content");
        
        // Configure AsyncDictionaryEntry.Read to return a collection with one GUID
        _asyncDictionaryEntry.Read().Returns(new[] { existingGuid }.ToFrozenSet());
        
        // Configure blobStore.ReadAsync to return a stream with matching content
        var testStream = new MemoryStream(testData);
        _blobRepo.ReadAsync(existingGuid, Arg.Any<CancellationToken>())
            .Returns(testStream);
        
        // Act
        var result = await _repository.Put(testData);

        // Assert
        await Assert.That(result).IsEqualTo(existingGuid);
        
        // Verify that Read was called on the entry
        await _asyncDictionaryEntry.Received(1).Read();
        
        // Verify that ReadAsync was called on the blob store with the existing GUID
        await _blobRepo.Received(1).ReadAsync(existingGuid, Arg.Any<CancellationToken>());
        
        // Verify that Update was NOT called (since we found a match)
        await _asyncDictionaryEntry.DidNotReceive().Update(Arg.Any<IReadOnlyCollection<Guid>>());
    }

    [Test]
    public async Task Put_ReturnsNewGuid_WhenNoExactMatchExists()
    {
        // Arrange
        var testData = Encoding.UTF8.GetBytes("test content");
        
        // Configure AsyncDictionaryEntry.Read to return an empty collection
        _asyncDictionaryEntry.Read().Returns(FrozenSet<Guid>.Empty);
        
        // Capture the GUID that gets added to the hash map
        Guid capturedGuid = Guid.Empty;
        _asyncDictionaryEntry.Update(Arg.Do<IReadOnlyCollection<Guid>>(guids => 
            capturedGuid = guids.First()))
            .Returns(true);
        
        // Act
        var result = await _repository.Put(testData);

        // Assert
        await Assert.That(result).IsNotEqualTo(Guid.Empty);
        
        // Verify that Read was called on the entry
        await _asyncDictionaryEntry.Received(1).Read();
        
        // Verify that Update was called on the entry with a collection containing the new GUID
        await _asyncDictionaryEntry.Received(1).Update(Arg.Any<IReadOnlyCollection<Guid>>());
    }

    [Test]
    public async Task Put_ReturnsNewGuid_WhenExistingBlobHasDifferentLength()
    {
        // Arrange
        var existingGuid = Guid.NewGuid();
        var testData = Encoding.UTF8.GetBytes("test content");
        
        // Configure AsyncDictionaryEntry.Read to return a collection with one GUID
        _asyncDictionaryEntry.Read().Returns(new[] { existingGuid }.ToFrozenSet());
        
        // Configure blobStore.ReadAsync to return a stream with different length
        var differentLengthStream = new MemoryStream(Encoding.UTF8.GetBytes("different"));
        _blobRepo.ReadAsync(existingGuid, Arg.Any<CancellationToken>())
            .Returns(differentLengthStream);
        
        // Capture the GUID that gets added to the hash map
        Guid capturedGuid = Guid.Empty;
        _asyncDictionaryEntry.Update(Arg.Do<IReadOnlyCollection<Guid>>(guids => 
            capturedGuid = guids.Last()))
            .Returns(true);
        
        // Act
        var result = await _repository.Put(testData);

        // Assert
        await Assert.That(result).IsNotEqualTo(existingGuid);
        
        // Verify that Update was called on the entry with a collection containing both GUIDs
        await _asyncDictionaryEntry.Received(1).Update(Arg.Is<IReadOnlyCollection<Guid>>(
            guids => guids.Count == 2 && guids.Contains(existingGuid)));
    }

    [Test]
    public async Task Put_ReturnsNewGuid_WhenExistingBlobHasDifferentContent()
    {
        // Arrange
        var existingGuid = Guid.NewGuid();
        var testData = Encoding.UTF8.GetBytes("test content");
        
        // Configure AsyncDictionaryEntry.Read to return a collection with one GUID
        _asyncDictionaryEntry.Read().Returns(new[] { existingGuid }.ToFrozenSet());
        
        // Configure blobStore.ReadAsync to return a stream with same length but different content
        var differentContentStream = new MemoryStream(Encoding.UTF8.GetBytes("same length!"));
        _blobRepo.ReadAsync(existingGuid, Arg.Any<CancellationToken>())
            .Returns(differentContentStream);
        
        // Capture the GUID that gets added to the hash map
        Guid capturedGuid = Guid.Empty;
        _asyncDictionaryEntry.Update(Arg.Do<IReadOnlyCollection<Guid>>(guids => 
            capturedGuid = guids.Last()))
            .Returns(true);
        
        // Act
        var result = await _repository.Put(testData);

        // Assert
        await Assert.That(result).IsNotEqualTo(existingGuid);
        
        // Verify that Update was called on the entry with a collection containing both GUIDs
        await _asyncDictionaryEntry.Received(1).Update(Arg.Is<IReadOnlyCollection<Guid>>(
            guids => guids.Count == 2 && guids.Contains(existingGuid)));
    }

    [Test]
    public async Task Put_HandlesMultipleExistingGuids_FindsMatchingOne()
    {
        // Arrange
        var nonMatchingGuid1 = Guid.NewGuid();
        var matchingGuid = Guid.NewGuid();
        var nonMatchingGuid2 = Guid.NewGuid();
        var testData = Encoding.UTF8.GetBytes("test content");
        
        // Configure AsyncDictionaryEntry.Read to return a collection with multiple GUIDs
        _asyncDictionaryEntry.Read().Returns(new[] { nonMatchingGuid1, matchingGuid, nonMatchingGuid2 }.ToFrozenSet());
        
        // Configure blobStore.ReadAsync for the first GUID to return a non-matching stream
        var nonMatchingStream1 = new MemoryStream(Encoding.UTF8.GetBytes("different!!!"));
        _blobRepo.ReadAsync(nonMatchingGuid1, Arg.Any<CancellationToken>())
            .Returns(nonMatchingStream1);
        
        // Configure blobStore.ReadAsync for the second GUID to return a matching stream
        var matchingStream = new MemoryStream(testData);
        _blobRepo.ReadAsync(matchingGuid, Arg.Any<CancellationToken>())
            .Returns(matchingStream);
        
        // Configure blobStore.ReadAsync for the third GUID (implementation may check all GUIDs)
        var nonMatchingStream2 = new MemoryStream(Encoding.UTF8.GetBytes("also different"));
        _blobRepo.ReadAsync(nonMatchingGuid2, Arg.Any<CancellationToken>())
            .Returns(nonMatchingStream2);
        
        // Act
        var result = await _repository.Put(testData);

        // Assert
        await Assert.That(result).IsEqualTo(matchingGuid);
        
        // Verify that ReadAsync was called for the first and second GUIDs
        await _blobRepo.Received(1).ReadAsync(nonMatchingGuid1, Arg.Any<CancellationToken>());
        await _blobRepo.Received(1).ReadAsync(matchingGuid, Arg.Any<CancellationToken>());
        
        // We now know the implementation checks all GUIDs in the collection,
        // so we should verify the third GUID was accessed as well
        await _blobRepo.Received(1).ReadAsync(nonMatchingGuid2, Arg.Any<CancellationToken>());
        
        // Verify that Update was NOT called (since we found a match)
        await _asyncDictionaryEntry.DidNotReceive().Update(Arg.Any<IReadOnlyCollection<Guid>>());
    }

    [Test]
    public async Task Put_PropagatesCancellation_WhenCancellationIsRequested()
    {
        // Arrange
        var testData = Encoding.UTF8.GetBytes("test content");
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        // Act & Assert
        await ((Func<Task>)(async () => 
            await _repository.Put(testData, cancellationTokenSource.Token)))
            .ThrowsAsync<OperationCanceledException>();
    }

    [Test]
    public async Task Put_HandlesNullStreamFromBlobStore()
    {
        // Arrange
        var existingGuid = Guid.NewGuid();
        var testData = Encoding.UTF8.GetBytes("test content");
        
        // Configure AsyncDictionaryEntry.Read to return a collection with one GUID
        _asyncDictionaryEntry.Read().Returns(new[] { existingGuid }.ToFrozenSet());
        
        // Configure blobStore.ReadAsync to return null (should not happen in practice but testing for robustness)
        _blobRepo.ReadAsync(existingGuid, Arg.Any<CancellationToken>())
            .Returns((Stream?)null);
        
        // Capture the GUID that gets added to the hash map
        Guid capturedGuid = Guid.Empty;
        _asyncDictionaryEntry.Update(Arg.Do<IReadOnlyCollection<Guid>>(guids => 
            capturedGuid = guids.Last()))
            .Returns(true);
        
        // Act
        var result = await _repository.Put(testData);

        // Assert
        await Assert.That(result).IsNotEqualTo(existingGuid);
        
        // Verify that Update was called on the entry with a collection containing both GUIDs
        await _asyncDictionaryEntry.Received(1).Update(Arg.Is<IReadOnlyCollection<Guid>>(
            guids => guids.Count == 2 && guids.Contains(existingGuid)));
    }

    #endregion

    #region Edge Cases and Error Handling

    [Test]
    public async Task Put_HandlesEmptyData()
    {
        // Arrange
        var emptyData = Array.Empty<byte>();
        
        // Configure AsyncDictionaryEntry.Read to return an empty collection
        _asyncDictionaryEntry.Read().Returns(FrozenSet<Guid>.Empty);
        
        // Capture the GUID that gets added to the hash map
        Guid capturedGuid = Guid.Empty;
        _asyncDictionaryEntry.Update(Arg.Do<IReadOnlyCollection<Guid>>(guids => 
            capturedGuid = guids.First()))
            .Returns(true);
        
        // Act
        var result = await _repository.Put(emptyData);

        // Assert
        await Assert.That(result).IsNotEqualTo(Guid.Empty);
        
        // Verify that Update was called on the entry
        await _asyncDictionaryEntry.Received(1).Update(Arg.Any<IReadOnlyCollection<Guid>>());
    }

    [Test]
    public async Task Put_HandlesLargeBlobsEfficiently()
    {
        // Arrange - Create a "large" blob for test purposes (not actually large to avoid memory issues in tests)
        var largeData = new byte[8192]; // 8KB is enough to test the memory handling
        new Random(42).NextBytes(largeData); // Fill with random data
        
        // Configure AsyncDictionaryEntry.Read to return an empty collection
        _asyncDictionaryEntry.Read().Returns(FrozenSet<Guid>.Empty);
        
        // Capture the GUID that gets added to the hash map
        _asyncDictionaryEntry.Update(Arg.Any<IReadOnlyCollection<Guid>>())
            .Returns(true);
        
        // Act
        var result = await _repository.Put(largeData);

        // Assert
        await Assert.That(result).IsNotEqualTo(Guid.Empty);
    }

    [Test]
    public async Task Put_UpdatesHashMapWithNewGuid_WhenAllExistingBlobsAreMissing()
    {
        // Arrange
        var deletedGuid = Guid.NewGuid();
        var testData = Encoding.UTF8.GetBytes("test content");
        
        // Configure AsyncDictionaryEntry.Read to return a collection with a GUID that no longer exists
        _asyncDictionaryEntry.Read().Returns(new[] { deletedGuid }.ToFrozenSet());
        
        // Configure blobStore.ReadAsync to return null, simulating a missing blob
        _blobRepo.ReadAsync(deletedGuid, Arg.Any<CancellationToken>())
            .Returns((Stream?)null);
        
        // Capture the GUID that gets added to the hash map
        Guid capturedGuid = Guid.Empty;
        _asyncDictionaryEntry.Update(Arg.Do<IReadOnlyCollection<Guid>>(guids => 
            capturedGuid = guids.Last()))
            .Returns(true);
        
        // Act
        var result = await _repository.Put(testData);

        // Assert
        await Assert.That(result).IsNotEqualTo(deletedGuid);
        
        // Verify that Update was called with a collection containing both GUIDs
        // (the missing one and the new one - we don't clean up missing GUIDs)
        await _asyncDictionaryEntry.Received(1).Update(Arg.Is<IReadOnlyCollection<Guid>>(
            guids => guids.Count == 2 && guids.Contains(deletedGuid)));
    }

    #endregion
}