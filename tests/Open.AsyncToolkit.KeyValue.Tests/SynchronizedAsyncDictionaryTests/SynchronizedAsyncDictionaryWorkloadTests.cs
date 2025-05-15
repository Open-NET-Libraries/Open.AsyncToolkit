using System.Diagnostics;

namespace Open.AsyncToolkit.KeyValue.Tests;

/// <summary>
/// Advanced workload tests for the <see cref="SynchronizedAsyncDictionary{TKey, TValue}"/> class
/// focusing on specific operation patterns like read-heavy, write-heavy, and mixed workloads.
/// </summary>
internal partial class SynchronizedAsyncDictionaryTests
{
	#region Workload Test Utilities

	private const string WorkloadTestPrefix = "workload-key-";

	/// <summary>
	/// Test class to hold workload configuration parameters
	/// </summary>
	private class WorkloadParams
	{
		public string Name { get; set; } = "";
		public int KeyCount { get; set; }
		public int ThreadCount { get; set; }
		public int OperationsPerThread { get; set; }
		public double ReadRatio { get; set; }
		public double WriteRatio { get; set; }
		public double DeleteRatio { get; set; }
		public int MaxDelayMs { get; set; }
	}

	/// <summary>
	/// Stats class to track operation counts and timings
	/// </summary>
	private class WorkloadStats
	{
		public int ReadCount { get; set; }
		public int WriteCount { get; set; }
		public int DeleteCount { get; set; }
		public long TotalTimeMs { get; set; }
		public double OperationsPerSecond => (ReadCount + WriteCount + DeleteCount) * 1000.0 / TotalTimeMs;
	}

	/// <summary>
	/// Executes a workload with the specified parameters
	/// </summary>
	private static async Task<WorkloadStats> ExecuteWorkloadAsync(WorkloadParams parameters)
	{
		// Create a dedicated dictionary for this test
		var memoryDict = new MemoryAsyncDictionary<string, int>();
		using var sut = new SynchronizedAsyncDictionary<string, int>(memoryDict);

		// Create the keys
		string[] keys = Enumerable.Range(0, parameters.KeyCount)
			.Select(i => $"{WorkloadTestPrefix}{parameters.Name}-{i}")
			.ToArray();

		// Initialize all keys with value 0
		foreach (string? key in keys)
		{
			memoryDict[key] = 0;
		}

		// Create random generator with fixed seed for reproducibility
		var random = new Random(42);

		// Create thread-safe stats tracking
		var stats = new WorkloadStats();
		int readCount = 0;
		int writeCount = 0;
		int deleteCount = 0;

		// Signal to start all threads at once
		using var startSignal = new ManualResetEventSlim(false);

		// Create tasks for each thread
		var tasks = new List<Task>();
		for (int t = 0; t < parameters.ThreadCount; t++)
		{
			tasks.Add(Task.Run(async () =>
			{
				// Wait for start signal
				startSignal.Wait();

				for (int op = 0; op < parameters.OperationsPerThread; op++)
				{
					// Pick a random key
					string key = keys[random.Next(keys.Length)];

					// Determine operation based on distribution
					double opValue = random.NextDouble();

					if (opValue < parameters.ReadRatio)
					{
						// Read operation
						await sut.TryReadAsync(key, CancellationToken.None);
						Interlocked.Increment(ref readCount);
					}
					else if (opValue < parameters.ReadRatio + parameters.WriteRatio)
					{
						// Write operation (increment value)
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
						Interlocked.Increment(ref writeCount);
					}
					else
					{
						// Delete and recreate operation
						await sut.LeaseAsync(
							key, CancellationToken.None,
							async (entry, ct) =>
							{
								TryReadResult<int> result = await entry.TryRead(ct);
								if (result.Success)
								{
									await entry.Delete(ct);
									await Task.Delay(1, ct); // Small delay to increase contention
									await entry.Create(0, ct);
								}

								return true;
							});
						Interlocked.Increment(ref deleteCount);
					}

					// Add some random delay to simulate varying workloads
					if (parameters.MaxDelayMs > 0)
					{
						await Task.Delay(random.Next(parameters.MaxDelayMs));
					}
				}
			}));
		}

		// Start the timer
		var stopwatch = Stopwatch.StartNew();

		// Start all threads simultaneously
		startSignal.Set();

		// Wait for all tasks to complete
		await Task.WhenAll(tasks);

		// Stop timer
		stopwatch.Stop();

		// Compile stats
		stats.ReadCount = readCount;
		stats.WriteCount = writeCount;
		stats.DeleteCount = deleteCount;
		stats.TotalTimeMs = stopwatch.ElapsedMilliseconds;

		return stats;
	}

	#endregion

	#region Workload Tests

	[Test]
	public async Task ReadHeavyWorkload_ShouldHandleEfficiently()
	{
		// Arrange - 95% reads, 4% writes, 1% deletes
		var parameters = new WorkloadParams
		{
			Name = "read-heavy",
			KeyCount = 100,
			ThreadCount = 20,
			OperationsPerThread = 1000,
			ReadRatio = 0.95,
			WriteRatio = 0.04,
			DeleteRatio = 0.01,
			MaxDelayMs = 1
		};

		// Act
		WorkloadStats stats = await ExecuteWorkloadAsync(parameters);

		// Assert
		await Assert.That(stats.ReadCount + stats.WriteCount + stats.DeleteCount)
			.IsEqualTo(parameters.ThreadCount * parameters.OperationsPerThread);

		// Output stats for analysis
		Console.WriteLine($"Read-Heavy Workload Results:");
		Console.WriteLine($"  Operations: {parameters.ThreadCount * parameters.OperationsPerThread:N0} total");
		Console.WriteLine($"  Reads: {stats.ReadCount:N0} ({stats.ReadCount * 100.0 / (parameters.ThreadCount * parameters.OperationsPerThread):F1}%)");
		Console.WriteLine($"  Writes: {stats.WriteCount:N0} ({stats.WriteCount * 100.0 / (parameters.ThreadCount * parameters.OperationsPerThread):F1}%)");
		Console.WriteLine($"  Deletes: {stats.DeleteCount:N0} ({stats.DeleteCount * 100.0 / (parameters.ThreadCount * parameters.OperationsPerThread):F1}%)");
		Console.WriteLine($"  Time: {stats.TotalTimeMs:N0}ms");
		Console.WriteLine($"  Throughput: {stats.OperationsPerSecond:N0} ops/sec");
	}

	[Test]
	public async Task WriteHeavyWorkload_ShouldHandleEfficiently()
	{
		// Arrange - 20% reads, 70% writes, 10% deletes
		var parameters = new WorkloadParams
		{
			Name = "write-heavy",
			KeyCount = 100,
			ThreadCount = 20,
			OperationsPerThread = 500,
			ReadRatio = 0.20,
			WriteRatio = 0.70,
			DeleteRatio = 0.10,
			MaxDelayMs = 1
		};

		// Act
		WorkloadStats stats = await ExecuteWorkloadAsync(parameters);

		// Assert
		await Assert.That(stats.ReadCount + stats.WriteCount + stats.DeleteCount)
			.IsEqualTo(parameters.ThreadCount * parameters.OperationsPerThread);

		// Output stats for analysis
		Console.WriteLine($"Write-Heavy Workload Results:");
		Console.WriteLine($"  Operations: {parameters.ThreadCount * parameters.OperationsPerThread:N0} total");
		Console.WriteLine($"  Reads: {stats.ReadCount:N0} ({stats.ReadCount * 100.0 / (parameters.ThreadCount * parameters.OperationsPerThread):F1}%)");
		Console.WriteLine($"  Writes: {stats.WriteCount:N0} ({stats.WriteCount * 100.0 / (parameters.ThreadCount * parameters.OperationsPerThread):F1}%)");
		Console.WriteLine($"  Deletes: {stats.DeleteCount:N0} ({stats.DeleteCount * 100.0 / (parameters.ThreadCount * parameters.OperationsPerThread):F1}%)");
		Console.WriteLine($"  Time: {stats.TotalTimeMs:N0}ms");
		Console.WriteLine($"  Throughput: {stats.OperationsPerSecond:N0} ops/sec");
	}

	[Test]
	public async Task MixedWorkload_ShouldHandleEfficiently()
	{
		// Arrange - 50% reads, 40% writes, 10% deletes
		var parameters = new WorkloadParams
		{
			Name = "mixed",
			KeyCount = 100,
			ThreadCount = 20,
			OperationsPerThread = 500,
			ReadRatio = 0.50,
			WriteRatio = 0.40,
			DeleteRatio = 0.10,
			MaxDelayMs = 1
		};

		// Act
		WorkloadStats stats = await ExecuteWorkloadAsync(parameters);

		// Assert
		await Assert.That(stats.ReadCount + stats.WriteCount + stats.DeleteCount)
			.IsEqualTo(parameters.ThreadCount * parameters.OperationsPerThread);

		// Output stats for analysis
		Console.WriteLine($"Mixed Workload Results:");
		Console.WriteLine($"  Operations: {parameters.ThreadCount * parameters.OperationsPerThread:N0} total");
		Console.WriteLine($"  Reads: {stats.ReadCount:N0} ({stats.ReadCount * 100.0 / (parameters.ThreadCount * parameters.OperationsPerThread):F1}%)");
		Console.WriteLine($"  Writes: {stats.WriteCount:N0} ({stats.WriteCount * 100.0 / (parameters.ThreadCount * parameters.OperationsPerThread):F1}%)");
		Console.WriteLine($"  Deletes: {stats.DeleteCount:N0} ({stats.DeleteCount * 100.0 / (parameters.ThreadCount * parameters.OperationsPerThread):F1}%)");
		Console.WriteLine($"  Time: {stats.TotalTimeMs:N0}ms");
		Console.WriteLine($"  Throughput: {stats.OperationsPerSecond:N0} ops/sec");
	}

	[Test]
	public async Task HighContentionWorkload_ShouldNotDeadlock()
	{
		// Arrange - few keys with many threads competing for them
		var parameters = new WorkloadParams
		{
			Name = "contention",
			KeyCount = 5, // Very few keys to ensure contention
			ThreadCount = 30,
			OperationsPerThread = 100,
			ReadRatio = 0.30,
			WriteRatio = 0.60,
			DeleteRatio = 0.10,
			MaxDelayMs = 5
		};

		// Act
		WorkloadStats stats = await ExecuteWorkloadAsync(parameters);

		// Assert
		await Assert.That(stats.ReadCount + stats.WriteCount + stats.DeleteCount)
			.IsEqualTo(parameters.ThreadCount * parameters.OperationsPerThread);

		// Output stats for analysis
		Console.WriteLine($"High Contention Workload Results:");
		Console.WriteLine($"  Keys: {parameters.KeyCount} (very few to ensure contention)");
		Console.WriteLine($"  Threads: {parameters.ThreadCount}");
		Console.WriteLine($"  Operations: {parameters.ThreadCount * parameters.OperationsPerThread:N0} total");
		Console.WriteLine($"  Time: {stats.TotalTimeMs:N0}ms");
		Console.WriteLine($"  Throughput: {stats.OperationsPerSecond:N0} ops/sec");
	}

	[Test]
	public async Task CompareDifferentWorkloads_ShouldShowPerformanceCharacteristics()
	{
		// This test runs different types of workloads and compares their performance
		WorkloadParams[] workloads =
		[
			new WorkloadParams
			{
				Name = "mostly-reads",
				KeyCount = 100,
				ThreadCount = 10,
				OperationsPerThread = 1000,
				ReadRatio = 0.90,
				WriteRatio = 0.09,
				DeleteRatio = 0.01,
				MaxDelayMs = 1
			},
			new WorkloadParams
			{
				Name = "balanced",
				KeyCount = 100,
				ThreadCount = 10,
				OperationsPerThread = 1000,
				ReadRatio = 0.50,
				WriteRatio = 0.45,
				DeleteRatio = 0.05,
				MaxDelayMs = 1
			},
			new WorkloadParams
			{
				Name = "mostly-writes",
				KeyCount = 100,
				ThreadCount = 10,
				OperationsPerThread = 1000,
				ReadRatio = 0.10,
				WriteRatio = 0.80,
				DeleteRatio = 0.10,
				MaxDelayMs = 1
			},
			new WorkloadParams
			{
				Name = "high-contention",
				KeyCount = 10,
				ThreadCount = 10,
				OperationsPerThread = 1000,
				ReadRatio = 0.40,
				WriteRatio = 0.40,
				DeleteRatio = 0.20,
				MaxDelayMs = 2
			}
		];

		Console.WriteLine("Workload Comparison:");
		Console.WriteLine("---------------------------------------------------");
		Console.WriteLine("Workload         | Operations | Time (ms) | Ops/sec");
		Console.WriteLine("---------------------------------------------------");

		foreach (WorkloadParams? workload in workloads)
		{
			WorkloadStats stats = await ExecuteWorkloadAsync(workload);

			// Output comparative results
			int totalOps = workload.ThreadCount * workload.OperationsPerThread;
			Console.WriteLine($"{workload.Name,-16} | {totalOps,-10} | {stats.TotalTimeMs,-9} | {stats.OperationsPerSecond:N0}");
		}

		Console.WriteLine("---------------------------------------------------");
	}

	#endregion
}
