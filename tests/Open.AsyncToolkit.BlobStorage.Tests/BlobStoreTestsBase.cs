namespace Open.AsyncToolkit.BlobStorage.Tests;

/// <summary>
/// Base class for blob storage tests that provides common test methods and utilities.
/// </summary>
/// <typeparam name="TBlobStore">The type of blob store being tested.</typeparam>
internal abstract class BlobStoreTestsBase<TBlobStore>
	where TBlobStore : IBlobStore
{
	// Common test data
	protected const string StandardContent = "Hello, World!";
	protected const string UpdatedContent = "Updated content";

	// The blob store instance to test
	protected TBlobStore BlobStore { get; private set; } = default!;
	protected IBlobStore BlobStoreInterface => BlobStore;

	/// <summary>
	/// Creates and returns a new instance of the blob store for testing.
	/// </summary>
	/// <returns>A new instance of the blob store.</returns>
	protected abstract TBlobStore CreateBlobStore();

	/// <summary>
	/// Cleans up any resources used by the blob store.
	/// </summary>
	protected abstract Task CleanupBlobStoreAsync();

	[Before(Test)]
	public void Setup()
	{
		BlobStore = CreateBlobStore();
	}

	[After(Test)]
	public async Task Teardown()
	{
		await CleanupBlobStoreAsync();
	}

	#region Exists Tests

	[Test]
	public async Task ExistsAsync_ReturnsFalse_WhenBlobDoesNotExist()
	{
		// Act
		bool exists = await BlobStoreInterface.ExistsAsync("nonexistent-key");

		// Assert
		await Assert.That(exists).IsFalse();
	}

	[Test]
	public async Task ExistsAsync_ReturnsTrue_WhenBlobExists()
	{
		// Arrange
		const string key = "test-key";
		await CreateBlobWithContentAsync(key, StandardContent);

		// Act
		bool exists = await BlobStoreInterface.ExistsAsync(key);

		// Assert
		await Assert.That(exists).IsTrue();
	}

	#endregion

	#region Read Tests

	[Test]
	public async Task ReadAsync_ReturnsNull_WhenBlobDoesNotExist()
	{
		// Act
		Stream? stream = await BlobStoreInterface.ReadAsync("nonexistent-key");

		// Assert
		await Assert.That(stream).IsNull();
	}

	[Test]
	public async Task ReadAsync_ReturnsContent_WhenBlobExists()
	{
		// Arrange
		const string key = "test-key-read";
		await CreateBlobWithContentAsync(key, StandardContent);

		// Act
		using Stream? stream = await BlobStoreInterface.ReadAsync(key);

		// Assert
		await Assert.That(stream).IsNotNull();
		string content = await BlobStoreTestsBase<TBlobStore>.ReadStreamContentAsync(stream!);
		await Assert.That(content).IsEqualTo(StandardContent);
	}

	[Test]
	public async Task TryReadAsync_ReturnsNotSuccess_WhenBlobDoesNotExist()
	{
		// Act
		var result = await BlobStoreInterface.TryReadAsync("nonexistent-key");

		// Assert
		await Assert.That(result.Success).IsFalse();
	}

	[Test]
	public async Task TryReadAsync_ReturnsSuccessWithContent_WhenBlobExists()
	{
		// Arrange
		const string key = "test-key-tryread";
		await CreateBlobWithContentAsync(key, StandardContent);

		// Act
		var result = await BlobStoreInterface.TryReadAsync(key);

		// Assert
		await Assert.That(result.Success).IsTrue();
		string content = await BlobStoreTestsBase<TBlobStore>.ReadStreamContentAsync(result.Value);
		await Assert.That(content).IsEqualTo(StandardContent);
	}

	#endregion

	#region Create Tests

	[Test]
	public async Task CreateAsync_ReturnsTrue_WhenBlobDoesNotExist()
	{
		// Arrange
		const string key = "new-key";

		// Act
		bool created = await CreateBlobWithContentAsync(key, StandardContent);

		// Assert
		await Assert.That(created).IsTrue();

		// Verify content
		using Stream? stream = await BlobStoreInterface.ReadAsync(key);
		await Assert.That(stream).IsNotNull();
		string content = await BlobStoreTestsBase<TBlobStore>.ReadStreamContentAsync(stream!);
		await Assert.That(content).IsEqualTo(StandardContent);
	}

	[Test]
	public async Task CreateAsync_WithBytes_ReturnsTrue_WhenBlobDoesNotExist()
	{
		// Arrange
		const string key = "new-key-bytes";
		byte[] bytes = Encoding.UTF8.GetBytes(StandardContent);

		// Act
		bool created = await BlobStoreInterface.CreateAsync(key, bytes);

		// Assert
		await Assert.That(created).IsTrue();

		// Verify content
		using Stream? stream = await BlobStoreInterface.ReadAsync(key);
		await Assert.That(stream).IsNotNull();
		string content = await BlobStoreTestsBase<TBlobStore>.ReadStreamContentAsync(stream!);
		await Assert.That(content).IsEqualTo(StandardContent);
	}

	[Test]
	public async Task CreateAsync_WithBytes_ReturnsFalse_WhenBlobExists()
	{
		// Arrange
		const string key = "existing-key-bytes";
		byte[] initialBytes = Encoding.UTF8.GetBytes(StandardContent);
		byte[] updatedBytes = Encoding.UTF8.GetBytes(UpdatedContent);

		// Create the blob first
		bool initialCreation = await BlobStoreInterface.CreateAsync(key, initialBytes);
		await Assert.That(initialCreation).IsTrue();

		// Act - Try to create again
		bool secondCreation = await BlobStoreInterface.CreateAsync(key, updatedBytes);

		// Assert
		await Assert.That(secondCreation).IsFalse();

		// Verify original content is still there
		using Stream? stream = await BlobStoreInterface.ReadAsync(key);
		await Assert.That(stream).IsNotNull();
		string content = await BlobStoreTestsBase<TBlobStore>.ReadStreamContentAsync(stream!);
		await Assert.That(content).IsEqualTo(StandardContent);
	}

	[Test]
	public async Task CreateAsync_ReturnsFalse_WhenBlobExists()
	{
		// Arrange
		const string key = "existing-key";

		// Create the blob first
		bool initialCreation = await CreateBlobWithContentAsync(key, StandardContent);
		await Assert.That(initialCreation).IsTrue();

		// Act - Try to create again
		bool secondCreation = await CreateBlobWithContentAsync(key, UpdatedContent);

		// Assert
		await Assert.That(secondCreation).IsFalse();

		// Verify original content is still there
		using Stream? stream = await BlobStoreInterface.ReadAsync(key);
		await Assert.That(stream).IsNotNull();
		string content = await BlobStoreTestsBase<TBlobStore>.ReadStreamContentAsync(stream!);
		await Assert.That(content).IsEqualTo(StandardContent);
	}

	#endregion

	#region Update Tests

	[Test]
	public async Task UpdateAsync_ReturnsFalse_WhenBlobDoesNotExist()
	{
		// Act
		bool updated = await UpdateBlobWithContentAsync("nonexistent-key", UpdatedContent);

		// Assert
		await Assert.That(updated).IsFalse();
	}

	[Test]
	public async Task UpdateAsync_WithBytes_ReturnsFalse_WhenBlobDoesNotExist()
	{
		// Arrange
		const string key = "nonexistent-key-bytes";
		byte[] bytes = Encoding.UTF8.GetBytes(UpdatedContent);

		// Act
		bool updated = await BlobStoreInterface.UpdateAsync(key, bytes);

		// Assert
		await Assert.That(updated).IsFalse();
	}

	[Test]
	public async Task UpdateAsync_WithBytes_ReturnsTrue_AndUpdatesContent_WhenBlobExists()
	{
		// Arrange
		const string key = "update-key-bytes";
		byte[] initialBytes = Encoding.UTF8.GetBytes(StandardContent);
		byte[] updatedBytes = Encoding.UTF8.GetBytes(UpdatedContent);

		// Create the blob first
		await BlobStoreInterface.CreateAsync(key, initialBytes);

		// Act
		bool updated = await BlobStoreInterface.UpdateAsync(key, updatedBytes);

		// Assert
		await Assert.That(updated).IsTrue();

		// Verify updated content
		using Stream? stream = await BlobStoreInterface.ReadAsync(key);
		await Assert.That(stream).IsNotNull();
		string content = await BlobStoreTestsBase<TBlobStore>.ReadStreamContentAsync(stream!);
		await Assert.That(content).IsEqualTo(UpdatedContent);
	}

	[Test]
	public async Task UpdateAsync_WithBytes_ReturnsFalse_WhenContentIsUnchanged()
	{
		// Arrange
		const string key = "update-unchanged-bytes";
		byte[] bytes = Encoding.UTF8.GetBytes(StandardContent);

		// Create the blob first
		await BlobStoreInterface.CreateAsync(key, bytes);

		// Act - Try to update with the same content
		bool updated = await BlobStoreInterface.UpdateAsync(key, bytes);

		// Assert - Should return false because content is unchanged
		await Assert.That(updated).IsFalse();

		// Verify content is still there
		using Stream? stream = await BlobStoreInterface.ReadAsync(key);
		await Assert.That(stream).IsNotNull();
		string content = await BlobStoreTestsBase<TBlobStore>.ReadStreamContentAsync(stream!);
		await Assert.That(content).IsEqualTo(StandardContent);
	}

	[Test]
	public async Task UpdateAsync_ReturnsTrue_AndUpdatesContent_WhenBlobExists()
	{
		// Arrange
		const string key = "update-key";

		// Create the blob first
		await CreateBlobWithContentAsync(key, StandardContent);

		// Act
		bool updated = await UpdateBlobWithContentAsync(key, UpdatedContent);

		// Assert
		await Assert.That(updated).IsTrue();

		// Verify updated content
		using Stream? stream = await BlobStoreInterface.ReadAsync(key);
		await Assert.That(stream).IsNotNull();
		string content = await BlobStoreTestsBase<TBlobStore>.ReadStreamContentAsync(stream!);
		await Assert.That(content).IsEqualTo(UpdatedContent);
	}

	#endregion

	#region CreateOrUpdate Tests

	[Test]
	public async Task CreateOrUpdateAsync_CreatesNewBlob_WhenBlobDoesNotExist()
	{
		// Arrange
		const string key = "create-or-update-new";

		// Act
		bool result = await CreateOrUpdateBlobWithContentAsync(key, StandardContent);

		// Assert
		await Assert.That(result).IsTrue();

		// Verify content
		using Stream? stream = await BlobStoreInterface.ReadAsync(key);
		await Assert.That(stream).IsNotNull();
		string content = await BlobStoreTestsBase<TBlobStore>.ReadStreamContentAsync(stream!);
		await Assert.That(content).IsEqualTo(StandardContent);
	}

	[Test]
	public async Task CreateOrUpdateAsync_UpdatesExistingBlob_WhenBlobExists()
	{
		// Arrange
		const string key = "create-or-update-existing";

		// Create the blob first
		await CreateBlobWithContentAsync(key, StandardContent);

		// Act
		bool result = await CreateOrUpdateBlobWithContentAsync(key, UpdatedContent);

		// Assert
		await Assert.That(result).IsTrue();

		// Verify updated content
		using Stream? stream = await BlobStoreInterface.ReadAsync(key);
		await Assert.That(stream).IsNotNull();
		string content = await BlobStoreTestsBase<TBlobStore>.ReadStreamContentAsync(stream!);
		await Assert.That(content).IsEqualTo(UpdatedContent);
	}

	[Test]
	public async Task CreateOrUpdateAsync_WithBytes_CreatesNewBlob_WhenBlobDoesNotExist()
	{
		// Arrange
		const string key = "create-or-update-new-bytes";
		byte[] bytes = Encoding.UTF8.GetBytes(StandardContent);

		// Act
		bool result = await BlobStoreInterface.CreateOrUpdateAsync(key, bytes);

		// Assert
		await Assert.That(result).IsTrue();

		// Verify content
		using Stream? stream = await BlobStoreInterface.ReadAsync(key);
		await Assert.That(stream).IsNotNull();
		string content = await BlobStoreTestsBase<TBlobStore>.ReadStreamContentAsync(stream!);
		await Assert.That(content).IsEqualTo(StandardContent);
	}

	[Test]
	public async Task CreateOrUpdateAsync_WithBytes_UpdatesExistingBlob_WhenBlobExists()
	{
		// Arrange
		const string key = "create-or-update-existing-bytes";
		byte[] initialBytes = Encoding.UTF8.GetBytes(StandardContent);
		byte[] updatedBytes = Encoding.UTF8.GetBytes(UpdatedContent);

		// Create the blob first
		await BlobStoreInterface.CreateAsync(key, initialBytes);

		// Act
		bool result = await BlobStoreInterface.CreateOrUpdateAsync(key, updatedBytes);

		// Assert
		await Assert.That(result).IsTrue();

		// Verify updated content
		using Stream? stream = await BlobStoreInterface.ReadAsync(key);
		await Assert.That(stream).IsNotNull();
		string content = await BlobStoreTestsBase<TBlobStore>.ReadStreamContentAsync(stream!);
		await Assert.That(content).IsEqualTo(UpdatedContent);
	}

	[Test]
	public async Task CreateOrUpdateAsync_WithBytes_ReturnsFalse_WhenContentIsUnchanged()
	{
		// Arrange
		const string key = "create-or-update-unchanged-bytes";
		byte[] bytes = Encoding.UTF8.GetBytes(StandardContent);

		// Create the blob first
		await BlobStoreInterface.CreateAsync(key, bytes);

		// Act - Try to update with the same content
		bool result = await BlobStoreInterface.CreateOrUpdateAsync(key, bytes);

		// Assert - Should return false because content is unchanged
		await Assert.That(result).IsFalse();

		// Verify content is still there
		using Stream? stream = await BlobStoreInterface.ReadAsync(key);
		await Assert.That(stream).IsNotNull();
		string content = await BlobStoreTestsBase<TBlobStore>.ReadStreamContentAsync(stream!);
		await Assert.That(content).IsEqualTo(StandardContent);
	}

	[Test]
	public async Task CreateOrUpdateAsync_WithReadOnlyMemory_CreatesNewBlob_WhenBlobDoesNotExist()
	{
		// Arrange
		const string key = "create-or-update-new-rom";
		ReadOnlyMemory<byte> bytes = Encoding.UTF8.GetBytes(StandardContent);

		// Act
		bool result = await BlobStoreInterface.CreateOrUpdateAsync(key, bytes);

		// Assert
		await Assert.That(result).IsTrue();

		// Verify content
		using Stream? stream = await BlobStoreInterface.ReadAsync(key);
		await Assert.That(stream).IsNotNull();
		string content = await BlobStoreTestsBase<TBlobStore>.ReadStreamContentAsync(stream!);
		await Assert.That(content).IsEqualTo(StandardContent);
	}

	[Test]
	public async Task CreateOrUpdateAsync_WithReadOnlyMemory_UpdatesExistingBlob_WhenBlobExists()
	{
		// Arrange
		const string key = "create-or-update-existing-rom";
		ReadOnlyMemory<byte> initialBytes = Encoding.UTF8.GetBytes(StandardContent);
		ReadOnlyMemory<byte> updatedBytes = Encoding.UTF8.GetBytes(UpdatedContent);

		// Create the blob first
		await BlobStoreInterface.CreateAsync(key, initialBytes);

		// Act
		bool result = await BlobStoreInterface.CreateOrUpdateAsync(key, updatedBytes);

		// Assert
		await Assert.That(result).IsTrue();

		// Verify updated content
		using Stream? stream = await BlobStoreInterface.ReadAsync(key);
		await Assert.That(stream).IsNotNull();
		string content = await BlobStoreTestsBase<TBlobStore>.ReadStreamContentAsync(stream!);
		await Assert.That(content).IsEqualTo(UpdatedContent);
	}

	[Test]
	public async Task CreateOrUpdateAsync_WithReadOnlyMemory_ReturnsFalse_WhenContentIsUnchanged()
	{
		// Arrange
		const string key = "create-or-update-unchanged-rom";
		ReadOnlyMemory<byte> bytes = Encoding.UTF8.GetBytes(StandardContent);

		// Create the blob first
		await BlobStoreInterface.CreateAsync(key, bytes);

		// Act - Try to update with the same content
		bool result = await BlobStoreInterface.CreateOrUpdateAsync(key, bytes);

		// Assert - Should return false because content is unchanged
		await Assert.That(result).IsFalse();

		// Verify content is still there
		using Stream? stream = await BlobStoreInterface.ReadAsync(key);
		await Assert.That(stream).IsNotNull();
		string content = await BlobStoreTestsBase<TBlobStore>.ReadStreamContentAsync(stream!);
		await Assert.That(content).IsEqualTo(StandardContent);
	}

	#endregion

	#region Delete Tests

	[Test]
	public async Task DeleteAsync_ReturnsFalse_WhenBlobDoesNotExist()
	{
		// Act
		bool deleted = await BlobStoreInterface.DeleteAsync("nonexistent-key");

		// Assert
		await Assert.That(deleted).IsFalse();
	}

	[Test]
	public async Task DeleteAsync_ReturnsTrueAndDeletesBlob_WhenBlobExists()
	{
		// Arrange
		const string key = "delete-key";
		await CreateBlobWithContentAsync(key, StandardContent);

		// Verify blob exists
		bool existsBefore = await BlobStoreInterface.ExistsAsync(key);
		await Assert.That(existsBefore).IsTrue();

		// Act
		bool deleted = await BlobStoreInterface.DeleteAsync(key);

		// Assert
		await Assert.That(deleted).IsTrue();

		// Verify blob no longer exists
		bool existsAfter = await BlobStoreInterface.ExistsAsync(key);
		await Assert.That(existsAfter).IsFalse();
	}

	#endregion

	#region Argument Validation Tests
	[Test]
	public async Task Methods_ThrowArgumentNullException_WhenKeyIsNull()
	{
		// Arrange
		string? nullKey = null;
		byte[] validBytes = Encoding.UTF8.GetBytes(StandardContent);

		// Act & Assert
		await Assert.ThrowsAsync<ArgumentNullException>(
			async () => await BlobStoreInterface.ExistsAsync(nullKey!));

		await Assert.ThrowsAsync<ArgumentNullException>(
			async () => await BlobStoreInterface.ReadAsync(nullKey!));

		await Assert.ThrowsAsync<ArgumentNullException>(
			async () => await BlobStoreInterface.TryReadAsync(nullKey!));

		await Assert.ThrowsAsync<ArgumentNullException>(
			async () => await BlobStoreInterface.CreateAsync(nullKey!, WriteContentAsync));

		await Assert.ThrowsAsync<ArgumentNullException>(
			async () => await BlobStoreInterface.CreateAsync(nullKey!, validBytes));

		await Assert.ThrowsAsync<ArgumentNullException>(
			async () => await BlobStoreInterface.UpdateAsync(nullKey!, WriteContentAsync));

		await Assert.ThrowsAsync<ArgumentNullException>(
			async () => await BlobStoreInterface.UpdateAsync(nullKey!, validBytes));

		await Assert.ThrowsAsync<ArgumentNullException>(
			async () => await BlobStoreInterface.CreateOrUpdateAsync(nullKey!, WriteContentAsync));

		await Assert.ThrowsAsync<ArgumentNullException>(
			async () => await BlobStoreInterface.CreateOrUpdateAsync(nullKey!, validBytes));

		await Assert.ThrowsAsync<ArgumentNullException>(
			async () => await BlobStoreInterface.DeleteAsync(nullKey!));
	}
	[Test]
	public async Task Methods_ThrowArgumentNullException_WhenWriteHandlerIsNull()
	{
		// Arrange
		Func<Stream, ValueTask>? nullHandler = null;

		// Act & Assert
		await Assert.ThrowsAsync<ArgumentNullException>(
			async () => await BlobStoreInterface.CreateAsync("key", nullHandler!));

		await Assert.ThrowsAsync<ArgumentNullException>(
			async () => await BlobStoreInterface.UpdateAsync("key", nullHandler!));

		await Assert.ThrowsAsync<ArgumentNullException>(
			async () => await BlobStoreInterface.CreateOrUpdateAsync("key", nullHandler!));
	}

	#endregion

	#region Cancellation Tests
	[Test]
	public async Task Methods_ThrowOperationCanceledException_WhenCancellationRequested()
	{
		// Arrange
		using var cts = new CancellationTokenSource();
		await cts.CancelAsync(); // Use async version instead of synchronously blocking
		byte[] validBytes = Encoding.UTF8.GetBytes(StandardContent);

		// Act & Assert
		await Assert.ThrowsAsync<OperationCanceledException>(
			async () => await BlobStoreInterface.ExistsAsync("key", cts.Token));

		await Assert.ThrowsAsync<OperationCanceledException>(
			async () => await BlobStoreInterface.ReadAsync("key", cts.Token));

		await Assert.ThrowsAsync<OperationCanceledException>(
			async () => await BlobStoreInterface.TryReadAsync("key", cts.Token));

		await Assert.ThrowsAsync<OperationCanceledException>(
			async () => await BlobStoreInterface.CreateAsync("key", cts.Token, WriteContentAsyncWithCancellation));

		await Assert.ThrowsAsync<OperationCanceledException>(
			async () => await BlobStoreInterface.CreateAsync("key", validBytes, cts.Token));

		await Assert.ThrowsAsync<OperationCanceledException>(
			async () => await BlobStoreInterface.UpdateAsync("key", cts.Token, WriteContentAsyncWithCancellation));

		await Assert.ThrowsAsync<OperationCanceledException>(
			async () => await BlobStoreInterface.UpdateAsync("key", validBytes, cts.Token));

		await Assert.ThrowsAsync<OperationCanceledException>(
			async () => await BlobStoreInterface.CreateOrUpdateAsync("key", cts.Token, WriteContentAsyncWithCancellation));

		await Assert.ThrowsAsync<OperationCanceledException>(
			async () => await BlobStoreInterface.CreateOrUpdateAsync("key", validBytes, cts.Token));

		await Assert.ThrowsAsync<OperationCanceledException>(
			async () => await BlobStoreInterface.DeleteAsync("key", cts.Token));
	}

	#endregion

	#region Helper Methods

	/// <summary>
	/// Creates a blob with the specified content.
	/// </summary>
	protected async Task<bool> CreateBlobWithContentAsync(string key, string content)
	{
		return await BlobStoreInterface.CreateAsync(key,
			async stream => await BlobStoreTestsBase<TBlobStore>.WriteContentToStreamAsync(stream, content));
	}

	/// <summary>
	/// Converts a string to byte array using UTF-8 encoding.
	/// </summary>
	protected static byte[] GetBytesFromString(string content)
	{
		return Encoding.UTF8.GetBytes(content);
	}

	/// <summary>
	/// Updates a blob with the specified content.
	/// </summary>
	protected async Task<bool> UpdateBlobWithContentAsync(string key, string content)
	{
		return await BlobStoreInterface.UpdateAsync(key,
			async stream => await BlobStoreTestsBase<TBlobStore>.WriteContentToStreamAsync(stream, content));
	}

	/// <summary>
	/// Creates or updates a blob with the specified content.
	/// </summary>
	protected async Task<bool> CreateOrUpdateBlobWithContentAsync(string key, string content)
	{
		return await BlobStoreInterface.CreateOrUpdateAsync(key,
			async stream => await BlobStoreTestsBase<TBlobStore>.WriteContentToStreamAsync(stream, content));
	}

	/// <summary>
	/// Writes the specified content to a stream.
	/// </summary>
	protected static async Task WriteContentToStreamAsync(Stream stream, string content, CancellationToken cancellationToken = default)
	{
		using var writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true);
		await writer.WriteAsync(content.AsMemory(), cancellationToken);
		await writer.FlushAsync(cancellationToken);
	}

	/// <summary>
	/// Reads the content of a stream as a string.
	/// </summary>
	protected static async Task<string> ReadStreamContentAsync(Stream stream)
	{
		stream.Position = 0;
		using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
		return await reader.ReadToEndAsync();
	}

	/// <summary>
	/// A reusable write handler for testing.
	/// </summary>
	protected async ValueTask WriteContentAsyncWithCancellation(Stream stream, CancellationToken cancellationToken)
	{
		await BlobStoreTestsBase<TBlobStore>.WriteContentToStreamAsync(stream, StandardContent, cancellationToken);
	}

	/// <summary>
	/// A reusable write handler for testing.
	/// </summary>
	protected ValueTask WriteContentAsync(Stream stream)
		=> WriteContentAsyncWithCancellation(stream, default);

	#endregion
}
