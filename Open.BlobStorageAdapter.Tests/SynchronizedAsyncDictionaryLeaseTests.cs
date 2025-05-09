using System.Reflection;

namespace Open.BlobStorageAdapter.Tests;

/// <summary>
/// Additional tests for verifying exclusive lease access in <see cref="SynchronizedAsyncDictionary{TKey, TValue}"/>.
/// </summary>
public class SynchronizedAsyncDictionaryLeaseTests
{
	private MemoryAsyncDictionary<string, string> _memoryDict = null!;
	private SynchronizedAsyncDictionary<string, string> _syncedDict = null!;

	[Before(Test)]
	public void Setup()
	{
		_memoryDict = new MemoryAsyncDictionary<string, string>();
		_syncedDict = _memoryDict.Synchronized();
	}

	[After(Test)]
	public void Cleanup() => _syncedDict.Dispose();

	/// <summary>
	/// Helper method to get the count of leases currently in the dictionary using reflection.
	/// This is necessary to verify that leases are properly recycled.
	/// </summary>
	/// <param name="dictionary">The synchronized dictionary to inspect.</param>
	/// <returns>The number of leases currently in the dictionary.</returns>
	private static int GetLeaseCount<TKey, TValue>(SynchronizedAsyncDictionary<TKey, TValue> dictionary)
		where TKey : notnull
	{
		// Use reflection to access the private _leases field
		FieldInfo leasesField = typeof(SynchronizedAsyncDictionary<TKey, TValue>)
			.GetField("_leases", BindingFlags.NonPublic | BindingFlags.Instance)
			?? throw new InvalidOperationException("Could not find _leases field using reflection");

		var leases = leasesField.GetValue(dictionary) as Dictionary<TKey, object>;
		return leases?.Count ?? 0;
	}

	[Test]
	public async Task OnlyOneLease_ShouldBeActiveAtATime_ForEachKey()
	{
		// Arrange
		const string testKey = "exclusive-access-key";
		const int concurrentAttempts = 10;

		// Create a flag to track when a lease is being held
		var leaseCurrentlyHeld = new ManualResetEventSlim(false);
		var concurrentAccessDetected = new ManualResetEventSlim(false);
		var startAllThreads = new ManualResetEventSlim(false);

		// Create multiple tasks that will try to acquire the lease simultaneously
		var tasks = new List<Task>();
		for (int i = 0; i < concurrentAttempts; i++)
		{
			tasks.Add(Task.Run(async () =>
			{
				// Wait for the signal to start
				startAllThreads.Wait();

				await _syncedDict.LeaseAsync(
					testKey, CancellationToken.None,
					async (entry, ct) =>
					{
						// We'll use a simpler approach to detect concurrent access
						// Check if another thread already marked the lease as held
						if (leaseCurrentlyHeld.IsSet)
						{
							// If we reach here, another thread has the lease active
							// which indicates a synchronization problem
							concurrentAccessDetected.Set();
						}

						// Mark that we now hold the lease
						leaseCurrentlyHeld.Set();

						// Simulate some work that takes time
						await Task.Delay(20, ct);

						// Release the lease marker
						leaseCurrentlyHeld.Reset();

						return true;
					});
			}));
		}

		// Start all threads simultaneously
		startAllThreads.Set();

		// Wait for all tasks to complete
		await Task.WhenAll(tasks);

		// Assert that no concurrent access was detected
		await Assert.That(concurrentAccessDetected.IsSet).IsFalse();
	}

	[Test]
	public async Task MultipleConcurrentLeases_ShouldRecycleSemaphores()
	{
		// Arrange
		const int keyCount = 10;
		const int operationsPerKey = 100;
		string[] keys = Enumerable.Range(1, keyCount).Select(i => $"pool-key-{i}").ToArray();

		// Run a first batch of operations to establish a baseline
		for (int i = 0; i < keyCount; i++)
		{
			string key = keys[i];
			await _syncedDict.LeaseAsync(
				key, CancellationToken.None,
				async (entry, ct) =>
				{
					await entry.CreateOrUpdate($"value-{i}", ct);
					return true;
				});
		}

		// Verify keys were created
		foreach (string? key in keys)
		{
			TryReadResult<string> result = await _syncedDict.TryReadAsync(key, CancellationToken.None);
			await Assert.That(result.Success).IsTrue();
		}

		// Check the initial lease count - should be 0 since operations are complete
		await Task.Delay(50); // Small delay to ensure cleanup completes
		int initialLeaseCount = GetLeaseCount(_syncedDict);
		await Assert.That(initialLeaseCount).IsEqualTo(0);

		// Now run many operations concurrently on the same keys
		var random = new Random(42);
		var tasks = new List<Task>();

		for (int i = 0; i < keyCount * operationsPerKey; i++)
		{
			string key = keys[random.Next(keyCount)];
			tasks.Add(Task.Run(async () =>
			{
				await _syncedDict.LeaseAsync(
					key,
					async entry =>
					{
						await Task.Delay(1);
						TryReadResult<string> result = await entry.TryRead();
					});
			}));
		}

		await Task.WhenAll(tasks);

		// Check the final lease count - should still be 0
		await Task.Delay(50); // Small delay to ensure cleanup completes
		int finalLeaseCount = GetLeaseCount(_syncedDict);
		await Assert.That(finalLeaseCount).IsEqualTo(0);
	}
}
