using System;
using System.Threading.Tasks;
using TUnit;
using TUnit.Assertions.AssertionBuilders;

namespace Open.BlobStorageAdapter.Tests
{
    /// <summary>
    /// Extensions for TUnit to provide simpler exception testing.
    /// </summary>
    public static class AssertExtensions
    {
        /// <summary>
        /// Tests that a delegate throws an exception of the specified type.
        /// </summary>
        /// <typeparam name="TException">The type of exception expected to be thrown.</typeparam>
        /// <param name="testCode">The code that should throw the exception.</param>
        /// <returns>A task that will complete when the assertion has completed.</returns>
        public static async Task ThrowsAsync<TException>(this Action testCode) where TException : Exception
        {
            bool exceptionThrown = false;
            
            try
            {
                testCode();
            }
            catch (Exception ex)
            {
                exceptionThrown = ex.GetType() == typeof(TException);
            }
            
            // Simply assert that an exception of the expected type was thrown
            await Assert.That(exceptionThrown).IsTrue();
        }

        /// <summary>
        /// Tests that an async delegate throws an exception of the specified type.
        /// </summary>
        /// <typeparam name="TException">The type of exception expected to be thrown.</typeparam>
        /// <param name="testCode">The async code that should throw the exception.</param>
        /// <returns>A task that will complete when the assertion has completed.</returns>
        public static async Task ThrowsAsync<TException>(this Func<Task> testCode) where TException : Exception
        {
            bool exceptionThrown = false;
            
            try
            {
                await testCode();
            }
            catch (Exception ex)
            {
                exceptionThrown = ex.GetType() == typeof(TException);
            }
            
            // Simply assert that an exception of the expected type was thrown
            await Assert.That(exceptionThrown).IsTrue();
        }
    }
}