namespace Open.AsyncToolkit.KeyValue.Tests;

/// <summary>
/// Basic tests for the <see cref="SynchronizedAsyncDictionary{TKey, TValue}"/> class
/// covering core functionality and simple operations.
/// </summary>
public partial class SynchronizedAsyncDictionaryTests
{
	#region Read Tests

	[Test]
	public async Task ExistsAsync_ShouldDelegateToUnderlyingDictionary()
	{
		// Act - Check non-existent key
		bool existsBeforeAdd = await _asyncDictionary.ExistsAsync(TestKey, CancellationToken.None);

		// Add to underlying dictionary
		_memoryDict[TestKey] = TestValue;

		// Act - Check after adding
		bool existsAfterAdd = await _asyncDictionary.ExistsAsync(TestKey, CancellationToken.None);

		// Assert
		await Assert.That(existsBeforeAdd).IsFalse();
		await Assert.That(existsAfterAdd).IsTrue();
	}

	[Test]
	public async Task TryReadAsync_ShouldDelegateToUnderlyingDictionary()
	{
		// Act - Try to read non-existent key
		TryReadResult<string> resultBeforeAdd = await _asyncDictionary.TryReadAsync(TestKey, CancellationToken.None);

		// Add to underlying dictionary
		_memoryDict[TestKey] = TestValue;

		// Act - Read after adding
		TryReadResult<string> resultAfterAdd = await _asyncDictionary.TryReadAsync(TestKey, CancellationToken.None);

		// Assert
		await Assert.That(resultBeforeAdd.Success).IsFalse();
		await Assert.That(resultAfterAdd.Success).IsTrue();
		await Assert.That(resultAfterAdd.Value).IsEqualTo(TestValue);
	}

	#endregion

	#region Basic Lease Tests
	[Test]
	public async Task LeaseAsync_ShouldAllowOperationsOnDictionaryEntry()
	{
		// Arrange
		const string initialValue = "initial-value";
		const string updatedValue = "updated-value";

		// Add value to inner dictionary
		_memoryDict[TestKey] = initialValue;

		// Act - Read value using lease
		string readValue = await ExecuteSimpleLeaseOperation(_asyncDictionary,
			static async (entry, ct) =>
			{
				TryReadResult<string> result = await entry.TryRead(ct);
				return result.Value;
			});

		// Act - Update value using lease
		await ExecuteSimpleLeaseOperation(_asyncDictionary,
			static async (entry, ct) =>
			{
				await entry.CreateOrUpdate(updatedValue, ct);
				return true;
			});

		// Act - Read updated value
		TryReadResult<string> updatedResult = await _asyncDictionary.TryReadAsync(TestKey, CancellationToken.None);

		// Assert
		await Assert.That(readValue).IsEqualTo(initialValue);
		await Assert.That(updatedResult.Success).IsTrue();
		await Assert.That(updatedResult.Value).IsEqualTo(updatedValue);
	}

	[Test]
	public async Task LeaseAsync_WithCancelledToken_ShouldThrow()
	{
		// Arrange
		const string key = "test-key";
		// Create a cancelled token
		using var cts = new CancellationTokenSource();
		await cts.CancelAsync();

		// Act & Assert
		try
		{
			await _asyncDictionary.LeaseAsync<bool>(
				key, cts.Token,
				static async (entry, ct) =>
				{
					await Task.Delay(1, ct);
					return true;
				});

			// If we get here, the test failed
			Assert.Fail("Expected OperationCanceledException was not thrown");
		}
		catch (OperationCanceledException)
		{
			// Expected exception, test passes
		}
	}

	[Test]
	public async Task LeaseAsync_WhenKeyDoesNotExist_ShouldStillAcquireLease()
	{
		// Arrange
		const string key = "non-existent-key";
		const string value = "new-value";

		// Act - Create a new entry using the lease
		bool created = await _asyncDictionary.LeaseAsync<bool>(
			key, CancellationToken.None,
			static async (entry, ct) =>
			{
				// Check if it exists first
				bool exists = await entry.Exists(ct);
				if (!exists)
				{
					// Create the entry
					return await entry.Create(value, ct);
				}

				return false;
			});

		// Get the value to verify it was created
		TryReadResult<string> result = await _asyncDictionary.TryReadAsync(key, CancellationToken.None);

		// Assert
		await Assert.That(created).IsTrue();
		await Assert.That(result.Success).IsTrue();
		await Assert.That(result.Value).IsEqualTo(value);
	}

	#endregion

	#region Construction and Disposal Tests

	[Test]
	public Task Constructor_WithNullDictionary_ShouldThrowArgumentNullException()
	{
		// Act & Assert
		Assert.Throws<ArgumentNullException>(static () =>
			_ = new SynchronizedAsyncDictionary<string, string>(null!));

		return Task.CompletedTask;
	}

	[Test]
	public Task Dispose_ShouldReleaseResources()
	{
		// Arrange - Create a separate instance
		var localMemoryDict = new MemoryAsyncDictionary<string, string>();
		var localSut = new SynchronizedAsyncDictionary<string, string>(localMemoryDict);

		// Act
		localSut.Dispose();

		// Assert - Trying to use after disposal should throw
		return Assert.ThrowsAsync<ObjectDisposedException>(async () =>
		{
			_ = await localSut.LeaseAsync(
				"any-key", CancellationToken.None,
				async (entry, ct) =>
				{
					await Task.Delay(1, ct);
					return true;
				});
		});
	}

	[Test]
	public async Task DisposedDictionary_ThrowsOnSimpleMethods()
	{
		// Arrange - Create a separate instance with test data
		SynchronizedAsyncDictionary<string, string> localDict = CreateTestDictionary(TestValue);

		// Act
		localDict.Dispose();

		// Assert - Trying to use after disposal should throw
		await Assert.ThrowsAsync<ObjectDisposedException>(
			async () => _ = await localDict.ExistsAsync(TestKey, CancellationToken.None));

		await Assert.ThrowsAsync<ObjectDisposedException>(
			async () => _ = await localDict.TryReadAsync(TestKey, CancellationToken.None));
	}

	#endregion

	#region Extension Method Tests

	[Test]
	public async Task Extension_Synchronized_CreatesProperWrapper()
	{
		// Arrange
		var memoryDict = new MemoryAsyncDictionary<string, string> { [TestKey] = TestValue };

		// Act - Use the extension method
		SynchronizedAsyncDictionary<string, string>? synchronizedDict = memoryDict.Synchronized();

		try
		{
			// Use the synchronized dictionary
			string readValue = await synchronizedDict.LeaseAsync<string>(
				TestKey, CancellationToken.None,
				static async (entry, ct) =>
				{
					TryReadResult<string> result = await entry.TryRead(ct);
					return result.Value;
				});

			// Assert
			await Assert.That(readValue).IsEqualTo(TestValue);

			// Check that synchronizedDict is not null and implements the right interface
			await Assert.That(synchronizedDict != null).IsTrue();

			// Type check that it's the correct implementation
			if (synchronizedDict != null)
			{
				Type type = synchronizedDict.GetType();
				await Assert.That(typeof(ISynchronizedAsyncDictionary<string, string>).IsAssignableFrom(type)).IsTrue();
				await Assert.That(typeof(SynchronizedAsyncDictionary<string, string>).IsAssignableFrom(type)).IsTrue();
			}
		}
		finally
		{
			// Clean up - cast to the concrete type which implements IDisposable
			if (synchronizedDict is SynchronizedAsyncDictionary<string, string> disposable)
			{
				disposable.Dispose();
			}
		}
	}

	#endregion
}
