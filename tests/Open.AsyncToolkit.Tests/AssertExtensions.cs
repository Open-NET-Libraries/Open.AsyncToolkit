using System.Runtime.CompilerServices;

namespace Open.AsyncToolkit.Tests;

/// <summary>
/// Provides extension methods for TUnit to enable more elegant exception testing.
/// </summary>
public static class AssertExtensions
{
	/// <summary>
	/// Tests that an asynchronous delegate throws an exception of the specified type.
	/// </summary>
	/// <typeparam name="TException">The type of exception expected to be thrown.</typeparam>
	/// <param name="testCode">The async code that should throw the exception.</param>
	/// <returns>A task that will complete when the assertion has completed.</returns>
	/// <example>
	/// <code>
	/// await ((Func&lt;Task&gt;)(async () => await someObject.AsyncMethodThatThrows())).ThrowsAsync&lt;ArgumentNullException&gt;();
	/// </code>
	/// </example>
	[OverloadResolutionPriority(2)]
	public static async ValueTask ThrowsAsync<TException>(this Func<ValueTask> testCode)
		where TException : Exception
	{
		bool exceptionThrown = false;
		Type exceptionType = typeof(TException);
		Exception? caughtException = null;

		try
		{
			await testCode();
		}
		catch (Exception ex)
		{
			caughtException = ex;
			exceptionThrown = ex.GetType() == exceptionType;
		}

		// Assert that an exception of the expected type was thrown
		await Assert.That(exceptionThrown).IsTrue();
		await Assert.That(caughtException).IsNotNull();
		await Assert.That(caughtException?.GetType()).IsEqualTo(exceptionType);
	}

	/// <inheritdoc cref="ThrowsAsync{TException}(Func{ValueTask})"/>/>
	[OverloadResolutionPriority(1)]
	public static ValueTask ThrowsAsync<TException>(this Func<Task> testCode)
		where TException : Exception
		=> ThrowsAsync<TException>(async () => await testCode());

	/// <summary>
	/// Tests that a delegate throws an exception of the specified type.
	/// </summary>
	/// <typeparam name="TException">The type of exception expected to be thrown.</typeparam>
	/// <param name="testCode">The code that should throw the exception.</param>
	/// <returns>A task that will complete when the assertion has completed.</returns>
	public static ValueTask Throws<TException>(this Action testCode)
		where TException : Exception
		=> ThrowsAsync<TException>(() =>
		{
			testCode();
			return ValueTask.CompletedTask;
		});
}