using System.Collections.Concurrent;
using System.Diagnostics;

namespace Open.AsyncToolkit.KeyValue.Tests;

/// <summary>
/// Tests specifically for the lease functionality of <see cref="SynchronizedAsyncDictionary{TKey, TValue}"/>.
/// </summary>
public partial class SynchronizedAsyncDictionaryTests
{
	#region Lease Tests

	[Test]
	public async Task LeaseAsync_CancelledToken_ShouldThrowOperationCanceledException()
	{
		// Arrange
		const string key = "cancel-test-key";
		using var cts = new CancellationTokenSource();
		await cts.CancelAsync(); // Pre-cancel the token

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
			// Expected exception
		}
	}

	[Test]
	public async Task LeaseAsync_TokenCancelledDuringOperation_ShouldThrow()
	{
		// Arrange
		const string key = "token-cancel-during-op";
		using var cts = new CancellationTokenSource();

		// Set initial value
		_memoryDict[key] = TestValue;

		// Act - Start a long operation that will be cancelled
		ValueTask<bool> task = _asyncDictionary.LeaseAsync<bool>(
			key, cts.Token,
			async (entry, ct) =>
			{
				// Read the value (should succeed)
				TryReadResult<string> readResult = await entry.TryRead(ct);

				// Cancel the token during the operation
				await cts.CancelAsync();

				// This delay should be interrupted by cancellation
				try
				{
					await Task.Delay(1000, ct);
					return true; // Should not reach here
				}
				catch (OperationCanceledException)
				{
					throw; // Re-throw to test that the lease propagates this
				}
			});

		// Assert
		try
		{
			await task;
			Assert.Fail("Expected OperationCanceledException was not thrown");
		}
		catch (OperationCanceledException)
		{
			// Expected exception
		}
	}

	[Test]
	public async Task MultipleLeaseOperations_ShouldExecuteSerially()
	{
		// Arrange
		const string key = "serial-execution-key";
		const int operationCount = 5;
		const int operationDelayMs = 50;

		// Set initial value
		_memoryDict[key] = "initial";

		// Create a stopwatch to verify execution time
		var stopwatch = Stopwatch.StartNew();

		// Act - Queue up multiple lease operations on the same key
		var tasks = new List<Task<string>>();

		for (int i = 0; i < operationCount; i++)
		{
			int iterationValue = i;
			tasks.Add(_asyncDictionary.LeaseAsync(
				key, CancellationToken.None,
				async (entry, ct) =>
				{
					// Each operation takes a consistent time
					await Task.Delay(operationDelayMs, ct);

					// Update the value
					string newValue = $"updated-{iterationValue}";
					await entry.CreateOrUpdate(newValue, ct);

					return newValue;
				}).AsTask());
		}

		// Wait for all operations to complete
		await Task.WhenAll(tasks);

		// Stop the timer
		stopwatch.Stop();

		// Get the final value
		TryReadResult<string> finalResult = await _asyncDictionary.TryReadAsync(key, CancellationToken.None);

		// Assert

		// Verify all tasks completed successfully
		foreach (Task<string> task in tasks)
		{
			await Assert.That(task.Status).IsEqualTo(TaskStatus.RanToCompletion);
		}

		// If the operations executed serially, the total time should be at least 
		// operationCount * operationDelayMs (with some tolerance for overhead)
		await Assert.That(stopwatch.ElapsedMilliseconds >= operationCount * operationDelayMs * 0.9).IsTrue();

		// The final value should be from the last operation
		await Assert.That(finalResult.Success).IsTrue();
		await Assert.That(finalResult.Value).IsEqualTo($"updated-{operationCount - 1}");
	}

	[Test]
	public async Task NestedLeaseOperations_ShouldNotDeadlock()
	{
		// Arrange
		const string key1 = "nested-key-1";
		const string key2 = "nested-key-2";

		// Set initial values
		_memoryDict[key1] = "value1";
		_memoryDict[key2] = "value2";

		// Act - Execute a lease that contains another lease
		string result = await _asyncDictionary.LeaseAsync(
			key1, CancellationToken.None,
			async (entry1, ct) =>
			{
				// First lease operation
				TryReadResult<string> value1 = await entry1.TryRead(ct);

				// Execute a nested lease on a different key
				return await _asyncDictionary.LeaseAsync(
					key2, ct,
					async (entry2, innerCt) =>
					{
						// Second lease operation
						TryReadResult<string> value2 = await entry2.TryRead(innerCt);

						// Combine the values as proof both leases worked
						return value1.Success && value2.Success
							? $"{value1.Value}+{value2.Value}"
							: "failed";
					});
			});

		// Assert
		await Assert.That(result).IsEqualTo("value1+value2");
	}

	[Test]
	public async Task ParallelLeaseOperations_OnDifferentKeys_ShouldNotBlock()
	{
		// Arrange
		const int keyCount = 10000;
		const int delayMs = 20; // Each operation takes this long
		Console.WriteLine($"[ParallelLeaseOperations] Starting test with {keyCount} keys, {delayMs}ms delay per operation");

		string[] keys = Enumerable.Range(0, keyCount)
			.Select(i => $"parallel-key-{i}")
			.ToArray();

		// Initialize all keys
		foreach (string? key in keys)
		{
			_memoryDict[key] = "initial";
		}

		Console.WriteLine("[ParallelLeaseOperations] All keys initialized");

		// Capture start times for each task
		var startTimes = new ConcurrentDictionary<string, long>();
		var endTimes = new ConcurrentDictionary<string, long>();
		var taskDurations = new ConcurrentDictionary<string, long>();

		// Start a timer to measure total time
		var stopwatch = Stopwatch.StartNew();

		// Act - Start lease operations on all keys in parallel
		Console.WriteLine("[ParallelLeaseOperations] Scheduling lease operations on all keys in parallel");

		await Parallel.ForEachAsync(keys, async (key, _) =>
		{
			await _asyncDictionary
				.LeaseAsync(
					key, CancellationToken.None,
					async (entry, ct) =>
				{
					var localStopwatch = Stopwatch.StartNew();
					startTimes[key] = stopwatch.ElapsedMilliseconds;
					Console.WriteLine($"[ParallelLeaseOperations] Started operation on key '{key}' at {startTimes[key]}ms");

					// Each operation has the same delay
					await Task.Delay(delayMs, ct);

					// Update the key's value
					await entry.CreateOrUpdate($"{key}-updated", ct);

					endTimes[key] = stopwatch.ElapsedMilliseconds;
					taskDurations[key] = localStopwatch.ElapsedMilliseconds;
					Console.WriteLine($"[ParallelLeaseOperations] Completed operation on key '{key}' at {endTimes[key]}ms (took {taskDurations[key]}ms)");

					return true;
				})
				.ConfigureAwait(false);
		});

		// Stop the timer
		stopwatch.Stop();
		Console.WriteLine($"[ParallelLeaseOperations] All tasks completed in {stopwatch.ElapsedMilliseconds}ms");

		// Verify all keys were updated
		bool allUpdated = true;
		foreach (string? key in keys)
		{
			TryReadResult<string> result = await _asyncDictionary.TryReadAsync(key, CancellationToken.None);
			if (!result.Success || result.Value != $"{key}-updated")
			{
				Console.WriteLine($"[ParallelLeaseOperations] Key '{key}' was not updated correctly");
				allUpdated = false;
				break;
			}
		}

		// Log task timings for analysis
		Console.WriteLine("[ParallelLeaseOperations] Task timing details:");
		foreach (string key in keys)
		{
			Console.WriteLine($"  Key '{key}': Started at {startTimes[key]}ms, ended at {endTimes[key]}ms, duration {taskDurations[key]}ms");
		}

		// Calculate overlap statistics
		int overlapCount = 0;
		for (int i = 0; i < keys.Length; i++)
		{
			for (int j = i + 1; j < keys.Length; j++)
			{
				string key1 = keys[i];
				string key2 = keys[j];

				// Check if operations overlapped in time
				bool overlapped
					 = startTimes[key1] <= startTimes[key2] && endTimes[key1] >= startTimes[key2]
					|| startTimes[key2] <= startTimes[key1] && endTimes[key2] >= startTimes[key1];

				if (overlapped) overlapCount++;
			}
		}

		Console.WriteLine($"[ParallelLeaseOperations] Detected {overlapCount} overlapping operations out of {keys.Length * (keys.Length - 1) / 2} possible pairs");
		Console.WriteLine($"[ParallelLeaseOperations] Expected time for serial execution: {keyCount * delayMs}ms");
		Console.WriteLine($"[ParallelLeaseOperations] Actual execution time: {stopwatch.ElapsedMilliseconds}ms");
		Console.WriteLine($"[ParallelLeaseOperations] Parallel efficiency: {keyCount * delayMs / (double)stopwatch.ElapsedMilliseconds:P2}");

		// Assert

		// All keys should have been successfully updated
		await Assert.That(allUpdated).IsTrue();

		// If operations happened in parallel, the total time should be closer to 
		// a single operation time than to sequential time (keyCount * delayMs)
		await Assert.That(stopwatch.ElapsedMilliseconds < keyCount * delayMs * 0.5).IsTrue();
	}

	[Test]
	public async Task LeaseAsync_WithDisposedDictionary_ShouldThrowObjectDisposedException()
	{
		// Arrange - Create a dictionary and dispose it
		SynchronizedAsyncDictionary<string, string> localDict = CreateTestDictionary("test");
		localDict.Dispose();

		// Act & Assert
		try
		{
			await localDict.LeaseAsync<bool>(
				TestKey, CancellationToken.None,
				static async (entry, ct) =>
				{
					await Task.Delay(1, ct);
					return true;
				});

			Assert.Fail("Expected ObjectDisposedException was not thrown");
		}
		catch (ObjectDisposedException)
		{
			// Expected exception
		}
	}

	[Test]
	public async Task LeaseAsync_WithException_ShouldPropagateException()
	{
		// Arrange
		const string exceptionMessage = "Test exception in lease operation";

		// Act & Assert
		try
		{
			await _asyncDictionary.LeaseAsync<bool>(
				TestKey, CancellationToken.None,
				static async (entry, ct) =>
				{
					await Task.Delay(1, ct);
					throw new InvalidOperationException(exceptionMessage);
				});

			Assert.Fail("Expected InvalidOperationException was not thrown");
		}
		catch (InvalidOperationException ex)
		{
			await Assert.That(ex.Message).IsEqualTo(exceptionMessage);
		}
	}

	[Test]
	public async Task LeaseAsync_WithReturnValue_ShouldReturnCorrectValue()
	{
		// Arrange
		const string key = "lease-return-key";
		const string value = "initial-value";

		// Set initial value
		_memoryDict[key] = value;

		// Act - Run a lease operation that returns the value
		string result = await _asyncDictionary.LeaseAsync(
			key, CancellationToken.None,
			static async (entry, ct) =>
			{
				TryReadResult<string> readResult = await entry.TryRead(ct);
				return readResult.Success ? readResult.Value.ToUpperInvariant() : string.Empty;
			});

		// Assert - The operation should return the transformed value
		await Assert.That(result).IsEqualTo(value.ToUpperInvariant());
	}

	#endregion
}
