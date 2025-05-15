using System.Collections.Concurrent;

namespace Open.AsyncToolkit.KeyValue.Tests;

/// <summary>
/// Advanced stress tests for the <see cref="SynchronizedAsyncDictionary{TKey, TValue}"/> class
/// to verify behavior under heavy load and concurrent operations.
/// </summary>
internal partial class SynchronizedAsyncDictionaryTests
{
	#region Advanced Concurrency Tests

	/// <summary>
	/// Tests atomic increment/decrement operations under high concurrency.
	/// </summary>
	[Test]
	public async Task ConcurrentIncrementDecrements_ShouldMaintainConsistency()
	{
		// Arrange
		const string key = "counter";
		const int initialValue = 1000;
		const int threadCount = 20;
		const int incrementsPerThread = 100;
		const int decrementsPerThread = 100;        // Create int dictionary for this test
		var memoryDict = new MemoryAsyncDictionary<string, int>();
		using var sut = new SynchronizedAsyncDictionary<string, int>(memoryDict);

		// Expected final value (if increments and decrements match)
		const int expectedFinalValue = initialValue;

		// Set initial value
		memoryDict[key] = initialValue;

		// Create tasks for parallel execution
		var tasks = new List<Task>();

		// Create increment tasks
		for (int i = 0; i < threadCount; i++)
		{
			tasks.Add(Task.Run(async () =>
			{
				for (int j = 0; j < incrementsPerThread; j++)
				{
					await sut.LeaseAsync(key, CancellationToken.None, async (entry, ct) =>
					{
						TryReadResult<int> result = await entry.TryRead(ct);
						if (result.Success)
						{
							await entry.CreateOrUpdate(result.Value + 1, ct);
						}

						return true;
					});
				}
			}));
		}

		// Create decrement tasks
		for (int i = 0; i < threadCount; i++)
		{
			tasks.Add(Task.Run(async () =>
			{
				for (int j = 0; j < decrementsPerThread; j++)
				{
					await sut.LeaseAsync(key, CancellationToken.None, async (entry, ct) =>
					{
						TryReadResult<int> result = await entry.TryRead(ct);
						if (result.Success)
						{
							await entry.CreateOrUpdate(result.Value - 1, ct);
						}

						return true;
					});
				}
			}));
		}

		// Wait for all tasks to complete
		await Task.WhenAll(tasks);

		// Read final value
		TryReadResult<int> finalResult = await sut.TryReadAsync(key, CancellationToken.None);

		// Assert
		await Assert.That(finalResult.Success).IsTrue();
		await Assert.That(finalResult.Value).IsEqualTo(expectedFinalValue);
	}

	/// <summary>
	/// Tests that the dictionary can handle high contention on a single key.
	/// </summary>
	[Test]
	public async Task HighContention_SingleKey_ShouldNotDeadlock()
	{        // Arrange
		const string key = "contention-key";
		const int threads = 30;
		const int operationsPerThread = 10;

		var memoryDict = new MemoryAsyncDictionary<string, int>();
		using var sut = new SynchronizedAsyncDictionary<string, int>(memoryDict);

		// Set initial value
		memoryDict[key] = 0;

		// Create a barrier to synchronize all threads
		using var barrier = new Barrier(threads);
		var errors = new ConcurrentBag<Exception>();

		// Act - create multiple threads all trying to update the same key
		Task[] tasks = Enumerable.Range(0, threads)
			.Select(_ => Task.Run(async () =>
			{
				try
				{
					// Wait for all threads to be ready
					barrier.SignalAndWait();

					for (int i = 0; i < operationsPerThread; i++)
					{
						await sut.LeaseAsync(key, CancellationToken.None, async (entry, ct) =>
						{
							TryReadResult<int> result = await entry.TryRead(ct);
							if (result.Success)
							{
								await entry.CreateOrUpdate(result.Value + 1, ct);
							}

							return true;
						});
					}
				}
				catch (Exception ex)
				{
					errors.Add(ex);
				}
			}))
			.ToArray();

		// Wait for all tasks to complete
		await Task.WhenAll(tasks);

		// Read final value
		TryReadResult<int> finalResult = await sut.TryReadAsync(key, CancellationToken.None);

		// Assert
		await Assert.That(errors.Count).IsEqualTo(0);
		await Assert.That(finalResult.Success).IsTrue();
		await Assert.That(finalResult.Value).IsEqualTo(threads * operationsPerThread);
	}

	#endregion

	#region Resource Management Tests

	/// <summary>
	/// Tests rapid creation and deletion of entries.
	/// </summary>
	[Test]
	public async Task RapidCreateDelete_ShouldNotLeakResources()
	{        // Arrange
		const int keyCount = 20;
		const int iterationsPerKey = 10;

		var memoryDict = new MemoryAsyncDictionary<string, int>();
		using var sut = new SynchronizedAsyncDictionary<string, int>(memoryDict);

		string[] keys = Enumerable.Range(0, keyCount)
			.Select(i => $"rapid-key-{i}")
			.ToArray();

		var exceptions = new ConcurrentBag<Exception>();

		// Act - rapidly create and delete entries
		Task[] tasks = keys.SelectMany(key =>
			Enumerable.Range(0, iterationsPerKey).Select(_ =>
				Task.Run(async () =>
				{
					try
					{
						// Create the entry
						await sut.LeaseAsync(key, CancellationToken.None, async (entry, ct) =>
						{
							await entry.CreateOrUpdate(1, ct);
							return true;
						});

						// Small delay to increase contention
						await Task.Delay(1);

						// Delete the entry
						await sut.LeaseAsync(key, CancellationToken.None, async (entry, ct) =>
						{
							bool exists = await entry.Exists(ct);
							if (exists)
							{
								await entry.Delete(ct);
							}

							return true;
						});
					}
					catch (Exception ex)
					{
						exceptions.Add(ex);
					}
				})
			)
		).ToArray();

		// Wait for all operations to complete
		await Task.WhenAll(tasks);

		// Verify no keys remain
		bool allKeysDeleted = true;
		foreach (string? key in keys)
		{
			bool exists = await sut.ExistsAsync(key, CancellationToken.None);
			if (exists)
			{
				allKeysDeleted = false;
				break;
			}
		}

		// Assert
		await Assert.That(exceptions.Count).IsEqualTo(0);
		await Assert.That(allKeysDeleted).IsTrue();
	}

	#endregion
}
