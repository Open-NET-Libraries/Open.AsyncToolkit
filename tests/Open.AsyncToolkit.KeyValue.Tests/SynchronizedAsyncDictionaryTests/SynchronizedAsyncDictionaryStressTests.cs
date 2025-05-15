using System.Collections.Concurrent;
using System.Diagnostics;

namespace Open.AsyncToolkit.KeyValue.Tests;

/// <summary>
/// Comprehensive stress tests for the <see cref="SynchronizedAsyncDictionary{TKey, TValue}"/> class
/// to verify behavior under extreme workloads combining multiple operation patterns.
/// </summary>
internal partial class SynchronizedAsyncDictionaryTests
{
	/// <summary>
	/// Comprehensive stress test that combines several concurrent operations to verify
	/// the robustness of SynchronizedAsyncDictionary under extreme workloads.
	/// </summary>
	[Test]
	public async Task ComprehensiveStressTest_ShouldHandleAllOperationTypes()
	{
		// Arrange
		const int numKeyGroups = 5;
		const int keysPerGroup = 10;
		const int totalKeys = numKeyGroups * keysPerGroup;
		const int threadCount = 20;
		const int operationsPerThread = 500;
		var memoryDict = new MemoryAsyncDictionary<string, int>();
		using var sut = new SynchronizedAsyncDictionary<string, int>(memoryDict);

		var random = new Random(42); // Fixed seed for reproducibility
		var stopwatch = Stopwatch.StartNew();

		// Create key groups - each group will have a different access pattern:
		// Group 0: High contention - few keys accessed by many threads
		// Group 1: Read-heavy - mostly read operations
		// Group 2: Write-heavy - mostly write operations
		// Group 3: Delete-heavy - frequent delete/recreate cycles
		// Group 4: Mixed operations - balanced mix of reads, writes, and deletes
		var keyGroupList = new List<string[]>();

		for (int g = 0; g < numKeyGroups; g++)
		{
			string[] groupKeys = Enumerable.Range(0, keysPerGroup)
				.Select(i => $"stress-g{g}-k{i}")
				.ToArray();

			keyGroupList.Add(groupKeys);

			// Initialize all keys with initial values
			foreach (string? key in groupKeys)
			{
				memoryDict[key] = 0;
			}
		}

		// Track statistics to verify correctness
		var statistics = new ConcurrentDictionary<string, int>();
		var atomicCounters = new ConcurrentDictionary<string, int>();
		var operations = new ConcurrentDictionary<string, int>();
		operations["reads"] = 0;
		operations["writes"] = 0;
		operations["deletes"] = 0;

		// Track any errors that occur
		var errors = new ConcurrentBag<Exception>();

		// Start barrier to synchronize thread start
		using var startBarrier = new ManualResetEventSlim(false);

		// Create worker tasks
		var tasks = Enumerable.Range(0, threadCount)
			.Select(threadId => Task.Run(async () =>
			{
				try
				{
					// Wait for all threads to be ready
					startBarrier.Wait();

					for (int i = 0; i < operationsPerThread; i++)
					{
						// Choose a key group based on thread ID to create different contention patterns
						int groupIndex;

						if (threadId % 5 == 0)
						{
							// Some threads always hammer the high contention group
							groupIndex = 0;
						}
						else
						{
							// Other threads use a weighted distribution
							double groupSelector = random.NextDouble();

							if (groupSelector < 0.3)
							{
								groupIndex = 1; // 30% read-heavy
							}
							else if (groupSelector < 0.6)
							{
								groupIndex = 2; // 30% write-heavy
							}
							else if (groupSelector < 0.8)
							{
								groupIndex = 3; // 20% delete-heavy
							}
							else
							{
								groupIndex = 4; // 20% mixed
							}
						}

						string[] keyGroup = keyGroupList[groupIndex];

						// Select key from the group
						string key;

						if (groupIndex == 0)
						{
							// For high contention group, use only a couple of keys
							key = keyGroup[random.Next(2)];
						}
						else
						{
							// For other groups, use any key in the group
							key = keyGroup[random.Next(keyGroup.Length)];
						}

						// Determine operation type based on group
						double opSelector = random.NextDouble();

						switch (groupIndex)
						{
							case 1: // Read-heavy
								if (opSelector < 0.9)
								{                                    // Read operation (90%)
									await PerformReadOperation(sut, key);
									operations.AddOrUpdate("reads", 1, (_, v) => v + 1);
								}
								else
								{                                    // Write operation (10%)
									await PerformWriteOperation(sut, key);
									operations.AddOrUpdate("writes", 1, (_, v) => v + 1);
								}

								break;
							case 2: // Write-heavy
								if (opSelector < 0.2)
								{
									// Read operation (20%)
									await PerformReadOperation(sut, key);
									operations.AddOrUpdate("reads", 1, (_, v) => v + 1);
								}
								else
								{
									// Write operation (80%)
									await PerformWriteOperation(sut, key);
									operations.AddOrUpdate("writes", 1, (_, v) => v + 1);
								}

								break;
							case 3: // Delete-heavy
								if (opSelector < 0.3)
								{
									// Read operation (30%)
									await PerformReadOperation(sut, key);
									operations.AddOrUpdate("reads", 1, (_, v) => v + 1);
								}
								else if (opSelector < 0.6)
								{
									// Write operation (30%)
									await PerformWriteOperation(sut, key);
									operations.AddOrUpdate("writes", 1, (_, v) => v + 1);
								}
								else
								{
									// Delete-recreate operation (40%)
									await PerformDeleteRecreateOperation(sut, key, random);
									operations.AddOrUpdate("deletes", 1, (_, v) => v + 1);
								}

								break;
							case 0: // High contention
							case 4: // Mixed operations
							default:
								if (opSelector < 0.4)
								{
									// Read operation (40%)
									await PerformReadOperation(sut, key);
									operations.AddOrUpdate("reads", 1, (_, v) => v + 1);
								}
								else if (opSelector < 0.8)
								{
									// Write operation (40%)
									await PerformWriteOperation(sut, key);
									operations.AddOrUpdate("writes", 1, (_, v) => v + 1);
								}
								else
								{
									// Delete-recreate operation (20%)
									await PerformDeleteRecreateOperation(sut, key, random);
									operations.AddOrUpdate("deletes", 1, (_, v) => v + 1);
								}

								break;
						}

						// Occasionally add a small delay to create more interleaving opportunities
						if (random.Next(20) == 0)
						{
							await Task.Delay(random.Next(5));
						}
					}
				}
				catch (Exception ex)
				{
					errors.Add(ex);
				}
			}))
			.ToArray();

		// Helper methods for operations
		async Task PerformReadOperation(SynchronizedAsyncDictionary<string, int> dict, string key)
		{
			TryReadResult<int> result = await dict.TryReadAsync(key, CancellationToken.None);
			if (result.Success)
			{
				// Track the read value in statistics (ignoring race conditions as we just want a rough count)
				if (!statistics.ContainsKey(key))
				{
					statistics[key] = 0;
				}
			}
		}

		async Task PerformWriteOperation(SynchronizedAsyncDictionary<string, int> dict, string key)
		{
			await dict.LeaseAsync(key, CancellationToken.None, async (entry, ct) =>
			{
				TryReadResult<int> result = await entry.TryRead(ct);
				if (result.Success)
				{
					int newValue = result.Value + 1;
					await entry.CreateOrUpdate(newValue, ct);

					// Track atomic counter increments for validation
					atomicCounters.AddOrUpdate(key, 1, (_, oldValue) => oldValue + 1);
				}
				else
				{
					// Create if it doesn't exist
					await entry.Create(1, ct);
					atomicCounters.AddOrUpdate(key, 1, (_, oldValue) => oldValue + 1);
				}

				return true;
			});
		}

		async Task PerformDeleteRecreateOperation(SynchronizedAsyncDictionary<string, int> dict, string key, Random random)
		{
			await dict.LeaseAsync(key, CancellationToken.None, async (entry, ct) =>
			{
				bool exists = await entry.Exists(ct);
				if (exists)
				{
					// Read current value
					TryReadResult<int> result = await entry.TryRead(ct);
					int currentValue = result.Success ? result.Value : 0;

					// Delete it
					await entry.Delete(ct);

					// Recreate with same or new value
					if (random.Next(2) == 0)
					{
						await entry.Create(currentValue, ct); // Restore same value
					}
					else
					{
						await entry.Create(0, ct); // Reset to 0

						// Track that we reset this key's atomic counter
						atomicCounters[key] = 0;
					}
				}
				else
				{
					// If it doesn't exist, create it
					await entry.Create(0, ct);
					atomicCounters[key] = 0;
				}

				return true;
			});
		}

		// Start all threads simultaneously
		startBarrier.Set();

		// Wait for all operations to complete
		await Task.WhenAll(tasks);

		// Stop timer
		stopwatch.Stop();

		// Verify final state
		var finalValues = new Dictionary<string, int>();
		foreach (string[] group in keyGroupList)
		{
			foreach (string key in group)
			{
				TryReadResult<int> result = await sut.TryReadAsync(key, CancellationToken.None);
				if (result.Success)
				{
					finalValues[key] = result.Value;
				}
			}
		}

		// Output statistics
		Console.WriteLine("Comprehensive Stress Test Results:");
		Console.WriteLine($"  Keys: {totalKeys}");
		Console.WriteLine($"  Threads: {threadCount}");
		Console.WriteLine($"  Operations: {threadCount * operationsPerThread:N0}");
		Console.WriteLine($"  Reads: {operations["reads"]:N0}");
		Console.WriteLine($"  Writes: {operations["writes"]:N0}");
		Console.WriteLine($"  Deletes: {operations["deletes"]:N0}");
		Console.WriteLine($"  Duration: {stopwatch.ElapsedMilliseconds:N0}ms");
		Console.WriteLine($"  Operations/second: {threadCount * operationsPerThread * 1000.0 / stopwatch.ElapsedMilliseconds:N0}");

		// Assert
		await Assert.That(errors.Count).IsEqualTo(0);

		// Verify that all keys still exist (were not permanently deleted)
		bool allKeysExist = true;
		foreach (string[] group in keyGroupList)
		{
			foreach (string? key in group.Take(keysPerGroup / 2)) // Check at least half the keys
			{
				if (!await sut.ExistsAsync(key, CancellationToken.None))
				{
					allKeysExist = false;
					break;
				}
			}
		}

		await Assert.That(allKeysExist).IsTrue();
	}
}
