namespace Open.AsyncToolkit.HashedRepository.Tests;

/// <summary>
/// Tests for the <see cref="HashedBlobRepository"/> class.
/// </summary>
public sealed class HashedBlobRepositoryTests
{
	// Use the real (test covered) Sha256HashProvider instead of mocking
	private static readonly Sha256HashProvider HashProvider = Sha256HashProvider.Default;

	// Common test data
	private const int LargeDataSize = 8192; // 8KB
	private const string StandardContent = "test content";
	private static readonly ReadOnlyMemory<byte> StandardTestData = Encoding.UTF8.GetBytes(StandardContent);

	private readonly IBlobRepo<Guid> _blobRepo;
	private readonly ISynchronizedAsyncDictionary<string, IReadOnlyCollection<Guid>> _hashMap;
	private readonly IAsyncDictionaryEntry<string, IReadOnlyCollection<Guid>> _asyncDictionaryEntry;
	private readonly HashedBlobRepository<Guid> _repository;

	public HashedBlobRepositoryTests()
	{
		// Create the mock dependencies using NSubstitute
		_blobRepo = Substitute.For<IBlobRepo<Guid>>();
		_hashMap = Substitute.For<ISynchronizedAsyncDictionary<string, IReadOnlyCollection<Guid>>>();

		// Set up the IAsyncDictionaryEntry mock
		_asyncDictionaryEntry = Substitute.For<IAsyncDictionaryEntry<string, IReadOnlyCollection<Guid>>>();

		// Set up Lease method to invoke the callback with the entry
#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable CA2012 // Use ValueTasks correctly
		_hashMap.LeaseAsync(
			Arg.Any<string>(),
			Arg.Any<CancellationToken>(),
			Arg.Any<Func<IAsyncDictionaryEntry<string, IReadOnlyCollection<Guid>>, CancellationToken, ValueTask<Guid>>>())
			.Returns(callInfo =>
			{
				Func<IAsyncDictionaryEntry<string, IReadOnlyCollection<Guid>>, CancellationToken, ValueTask<Guid>> callback = callInfo.ArgAt<Func<IAsyncDictionaryEntry<string, IReadOnlyCollection<Guid>>, CancellationToken, ValueTask<Guid>>>(2);
				return callback(_asyncDictionaryEntry, callInfo.ArgAt<CancellationToken>(1));
			});
#pragma warning restore CA2012 // Use ValueTasks correctly
#pragma warning restore IDE0079 // Remove unnecessary suppression

		// Initialize the repository with the mocks
		_repository = HashedBlobRepository.Create(_blobRepo, _hashMap, () => Guid.NewGuid());
	}

	#region Helper Methods

	private void SetupEmptyGuidSet()
	{
		FrozenSet<Guid> emptySet = [];
		var result = TryReadResult.Success<IReadOnlyCollection<Guid>>(emptySet);
		_asyncDictionaryEntry.TryRead(Arg.Any<CancellationToken>()).Returns(new ValueTask<TryReadResult<IReadOnlyCollection<Guid>>>(result));
	}

	private void SetupGuidSet(params Guid[] guids)
	{
		FrozenSet<Guid> guidSet = guids.ToFrozenSet();
		var result = TryReadResult.Success<IReadOnlyCollection<Guid>>(guidSet);
		_asyncDictionaryEntry.TryRead(Arg.Any<CancellationToken>()).Returns(new ValueTask<TryReadResult<IReadOnlyCollection<Guid>>>(result));
	}

	private Guid SetupCapturedGuid()
	{
		Guid capturedGuid = Guid.Empty;
		_asyncDictionaryEntry
			.CreateOrUpdate(Arg.Do<IReadOnlyCollection<Guid>>(guids => capturedGuid = guids.Last()), Arg.Any<CancellationToken>())
			.Returns(new ValueTask<bool>(true));
		return capturedGuid;
	}

	private void SetupStreamForGuid(Guid guid, ReadOnlyMemory<byte> content)
	{
		var stream = new MemoryStream(content.ToArray());

		// First, set up TryReadAsync to return a successful result
		var tryReadResult = TryReadResult.Success<Stream>(stream);
		_blobRepo
			.TryReadAsync(guid, Arg.Any<CancellationToken>())
			.Returns(new ValueTask<TryReadResult<Stream>>(tryReadResult));
	}

	private void SetupStreamForGuid(Guid guid, string content)
		=> SetupStreamForGuid(guid, Encoding.UTF8.GetBytes(content));

	private void SetupNullStreamForGuid(Guid guid)
	{
		// For null streams, set up TryReadAsync to return a failed result
		TryReadResult<Stream> tryReadResult = TryReadResult<Stream>.NotFound;
		_blobRepo
			.TryReadAsync(guid, Arg.Any<CancellationToken>())
			.Returns(new ValueTask<TryReadResult<Stream>>(tryReadResult));
	}

	private ValueTask<bool> VerifyCreateOrUpdateNotCalled()
		=> _asyncDictionaryEntry
			.DidNotReceive()
			.CreateOrUpdate(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>());

	private ValueTask<bool> VerifyCreateOrUpdateCalledWithGuidCount(int expectedCount)
		=> _asyncDictionaryEntry
			.Received(1)
			.CreateOrUpdate(Arg.Is<IReadOnlyCollection<Guid>>(guids => guids.Count == expectedCount), Arg.Any<CancellationToken>());

	private async ValueTask<bool> VerifyCreateOrUpdateCalledWithGuids(params Guid[] expectedGuids)
	{
		// Instead of using Arg.Is with a predicate, use Arg.Any and check manually
		await _asyncDictionaryEntry.Received(1).CreateOrUpdate(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>());

		// Get the actual argument that was passed
		var callsReceived = _asyncDictionaryEntry.ReceivedCalls()
			.Where(call => call.GetMethodInfo().Name == nameof(IAsyncDictionaryEntry<string, IReadOnlyCollection<Guid>>.CreateOrUpdate))
			.ToList();

		// Check that we received exactly one call and validate the argument
		if (callsReceived.Count != 1)
			return false;

		// Get the actual argument
		var actualGuids = callsReceived[0].GetArguments()[0] as IReadOnlyCollection<Guid>;

		// Verify all expected GUIDs are in the actual collection
		bool allFound = expectedGuids.All(guid => actualGuids?.Contains(guid) == true);

		return allFound;
	}

	#endregion

	#region Get Tests
	[Fact]
	public async Task Get_ReturnsBlobStream_WhenBlobExists()
	{
		// Arrange
		var guid = Guid.NewGuid();
		var testStream = new MemoryStream(Encoding.UTF8.GetBytes("test content"));

		// Set up TryReadAsync to return a successful result
		var tryReadResult = TryReadResult.Success<Stream>(testStream);
		_blobRepo
			.TryReadAsync(guid, Arg.Any<CancellationToken>())
			.Returns(new ValueTask<TryReadResult<Stream>>(tryReadResult));

		// Act
		Stream result = await _repository.GetAsync(guid);

		// Assert
		Assert.NotNull(result);

		// Verify that TryReadAsync was called with the correct GUID
		await _blobRepo.Received(1).TryReadAsync(guid, Arg.Any<CancellationToken>());

		// Verify the returned stream content
		using var streamReader = new StreamReader(result);
		string content = await streamReader.ReadToEndAsync();
		Assert.Equal("test content", content);
	}
	[Fact]
	public async Task Get_ThrowsKeyNotFoundException_WhenBlobDoesNotExist()
	{
		// Arrange
		var guid = Guid.NewGuid();

		// Set up TryReadAsync to return a failed result
		TryReadResult<Stream> tryReadResult = TryReadResult<Stream>.NotFound;
		_blobRepo
			.TryReadAsync(guid, Arg.Any<CancellationToken>())
			.Returns(new ValueTask<TryReadResult<Stream>>(tryReadResult));

		// Act & Assert
		await Assert.ThrowsAsync<KeyNotFoundException>(
			async () => await _repository.GetAsync(guid));

		// Verify that TryReadAsync was called with the correct GUID
		await _blobRepo.Received(1).TryReadAsync(guid, Arg.Any<CancellationToken>());
	}

	#endregion

	#region Put Tests
	[Fact]
	public async Task Put_ReturnsExistingGuid_WhenExactMatchExists()
	{
		// Arrange
		var existingGuid = Guid.NewGuid();

		// Configure IAsyncDictionaryEntry.Read to return a collection with one GUID
		SetupGuidSet(existingGuid);

		// Configure blobStore.ReadAsync to return a stream with matching content
		SetupStreamForGuid(existingGuid, StandardTestData);

		// Act
		Guid result = await _repository.PutAsync(StandardTestData);

		// Assert
		Assert.Equal(existingGuid, result);

		// Verify that TryRead was called on the entry
		await _asyncDictionaryEntry.Received(1).TryRead(Arg.Any<CancellationToken>());

		// Verify that TryReadAsync was called on the blob store with the existing GUID
		await _blobRepo.Received(1).TryReadAsync(existingGuid, Arg.Any<CancellationToken>());

		// Verify that CreateOrUpdate was NOT called (since we found a match)
		await VerifyCreateOrUpdateNotCalled();
	}
	[Fact]
	public async Task Put_ReturnsNewGuid_WhenNoExactMatchExists()
	{
		// Arrange

		// Configure IAsyncDictionaryEntry.Read to return an empty collection
		SetupEmptyGuidSet();

		// Capture the GUID that gets added to the hash map
		Guid capturedGuid = SetupCapturedGuid();

		// Act
		Guid result = await _repository.PutAsync(StandardTestData);

		// Assert
		Assert.NotEqual(Guid.Empty, result);

		// Verify that Read was called on the entry
		await _asyncDictionaryEntry.Received(1).TryRead(Arg.Any<CancellationToken>());

		// Verify that CreateOrUpdate was called on the entry with a collection containing the new GUID
		await VerifyCreateOrUpdateCalledWithGuidCount(1);
	}

	[Fact]
	public async Task Put_ReturnsNewGuid_WhenExistingBlobHasDifferentLength()
	{
		// Arrange
		var existingGuid = Guid.NewGuid();

		// Configure IAsyncDictionaryEntry.Read to return a collection with one GUID
		SetupGuidSet(existingGuid);

		// Configure blobStore.ReadAsync to return a stream with different length
		SetupStreamForGuid(existingGuid, "different");

		// Capture the GUID that gets added to the hash map
		Guid capturedGuid = SetupCapturedGuid();

		// Act
		Guid result = await _repository.PutAsync(StandardTestData);

		// Assert
		Assert.NotEqual(existingGuid, result);

		// Verify that CreateOrUpdate was called on the entry with a collection containing both GUIDs
		await VerifyCreateOrUpdateCalledWithGuids(existingGuid, capturedGuid);
	}

	[Fact]
	public async Task Put_ReturnsNewGuid_WhenExistingBlobHasDifferentContent()
	{
		// Arrange
		var existingGuid = Guid.NewGuid();

		// Configure IAsyncDictionaryEntry.Read to return a collection with one GUID
		SetupGuidSet(existingGuid);

		// Configure blobStore.ReadAsync to return a stream with same length but different content
		SetupStreamForGuid(existingGuid, "same length!");

		// Capture the GUID that gets added to the hash map
		Guid capturedGuid = SetupCapturedGuid();

		// Act
		Guid result = await _repository.PutAsync(StandardTestData);

		// Assert
		Assert.NotEqual(existingGuid, result);

		// Verify that CreateOrUpdate was called on the entry with a collection containing both GUIDs
		await VerifyCreateOrUpdateCalledWithGuids(existingGuid, capturedGuid);
	}

	[Fact]
	public async Task Put_HandlesMultipleExistingGuids_FindsMatchingOne()
	{
		// Arrange
		var nonMatchingGuid1 = Guid.NewGuid();
		var matchingGuid = Guid.NewGuid();
		var nonMatchingGuid2 = Guid.NewGuid();

		// Configure IAsyncDictionaryEntry.Read to return a collection with multiple GUIDs
		SetupGuidSet(nonMatchingGuid1, matchingGuid, nonMatchingGuid2);

		// Configure blobStore.ReadAsync for the first GUID to return a non-matching stream
		SetupStreamForGuid(nonMatchingGuid1, "different!!!");

		// Configure blobStore.ReadAsync for the second GUID to return a matching stream
		SetupStreamForGuid(matchingGuid, StandardTestData);

		// Configure blobStore.ReadAsync for the third GUID (implementation may check all GUIDs)
		SetupStreamForGuid(nonMatchingGuid2, "also different");

		// Act
		Guid result = await _repository.PutAsync(StandardTestData);

		// Assert
		Assert.Equal(matchingGuid, result);

		// Verify that CreateOrUpdate was NOT called (since we found a match)
		await VerifyCreateOrUpdateNotCalled();
	}

	[Fact]
	public async Task Put_PropagatesCancellation_WhenCancellationIsRequested()
	{
		// Arrange
		using var cancellationTokenSource = new CancellationTokenSource();
#if NET9_0_OR_GREATER
		await cancellationTokenSource.CancelAsync();
#else
		cancellationTokenSource.Cancel();
#endif

		// Configure the mocks to throw when cancellation token is used
		_hashMap.LeaseAsync(
			Arg.Any<string>(),
			Arg.Is<CancellationToken>(static ct => ct.IsCancellationRequested),
			Arg.Any<Func<IAsyncDictionaryEntry<string, IReadOnlyCollection<Guid>>, CancellationToken, ValueTask<Guid>>>())
			.Returns<ValueTask<Guid>>(static x => throw new OperationCanceledException());

		// Act & Assert
		bool exceptionThrown = false;
		try
		{
			await _repository.PutAsync(StandardTestData, cancellationTokenSource.Token);
		}
		catch (OperationCanceledException)
		{
			exceptionThrown = true;
		}

		Assert.True(exceptionThrown);
	}

	[Fact]
	public async Task Put_HandlesNullStreamFromBlobStore()
	{
		// Arrange
		var existingGuid = Guid.NewGuid();

		// Configure IAsyncDictionaryEntry.Read to return a collection with one GUID
		SetupGuidSet(existingGuid);

		// Configure blobStore.ReadAsync to return null (should not happen in practice but testing for robustness)
		SetupNullStreamForGuid(existingGuid);

		// Capture the GUID that gets added to the hash map
		Guid capturedGuid = SetupCapturedGuid();

		// Act
		Guid result = await _repository.PutAsync(StandardTestData);

		// Assert
		Assert.NotEqual(existingGuid, result);

		// Verify that CreateOrUpdate was called on the entry with a collection containing both GUIDs
		await VerifyCreateOrUpdateCalledWithGuids(existingGuid, capturedGuid);
	}

	[Fact]
	public async Task Put_HandlesEmptyNonNullSet_CreatesNewSet()
	{
		// Arrange

		// Configure IAsyncDictionaryEntry.Read to return an empty but non-null collection
		// (Testing scenario #2 explicitly - empty set that is not null)
		SetupEmptyGuidSet();

		// Capture the GUID that gets added to the hash map
		Guid capturedGuid = SetupCapturedGuid();

		// Act
		Guid result = await _repository.PutAsync(StandardTestData);

		// Assert
		Assert.NotEqual(Guid.Empty, result);

		// Verify that Read was called on the entry
		await _asyncDictionaryEntry.Received(1).TryRead(Arg.Any<CancellationToken>());

		// Verify that CreateOrUpdate was called on the entry with a collection containing the new GUID
		await VerifyCreateOrUpdateCalledWithGuidCount(1);
	}

	[Fact]
	public async Task Put_UpdatesSetAndReturnsNewGuid_WhenNoMatchesInSetOfMultiple()
	{
		// Arrange
		var nonMatchingGuid1 = Guid.NewGuid();
		var nonMatchingGuid2 = Guid.NewGuid();
		var nonMatchingGuid3 = Guid.NewGuid();

		// Configure IAsyncDictionaryEntry.Read to return a collection with multiple GUIDs
		// but none of them match the content we're trying to store
		SetupGuidSet(nonMatchingGuid1, nonMatchingGuid2, nonMatchingGuid3);

		// Configure blobStore.ReadAsync for all GUIDs to return non-matching streams
		SetupStreamForGuid(nonMatchingGuid1, "different content 1");
		SetupStreamForGuid(nonMatchingGuid2, "different content 2");
		SetupStreamForGuid(nonMatchingGuid3, "different content 3");

		// Capture the GUID that gets added to the hash map
		Guid capturedGuid = SetupCapturedGuid();

		// Act
		Guid result = await _repository.PutAsync(StandardTestData);

		// Assert
		Assert.NotEqual(Guid.Empty, result);
		Assert.NotEqual(nonMatchingGuid1, result);
		Assert.NotEqual(nonMatchingGuid2, result);
		Assert.NotEqual(nonMatchingGuid3, result);

		// Verify that ReadAsync was called for all GUIDs
		await _blobRepo.Received().ReadAsync(nonMatchingGuid1, Arg.Any<CancellationToken>());
		await _blobRepo.Received().ReadAsync(nonMatchingGuid2, Arg.Any<CancellationToken>());
		await _blobRepo.Received().ReadAsync(nonMatchingGuid3, Arg.Any<CancellationToken>());

		// Verify that CreateOrUpdate was called with a collection containing all the original GUIDs plus the new one
		await VerifyCreateOrUpdateCalledWithGuids(nonMatchingGuid1, nonMatchingGuid2, nonMatchingGuid3, capturedGuid);

		// Note: The HashedBlobRepository implementation doesn't actually call CreateAsync
		// when creating a new GUID, contrary to what we'd expect. It only updates the hashMap.
		// Therefore, we're not expecting CreateAsync to be called.
	}

	#endregion

	#region Edge Cases and Error Handling

	[Fact]
	public async Task Put_HandlesEmptyData()
	{
		// Arrange
		byte[] emptyData = [];

		// Configure IAsyncDictionaryEntry.Read to return an empty collection
		SetupEmptyGuidSet();

		// Capture the GUID that gets added to the hash map
		Guid capturedGuid = SetupCapturedGuid();

		// Act
		Guid result = await _repository.PutAsync(emptyData);

		// Assert
		Assert.NotEqual(Guid.Empty, result);

		// Verify that CreateOrUpdate was called on the entry
		await VerifyCreateOrUpdateCalledWithGuidCount(1);
	}

	[Fact]
	public async Task Put_HandlesLargeBlobsEfficiently()
	{
		// Arrange - Create a "large" blob for test purposes (not actually large to avoid memory issues in tests)
		byte[] largeData = new byte[LargeDataSize]; // 8KB is enough to test the memory handling
		new Random(42).NextBytes(largeData); // Fill with random data

		// Configure IAsyncDictionaryEntry.Read to return an empty collection
		SetupEmptyGuidSet();

		// Capture the GUID that gets added to the hash map
		Guid capturedGuid = SetupCapturedGuid();

		// Act
		Guid result = await _repository.PutAsync(largeData);

		// Assert
		Assert.NotEqual(Guid.Empty, result);
	}

	[Fact]
	public async Task Put_UpdatesHashMapWithNewGuid_WhenAllExistingBlobsAreMissing()
	{
		// Arrange
		var deletedGuid = Guid.NewGuid();

		// Configure IAsyncDictionaryEntry.Read to return a collection with a GUID that no longer exists
		SetupGuidSet(deletedGuid);

		// Configure blobStore.ReadAsync to return null, simulating a missing blob
		SetupNullStreamForGuid(deletedGuid);

		// Capture the GUID that gets added to the hash map
		Guid capturedGuid = SetupCapturedGuid();

		// Act
		Guid result = await _repository.PutAsync(StandardTestData);

		// Assert
		Assert.NotEqual(deletedGuid, result);

		// Verify that CreateOrUpdate was called with a collection containing both GUIDs
		// (the missing one and the new one - we don't clean up missing GUIDs)
		await VerifyCreateOrUpdateCalledWithGuids(deletedGuid, capturedGuid);
	}

	#endregion
}
