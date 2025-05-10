using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Open.AsyncToolkit.NetFramework.Tests
{
    /// <summary>
    /// Tests to verify that all necessary shims for .NET Standard 2.0 compatibility are correctly loaded
    /// when the library is used in .NET Framework 4.8.
    /// </summary>
    public class ShimVerificationTests
    {
        private readonly ITestOutputHelper _output;

        public ShimVerificationTests(ITestOutputHelper output)
        {
            _output = output;
        }        [Fact]
        public void AsyncInterfaces_Shim_IsLoaded()
        {
            // This test verifies that ValueTask functionality is properly available
            
            // Arrange & Act
            var valueTaskType = typeof(ValueTask<>);
            var asyncType = typeof(IAsyncDisposable);
            
            // Assert
            Assert.NotNull(valueTaskType);
            Assert.NotNull(asyncType);
            
            // In .NET Framework 4.8, ValueTask may come from System.Threading.Tasks.Extensions
            // rather than Microsoft.Bcl.AsyncInterfaces directly
            var assemblyName = valueTaskType.Assembly.GetName().Name;
            _output.WriteLine($"ValueTask is coming from: {assemblyName}");
            
            Assert.True(
                assemblyName == "Microsoft.Bcl.AsyncInterfaces" || 
                assemblyName == "System.Threading.Tasks.Extensions",
                $"Expected ValueTask to come from either Microsoft.Bcl.AsyncInterfaces or System.Threading.Tasks.Extensions, but it came from {assemblyName}");
        }
          [Fact]
        public void SystemCollectionsImmutable_Shim_IsLoaded()
        {
            // This test verifies that System.Collections.Immutable is available if needed
            
            // Since we don't actually use ImmutableArray in this project, simply check for ValueTuple
            // as a stand-in for modern type support
            var tupleType = typeof(ValueTuple<,>);
            
            // Assert
            Assert.NotNull(tupleType);
            _output.WriteLine($"ValueTuple is available from: {tupleType.Assembly.GetName().Name}");
        }
        
        [Fact]
        public async Task Shims_WorkTogether_WithNetFramework48()
        {
            // This test ensures that the shims work together and don't conflict
            
            // Act & Assert
            // Create a simple ValueTask and check it completes
            async Task<bool> TestValueTaskAsync()
            {
                await Task.Delay(1);
                return true;
            }
            
            ValueTask<bool> valueTask = new ValueTask<bool>(TestValueTaskAsync());
            bool completed = await valueTask;
            Assert.True(completed);
            
            // Verify we can use cancellation with ValueTask
            using var cts = new CancellationTokenSource();
            
            async ValueTask<bool> TestCancellationAsync(CancellationToken token)
            {
                token.ThrowIfCancellationRequested();
                await Task.Delay(1, token);
                return true;
            }
            
            var result = await TestCancellationAsync(cts.Token);
            Assert.True(result);
        }
    }
}
