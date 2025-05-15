namespace Open.AsyncToolkit.KeyValue.Tests;

/// <summary>
/// Base class for all tests of the <see cref="SynchronizedAsyncDictionary{TKey, TValue}"/> class.
/// Contains shared setup, teardown, and utility methods.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design",
	"CA1001:Types that own disposable fields should be disposable",
	Justification = "Cleanup method handles this.")]
internal partial class SynchronizedAsyncDictionaryTests
{
	// Constants used throughout tests
	protected const string TestKey = "test-key";
	protected const string TestValue = "test-value";

	private MemoryAsyncDictionary<string, string> _memoryDict = null!;
	private SynchronizedAsyncDictionary<string, string> _sut = null!;
#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable CA1859 // Use concrete types when possible for improved performance
	private ISynchronizedAsyncDictionary<string, string> _asyncDictionary = null!;
#pragma warning restore CA1859 // Use concrete types when possible for improved performance
#pragma warning restore IDE0079 // Remove unnecessary suppression

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
	protected static SynchronizedAsyncDictionary<string, TValue> CreateTestDictionary<TValue>(TValue initialValue = default!)
	{
		var memoryDict = new MemoryAsyncDictionary<string, TValue>();
		if (initialValue != null)
		{
			memoryDict[TestKey] = initialValue;
		}

		return new SynchronizedAsyncDictionary<string, TValue>(memoryDict);
	}

	// Helper method to execute a simple lease operation
	protected static ValueTask<TResult> ExecuteSimpleLeaseOperation<TResult>(
		ISynchronizedAsyncDictionary<string, string> dictionary,
		Func<IAsyncDictionaryEntry<string, string>, CancellationToken, ValueTask<TResult>> operation,
		CancellationToken cancellationToken = default)
		=> dictionary.LeaseAsync(TestKey, cancellationToken, operation);
}