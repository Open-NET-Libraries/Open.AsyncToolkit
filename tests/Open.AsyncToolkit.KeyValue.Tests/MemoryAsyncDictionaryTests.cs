namespace Open.AsyncToolkit.KeyValue.Tests;

/// <summary>
/// Tests for the <see cref="MemoryAsyncDictionary{TKey, TValue}"/> class.
/// </summary>
internal sealed class MemoryAsyncDictionaryTests
{
	private MemoryAsyncDictionary<string, string> _dictionary = null!;
	private IAsyncDictionary<string, string> _asyncDictionary = null!;

	[Before(Test)]
	public void Setup()
	{
		_dictionary = new MemoryAsyncDictionary<string, string>();
		_asyncDictionary = _dictionary;
	}

	#region Exists Tests

	[Test]
	public async Task ExistsAsync_ReturnsFalse_WhenKeyDoesNotExist()
	{
		// Act
		bool exists = await _asyncDictionary.ExistsAsync("non-existent-key");

		// Assert
		await Assert.That(exists).IsFalse();
	}

	[Test]
	public async Task ExistsAsync_ReturnsTrue_WhenKeyExists()
	{
		// Arrange
		string key = "test-key";
		string value = "test-value";
		_dictionary[key] = value;

		// Act
		bool exists = await _asyncDictionary.ExistsAsync(key);

		// Assert
		await Assert.That(exists).IsTrue();
	}

	#endregion

	#region Read Tests

	[Test]
	public async Task TryReadAsync_ReturnsFailedResult_WhenKeyDoesNotExist()
	{
		// Act  
		TryReadResult<string> result = await _asyncDictionary.TryReadAsync("non-existent-key");

		// Assert  
		await Assert.That(result.Success).IsFalse();

		Assert.Throws<InvalidOperationException>(() => _ = result.Value);
	}

	[Test]
	public async Task TryReadAsync_ReturnsSuccessResult_WhenKeyExists()
	{
		// Arrange
		string key = "test-key";
		string value = "test-value";
		_dictionary[key] = value;

		// Act
		TryReadResult<string> result = await _asyncDictionary.TryReadAsync(key);

		// Assert
		await Assert.That(result.Success).IsTrue();
		await Assert.That(result.Value).IsEqualTo(value);
	}

	[Test]
	public async Task AsyncDictionaryEntry_ReadsCorrectValue()
	{
		// Arrange
		string key = "entry-key";
		string value = "entry-value";
		_dictionary[key] = value;

		// Act
		IAsyncDictionaryEntry<string, string> entry = _asyncDictionary[key];
		string? entryValue = await entry.Read();

		// Assert
		await Assert.That(entryValue).IsEqualTo(value);
	}

	#endregion

	#region Create Tests

	[Test]
	public async Task CreateAsync_ReturnsTrue_WhenKeyDoesNotExist()
	{
		// Arrange
		string key = "new-key";
		string value = "new-value";

		// Act
		bool created = await _asyncDictionary.CreateAsync(key, value);

		// Assert
		await Assert.That(created).IsTrue();
		await Assert.That(_dictionary[key]).IsEqualTo(value);
	}

	[Test]
	public async Task CreateAsync_ReturnsFalse_WhenKeyAlreadyExists()
	{
		// Arrange
		string key = "existing-key";
		string value = "existing-value";
		_dictionary[key] = value;

		// Act
		bool created = await _asyncDictionary.CreateAsync(key, "new-value");

		// Assert
		await Assert.That(created).IsFalse();
		await Assert.That(_dictionary[key]).IsEqualTo(value); // Value should not change
	}

	#endregion

	#region Update Tests

	[Test]
	public async Task CreateOrUpdateAsync_CreatesNew_WhenKeyDoesNotExist()
	{
		// Arrange
		string key = "new-key";
		string value = "new-value";

		// Act
		bool success = await _asyncDictionary.CreateOrUpdateAsync(key, value);

		// Assert
		await Assert.That(success).IsTrue();
		await Assert.That(_dictionary[key]).IsEqualTo(value);
	}

	[Test]
	public async Task CreateOrUpdateAsync_UpdatesExisting_WhenKeyExists()
	{
		// Arrange
		string key = "existing-key";
		string originalValue = "original-value";
		string updatedValue = "updated-value";
		_dictionary[key] = originalValue;

		// Act
		bool success = await _asyncDictionary.CreateOrUpdateAsync(key, updatedValue);

		// Assert
		await Assert.That(success).IsTrue();
		await Assert.That(_dictionary[key]).IsEqualTo(updatedValue);
	}

	[Test]
	public async Task AsyncDictionaryEntry_UpdatesValue()
	{
		// Arrange
		string key = "entry-key";
		string originalValue = "original-value";
		string updatedValue = "updated-value";
		_dictionary[key] = originalValue;

		// Act
		IAsyncDictionaryEntry<string, string> entry = _asyncDictionary[key];
		bool updated = await entry.CreateOrUpdate(updatedValue);

		// Assert
		await Assert.That(updated).IsTrue();
		await Assert.That(_dictionary[key]).IsEqualTo(updatedValue);
	}

	#endregion

	#region Delete Tests

	[Test]
	public async Task DeleteAsync_ReturnsFalse_WhenKeyDoesNotExist()
	{
		// Act
		bool deleted = await _asyncDictionary.DeleteAsync("non-existent-key");

		// Assert
		await Assert.That(deleted).IsFalse();
	}

	[Test]
	public async Task DeleteAsync_ReturnsTrueAndRemovesKey_WhenKeyExists()
	{
		// Arrange
		string key = "existing-key";
		string value = "value";
		_dictionary[key] = value;

		// Act
		bool deleted = await _asyncDictionary.DeleteAsync(key);

		// Assert
		await Assert.That(deleted).IsTrue();
		await Assert.That(_dictionary.ContainsKey(key)).IsFalse();
	}

	[Test]
	public async Task AsyncDictionaryEntry_DeletesEntry()
	{
		// Arrange
		string key = "entry-key";
		string value = "value";
		_dictionary[key] = value;

		// Act
		IAsyncDictionaryEntry<string, string> entry = _asyncDictionary[key];
		bool deleted = await entry.Delete();

		// Assert
		await Assert.That(deleted).IsTrue();
		await Assert.That(_dictionary.ContainsKey(key)).IsFalse();
	}

	#endregion

	#region Cancellation Tests

	[Test]
	public async Task Operations_RespectCancellationToken()
	{
		// Arrange
		using var cts = new CancellationTokenSource();
		await cts.CancelAsync(); // Cancel before operations

		// Act & Assert
		// Replace the usage of ThrowsAsync with Assert.ThrowsAsync for proper exception testing.
		await Assert.ThrowsAsync<OperationCanceledException>(
			async () => await _asyncDictionary.ExistsAsync("key", cts.Token));

		await Assert.ThrowsAsync<OperationCanceledException>(
			async () => await _asyncDictionary.TryReadAsync("key", cts.Token));

		await Assert.ThrowsAsync<OperationCanceledException>(
			async () => await _asyncDictionary.CreateAsync("key", "value", cts.Token));

		await Assert.ThrowsAsync<OperationCanceledException>(
			async () => await _asyncDictionary.CreateOrUpdateAsync("key", "value", cts.Token));

		await Assert.ThrowsAsync<OperationCanceledException>(
			async () => await _asyncDictionary.DeleteAsync("key", cts.Token));
	}

	#endregion
}