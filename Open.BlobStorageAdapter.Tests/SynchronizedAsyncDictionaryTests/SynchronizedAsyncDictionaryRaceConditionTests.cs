using System.Collections.Concurrent;
using System.Diagnostics;

namespace Open.BlobStorageAdapter.Tests;

/// <summary>
/// Advanced tests specifically targeting race conditions and deadlock detection
/// in the <see cref="SynchronizedAsyncDictionary{TKey, TValue}"/> class.
/// </summary>
public partial class SynchronizedAsyncDictionaryTests
{
	#region Race Condition Tests

	private const string RaceTestPrefix = "race-test-";

	/// <summary>
	/// This test repeatedly performs operations that are prone to race conditions
	/// if synchronization is not implemented correctly, and checks that the final
	/// state is consistent.
	/// </summary>
	[Test]
	public async Task AtomicIncrementDecrement_WithHighConcurrency_ShouldBeRaceConditionFree()
	{
		// Arrange
		const string key = $"{RaceTestPrefix}counter";
		const int initialValue = 1000;
		const int threadCount = 20;
		const int incrementsPerThread = 500;
		const int decrementsPerThread = 500;

		var memoryDict = new MemoryAsyncDictionary<string, int>();
		var sut = new SynchronizedAsyncDictionary<string, int>(memoryDict);

		// Expected final value: should equal initial if increments and decrements match
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
					await sut.LeaseAsync(
						key, CancellationToken.None,
						async (entry, ct) =>
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
					await sut.LeaseAsync(
						key, CancellationToken.None,
						async (entry, ct) =>
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
	/// Tests for race conditions in dictionary entry deletion and recreation.
	/// This is a particularly challenging scenario for dictionaries with synchronization.
	/// </summary>
	[Test]
	public async Task DeleteAndRecreate_WithConcurrentReads_ShouldNotDeadlock()
	{
		// Arrange
		const string key = $"{RaceTestPrefix}volatile-key";
		const int initialValue = 0;
		const int cycles = 100; const int readThreads = 15;

		var memoryDict = new MemoryAsyncDictionary<string, int>();
		var sut = new SynchronizedAsyncDictionary<string, int>(memoryDict);

		// Set initial value
		memoryDict[key] = initialValue;

		// Track any exceptions that occur
		var exceptions = new ConcurrentBag<Exception>();

		// Flag to signal readers to stop
		using var cancellationSource = new CancellationTokenSource();

		// Task to constantly delete and recreate the entry
		var volatileTask = Task.Run(async () =>
		{
			try
			{
				for (int i = 0; i < cycles && !cancellationSource.Token.IsCancellationRequested; i++)
				{
					// Delete the entry
					await sut.LeaseAsync(
						key, cancellationSource.Token,
						async (entry, ct) =>
						{
							bool exists = await entry.Exists(ct);
							if (exists)
							{
								await entry.Delete(ct);
							}

							return true;
						});

					// Small delay to increase chance of race conditions
					await Task.Delay(1);

					// Recreate the entry with a new value
					await sut.LeaseAsync(
						key, cancellationSource.Token,
						async (entry, ct) =>
						{
							await entry.Create(i, ct);
							return true;
						});
				}
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch (Exception ex)
			{
				exceptions.Add(ex);
			}
		});

		// Create reader tasks that constantly try to read the entry
		Task[] readerTasks = Enumerable.Range(0, readThreads)
			.Select(_ => Task.Run(async () =>
			{
				try
				{
					while (!cancellationSource.Token.IsCancellationRequested)
					{
						// Try to read
						try
						{
							await sut.TryReadAsync(key, cancellationSource.Token);
						}
						catch (OperationCanceledException)
						{
							// Expected when cancellation is requested
							break;
						}

						// Occasionally try a lease operation too
						if (Random.Shared.Next(5) == 0)
						{
							try
							{
								await sut.LeaseAsync(
									key, cancellationSource.Token,
									async (entry, ct) =>
									{
										TryReadResult<int> result = await entry.TryRead(ct);
										return result.Success;
									});
							}
							catch (OperationCanceledException)
							{
								// Expected when cancellation is requested
								break;
							}
						}
					}
				}
				catch (OperationCanceledException)
				{
					throw;
				}
				catch (Exception ex)
				{
					exceptions.Add(ex);
				}
			}))
			.ToArray();

		// Set a timeout - we should complete within reasonable time
		var timeoutTask = Task.Delay(TimeSpan.FromSeconds(10));

		// Wait for the volatile task to complete or timeout
		Task completedTask = await Task.WhenAny(volatileTask, timeoutTask);

		// Stop the readers
		cancellationSource.Cancel();

		// Wait for all readers to finish
		await Task.WhenAll(readerTasks);

		// Assert that there were no exceptions and we didn't time out
		bool noExceptions = exceptions.IsEmpty;
		bool correctTaskCompleted = completedTask == volatileTask;

		await Assert.That(noExceptions).IsTrue();
		await Assert.That(correctTaskCompleted).IsTrue();
	}

	#endregion

	#region Semaphore and Resource Tests

	/// <summary>
	/// Tests for race conditions in the semaphore pool recycling.
	/// This creates high turnover of entries to stress test the recycling logic.
	/// </summary>
	[Test]
	public async Task RapidSemaphoreRecycling_ShouldNotCauseRaceConditions()
	{        // Arrange
		const int keyCount = 10;
		const int operationCount = 200;

		var memoryDict = new MemoryAsyncDictionary<string, int>();
		var sut = new SynchronizedAsyncDictionary<string, int>(memoryDict);

		// Generate keys
		string[] keys = Enumerable.Range(0, keyCount)
			.Select(i => $"{RaceTestPrefix}recycle-{i}")
			.ToArray();

		// Track exceptions
		var exceptions = new ConcurrentBag<Exception>();

		// Create a stopwatch for timing
		var stopwatch = Stopwatch.StartNew();

		// Execute multiple operations on each key in parallel
		Task[] tasks = keys.SelectMany(key =>
			Enumerable.Range(0, operationCount).Select(_ =>
				Task.Run(async () =>
				{
					try
					{
						// Create the entry
						await sut.LeaseAsync(
							key, CancellationToken.None,
							async (entry, ct) =>
							{
								await entry.CreateOrUpdate(1, ct);
								return true;
							});

						// Short delay to create overlapping operations
						await Task.Delay(1);

						// Delete the entry
						await sut.LeaseAsync(
							key, CancellationToken.None,
							async (entry, ct) =>
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
				}))
			).ToArray();

		// Wait for all tasks to complete
		await Task.WhenAll(tasks);

		// Stop timing
		stopwatch.Stop();

		// Assert no exceptions occurred
		await Assert.That(exceptions.Count).IsEqualTo(0);

		// Verify we can still use the dictionary after stress test
		string testKey = $"{RaceTestPrefix}post-test";
		await sut.LeaseAsync(
			testKey, CancellationToken.None,
			async (entry, ct) =>
			{
				await entry.CreateOrUpdate(999, ct);
				return true;
			});

		TryReadResult<int> result = await sut.TryReadAsync(testKey, CancellationToken.None);
		await Assert.That(result.Success).IsTrue();
		await Assert.That(result.Value).IsEqualTo(999);

		// Output timing information
		Console.WriteLine($"Completed {keys.Length * operationCount * 2} operations in {stopwatch.ElapsedMilliseconds}ms");
	}

	#endregion

	#region Deadlock Prevention Tests

	/// <summary>
	/// This test creates a pattern of operations that is likely to cause deadlocks
	/// if the locking mechanism isn't properly implemented.
	/// </summary>
	[Test]
	public async Task DeadlockSusceptiblePattern_ShouldComplete()
	{
		// Arrange - create a specific pattern of operations that could cause deadlocks
		const string key1 = $"{RaceTestPrefix}deadlock-a";
		const string key2 = $"{RaceTestPrefix}deadlock-b";

		var memoryDict = new MemoryAsyncDictionary<string, int>();
		var sut = new SynchronizedAsyncDictionary<string, int>(memoryDict);

		// Initialize dictionary
		memoryDict[key1] = 1;
		memoryDict[key2] = 2;

		// Timeout to detect deadlocks
		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

		// Create two tasks with lease operations in opposite order
		var task1 = Task.Run(async () =>
		{
			for (int i = 0; i < 100 && !cts.Token.IsCancellationRequested; i++)
			{
				// First lease key1, then key2
				await sut.LeaseAsync(
					key1, cts.Token,
					async (entry1, ct) =>
					{
						// Small delay to increase chance of conflict
						await Task.Delay(1, ct);

						// Try to lease key2 while holding key1
						return await sut.LeaseAsync(
							key2, ct,
							async (entry2, innerCt) =>
							{
								TryReadResult<int> result1 = await entry1.TryRead(innerCt);
								TryReadResult<int> result2 = await entry2.TryRead(innerCt);

								if (result1.Success && result2.Success)
								{
									// Update both values
									await entry1.CreateOrUpdate(result1.Value + result2.Value, innerCt);
									await entry2.CreateOrUpdate(result2.Value + 1, innerCt);
								}

								return true;
							});
					});
			}
		});

		var task2 = Task.Run(async () =>
		{
			for (int i = 0; i < 100 && !cts.Token.IsCancellationRequested; i++)
			{
				// First lease key2, then key1 (opposite order from task1)
				await sut.LeaseAsync(
					key2, cts.Token,
					async (entry2, ct) =>
					{
						// Small delay to increase chance of conflict
						await Task.Delay(1, ct);

						// Try to lease key1 while holding key2
						return await sut.LeaseAsync(
							key1, ct,
							async (entry1, innerCt) =>
							{
								TryReadResult<int> result1 = await entry1.TryRead(innerCt);
								TryReadResult<int> result2 = await entry2.TryRead(innerCt);

								if (result1.Success && result2.Success)
								{
									// Update both values
									await entry2.CreateOrUpdate(result2.Value + result1.Value, innerCt);
									await entry1.CreateOrUpdate(result1.Value + 1, innerCt);
								}

								return true;
							});
					});
			}
		});

		// Wait for both tasks to complete or timeout
		Task completedTask = await Task.WhenAny(
			Task.WhenAll(task1, task2),
			Task.Delay(TimeSpan.FromSeconds(10))
		);

		// Check if tasks completed
		bool completed = completedTask == Task.WhenAll(task1, task2);

		// Assert tasks completed without deadlock
		await Assert.That(completed).IsTrue();

		// Verify we can still access both keys
		TryReadResult<int> key1Result = await sut.TryReadAsync(key1, CancellationToken.None);
		TryReadResult<int> key2Result = await sut.TryReadAsync(key2, CancellationToken.None);

		await Assert.That(key1Result.Success).IsTrue();
		await Assert.That(key2Result.Success).IsTrue();
	}

	/// <summary>
	/// Tests the dictionary under extreme thread contention.
	/// </summary>
	[Test]
	public async Task ExtremeThreadContention_ShouldNotDeadlock()
	{
		// Arrange
		const string key = $"{RaceTestPrefix}contention"; const int threads = 50; // This is a lot of threads all hitting the same key
		const int operationsPerThread = 20;

		var memoryDict = new MemoryAsyncDictionary<string, int>();
		var sut = new SynchronizedAsyncDictionary<string, int>(memoryDict);

		// Set initial value
		memoryDict[key] = 0;

		// Track errors
		var errors = new ConcurrentBag<Exception>();

		// Create barrier to synchronize thread starts for maximum contention
		using var barrier = new Barrier(threads);

		// Create stopwatch for timing
		var stopwatch = Stopwatch.StartNew();

		// Create and start tasks
		Task[] tasks = Enumerable.Range(0, threads)
			.Select(threadId => Task.Run(async () =>
			{
				try
				{
					// Wait for all threads to reach this point
					barrier.SignalAndWait();

					for (int i = 0; i < operationsPerThread; i++)
					{
						// Perform a lease operation
						await sut.LeaseAsync(
							key, CancellationToken.None,
							async (entry, ct) =>
							{
								// Add a small variation in timing to increase chance of contention patterns
								await Task.Delay(threadId % 3, ct);

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

		// Stop timing
		stopwatch.Stop();

		// Retrieve final value
		TryReadResult<int> finalValue = await sut.TryReadAsync(key, CancellationToken.None);

		// Assert
		await Assert.That(errors.Count).IsEqualTo(0);
		await Assert.That(finalValue.Success).IsTrue();
		await Assert.That(finalValue.Value).IsEqualTo(threads * operationsPerThread);

		// Output timing information
		Console.WriteLine($"Extreme contention test: {threads * operationsPerThread} operations on a single key");
		Console.WriteLine($"Completed in {stopwatch.ElapsedMilliseconds}ms");
		Console.WriteLine($"Operations per second: {threads * operationsPerThread * 1000.0 / stopwatch.ElapsedMilliseconds:N0}");
	}

	#endregion
}
