using System.Collections.Concurrent;

namespace Open.BlobStorageAdapter.Tests;

/// <summary>
/// Tests for the <see cref="SynchronizedAsyncDictionary{TKey, TValue}"/> class.
/// </summary>
public class SynchronizedAsyncDictionaryTests
{
	// Constants used throughout tests
	private const string TestKey = "test-key";
	private const string TestValue = "test-value";

	private MemoryAsyncDictionary<string, string> _memoryDict = null!;
	private SynchronizedAsyncDictionary<string, string> _sut = null!;
	private ISynchronizedAsyncDictionary<string, string> _asyncDictionary = null!;

	[Before(Test)]
	public void Setup()
	{
		_memoryDict = new MemoryAsyncDictionary<string, string>();
		_sut = new SynchronizedAsyncDictionary<string, string>(_memoryDict);
		_asyncDictionary = _sut;
	}

	[After(Test)]
	public void Cleanup() => _sut.Dispose();

	// Helper method to create a disposable dictionary instance
	private static SynchronizedAsyncDictionary<string, TValue> CreateTestDictionary<TValue>(TValue initialValue = default!)
	{
		var memoryDict = new MemoryAsyncDictionary<string, TValue>();
		if (initialValue != null)
		{
			memoryDict[TestKey] = initialValue;
		}

		return new SynchronizedAsyncDictionary<string, TValue>(memoryDict);
	}

	// Helper method to execute a simple lease operation
	private static ValueTask<TResult> ExecuteSimpleLeaseOperation<TResult>(
		ISynchronizedAsyncDictionary<string, string> dictionary,
		Func<IAsyncDictionaryEntry<string, string>, CancellationToken, ValueTask<TResult>> operation,
		CancellationToken cancellationToken = default)
		=> dictionary.LeaseAsync(TestKey, cancellationToken, operation);

	#region Read Tests

	[Test]
	public async Task ExistsAsync_ShouldDelegateToUnderlyingDictionary()
	{
		// Act - Check non-existent key
		var existsBeforeAdd = await _asyncDictionary.ExistsAsync(TestKey, CancellationToken.None);

		// Add to underlying dictionary
		_memoryDict[TestKey] = TestValue;

		// Act - Check after adding
		var existsAfterAdd = await _asyncDictionary.ExistsAsync(TestKey, CancellationToken.None);

		// Assert
		await Assert.That(existsBeforeAdd).IsFalse();
		await Assert.That(existsAfterAdd).IsTrue();
	}

	[Test]
	public async Task TryReadAsync_ShouldDelegateToUnderlyingDictionary()
	{
		// Act - Try to read non-existent key
		var resultBeforeAdd = await _asyncDictionary.TryReadAsync(TestKey, CancellationToken.None);

		// Add to underlying dictionary
		_memoryDict[TestKey] = TestValue;

		// Act - Read after adding
		var resultAfterAdd = await _asyncDictionary.TryReadAsync(TestKey, CancellationToken.None);

		// Assert
		await Assert.That(resultBeforeAdd.Success).IsFalse();
		await Assert.That(resultAfterAdd.Success).IsTrue();
		await Assert.That(resultAfterAdd.Value).IsEqualTo(TestValue);
	}

	#endregion

	#region Lease Tests
	[Test]
	public async Task LeaseAsync_ShouldAllowOperationsOnDictionaryEntry()
	{
		// Arrange
		const string initialValue = "initial-value";
		const string updatedValue = "updated-value";

		// Add value to inner dictionary
		_memoryDict[TestKey] = initialValue;

		// Act - Read value using lease
		var readValue = await ExecuteSimpleLeaseOperation(_asyncDictionary,
			async (entry, ct) =>
			{
				var result = await entry.TryRead(ct);
				return result.Value;
			});

		// Act - Update value using lease
		await ExecuteSimpleLeaseOperation(_asyncDictionary,
			async (entry, ct) =>
			{
				await entry.CreateOrUpdate(updatedValue, ct);
				return true;
			});

		// Act - Read updated value
		var updatedResult = await _asyncDictionary.TryReadAsync(TestKey, CancellationToken.None);

		// Assert
		await Assert.That(readValue).IsEqualTo(initialValue);
		await Assert.That(updatedResult.Success).IsTrue();
		await Assert.That(updatedResult.Value).IsEqualTo(updatedValue);
	}

	[Test]
	public async Task LeaseAsync_ShouldPreventConcurrentAccess()
	{
		// Arrange
		const string key = "counter";
		const int initialValue = 0;
		const int iterations = 100;

		var memoryDictInt = new MemoryAsyncDictionary<string, int>();
		var sutInt = new SynchronizedAsyncDictionary<string, int>(memoryDictInt);

		try
		{
			// Add initial value to inner dictionary
			memoryDictInt[key] = initialValue;

			// We'll use a thread-safe collection to store any exceptions
			var exceptions = new ConcurrentBag<Exception>();
			var tasks = new List<Task>();
			using var resetEvent = new ManualResetEventSlim(false);

			// Create multiple tasks that will increment the counter
			for (int i = 0; i < iterations; i++)
			{
				tasks.Add(Task.Run(async () =>
				{
					try
					{
						// Wait for all tasks to be ready before starting
						resetEvent.Wait();

						await sutInt.LeaseAsync<bool>(
							key, CancellationToken.None,
							async (entry, ct) =>
							{
								var result = await entry.TryRead(ct);
								var currentValue = result.Value;

								// Simulate some work
								await Task.Delay(1);

								// Increment and update
								await entry.CreateOrUpdate(currentValue + 1, ct);
								return true;
							});
					}
					catch (Exception ex)
					{
						exceptions.Add(ex);
					}
				}));
			}

			// Start all tasks at once
			resetEvent.Set();

			// Wait for all tasks to complete
			await Task.WhenAll(tasks);

			// Act - Get the final value
			var finalResult = await sutInt.TryReadAsync(key, CancellationToken.None);

			// Assert
			await Assert.That(exceptions.Count).IsEqualTo(0); // No exceptions should have occurred
			await Assert.That(finalResult.Value).IsEqualTo(iterations); // Counter should equal iterations
		}
		finally
		{
			// Ensure proper cleanup
			sutInt.Dispose();
		}
	}

	[Test]
	public async Task LeaseAsync_WithCancelledToken_ShouldThrow()
	{
		// Arrange
		const string key = "test-key";

		// Create a cancelled token
		var cts = new CancellationTokenSource();
		cts.Cancel();
		// Act & Assert
		try
		{
			await _asyncDictionary.LeaseAsync<bool>(
				key, cts.Token,
				async (entry, ct) =>
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
	public async Task LeaseAsync_WhenOperationCancelled_ShouldReleaseLock()
	{
		// Arrange
		_memoryDict[TestKey] = TestValue;

		// Create a cancellation token source
		using var cts = new CancellationTokenSource();

		// Act - First attempt will be cancelled during execution
		var firstTask = Task.Run(async () =>
		{
			try
			{
				await _asyncDictionary.LeaseAsync<bool>(
					TestKey, cts.Token,
					async (entry, ct) =>
					{
						// Delay to simulate work
						await Task.Delay(500, ct);
						return true;
					});
			}
			catch (OperationCanceledException)
			{
				// Expected exception
				return;
			}
		});

		// Cancel the operation after a short delay
		await Task.Delay(100);
		cts.Cancel();

		// Wait for the first task to complete (it should throw)
		await firstTask;

		// Act - Second attempt should succeed because the lock was released
		var result = await ExecuteSimpleLeaseOperation(_asyncDictionary,
			async (entry, ct) =>
			{
				var readResult = await entry.TryRead(ct);
				return readResult.Value;
			});

		// Assert
		await Assert.That(result).IsEqualTo(TestValue);
	}

	[Test]
	public async Task LeaseAsync_WhenKeyDoesNotExist_ShouldStillAcquireLease()
	{
		// Arrange
		const string key = "non-existent-key";
		const string value = "new-value";

		// Act - Create a new entry using the lease
		var created = await _asyncDictionary.LeaseAsync<bool>(
			key, CancellationToken.None,
			async (entry, ct) =>
			{
				// Check if it exists first
				var exists = await entry.Exists(ct);
				if (!exists)
				{
					// Create the entry
					return await entry.Create(value, ct);
				}
				return false;
			});

		// Get the value to verify it was created
		var result = await _asyncDictionary.TryReadAsync(key, CancellationToken.None);

		// Assert
		await Assert.That(created).IsTrue();
		await Assert.That(result.Success).IsTrue();
		await Assert.That(result.Value).IsEqualTo(value);
	}

	#endregion

	#region Construction and Disposal Tests

	[Test]
	public void Constructor_WithNullDictionary_ShouldThrowArgumentNullException() =>
		// Act & Assert
		Assert.Throws<ArgumentNullException>(() =>
			new SynchronizedAsyncDictionary<string, string>(null!));
	[Test]
	public void Dispose_ShouldReleaseResources()
	{
		// Arrange - Create a separate instance
		var localMemoryDict = new MemoryAsyncDictionary<string, string>();
		var localSut = new SynchronizedAsyncDictionary<string, string>(localMemoryDict);

		// Act
		localSut.Dispose();

		// Assert - Trying to use after disposal should throw
		Assert.Throws<ObjectDisposedException>(() =>
		{
			var task = localSut.LeaseAsync<bool>(
				"any-key", CancellationToken.None,
				async (entry, ct) =>
				{
					await Task.Delay(1, ct);
					return true;
				});
			task.GetAwaiter().GetResult();
		});
	}

	[Test]
	public async Task DisposedDictionary_CanStillCallSimpleMethods()
	{
		// Arrange - Create a separate instance with test data
		var localDict = CreateTestDictionary(TestValue);

		// Act
		localDict.Dispose();

		// These methods should still work since they just delegate to the inner dictionary
		var exists = await localDict.ExistsAsync(TestKey, CancellationToken.None);
		var readResult = await localDict.TryReadAsync(TestKey, CancellationToken.None);

		// Assert
		await Assert.That(exists).IsTrue();
		await Assert.That(readResult.Success).IsTrue();
		await Assert.That(readResult.Value).IsEqualTo(TestValue);
	}

	#endregion

	#region Resource Management Tests

	[Test]
	public async Task LeaseAsync_ShouldCleanupUnusedLocks()
	{
		// Arrange
		const string temporaryKey = "temporary-key";
		const string value = "test-value";

		// Create a temporary entry
		_memoryDict[temporaryKey] = value;

		// Act - Use the entry through a lease
		await _asyncDictionary.LeaseAsync<bool>(
			temporaryKey, CancellationToken.None,
			async (entry, ct) =>
			{
				// Read the entry
				var result = await entry.TryRead(ct);

				// Delete the entry
				await entry.Delete(ct);
				return true;
			});

		// Try to lease it again to ensure the lock was cleaned up
		var lockCleanedUp = await _asyncDictionary.LeaseAsync<bool>(
			temporaryKey, CancellationToken.None,
			async (entry, ct) =>
			{
				// This should work even though the key doesn't exist anymore
				var exists = await entry.Exists(ct);
				return !exists; // Should return true (key doesn't exist)
			});

		// Assert
		await Assert.That(lockCleanedUp).IsTrue();
		await Assert.That(await _asyncDictionary.ExistsAsync(temporaryKey, CancellationToken.None)).IsFalse();
	}

	#endregion

	#region Extension Method Tests

	[Test]
	public async Task Extension_Synchronized_CreatesProperWrapper()
	{
		// Arrange
		var memoryDict = new MemoryAsyncDictionary<string, string> { [TestKey] = TestValue };

		// Act - Use the extension method
		var synchronizedDict = memoryDict.Synchronized();

		try
		{
			// Use the synchronized dictionary
			var readValue = await synchronizedDict.LeaseAsync<string>(
				TestKey, CancellationToken.None,
				async (entry, ct) =>
				{
					var result = await entry.TryRead(ct);
					return result.Value;
				});

			// Assert
			await Assert.That(readValue).IsEqualTo(TestValue);

			// Check that synchronizedDict is not null and implements the right interface
			await Assert.That(synchronizedDict != null).IsTrue();
			// Type check that it's the correct implementation
			if (synchronizedDict != null)
			{
				var type = synchronizedDict.GetType();
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