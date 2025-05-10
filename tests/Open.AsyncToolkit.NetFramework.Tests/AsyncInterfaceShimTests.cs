namespace Open.AsyncToolkit.NetFramework.Tests;

/// <summary>
/// Tests specifically targeting the Microsoft.Bcl.AsyncInterfaces shim for .NET Framework 4.8,
/// ensuring that async interfaces and ValueTask compatibility work correctly.
/// </summary>
public class AsyncInterfaceShimTests
{
    private readonly ITestOutputHelper _output;

    public AsyncInterfaceShimTests(ITestOutputHelper output)
    {
        _output = output;
    }

    #region ValueTask Shim Tests

    [Fact]
    public async Task ValueTask_CanBeUsed_InNetFramework48()
    {
        // This test verifies that ValueTask from Microsoft.Bcl.AsyncInterfaces works
        
        // Act
        ValueTask<int> task = GetValueAsync(42);
        int result = await task;
        
        // Assert
        Assert.Equal(42, result);
    }
    
    [Fact]
    public async Task ValueTask_CanBeAwaited_MultipleTimes()
    {
        // Setup
        int[] results = new int[3];
        
        // Act
        for (int i = 0; i < 3; i++)
        {
            ValueTask<int> task = GetValueAsync(i);
            results[i] = await task;
        }
        
        // Assert
        Assert.Equal(0, results[0]);
        Assert.Equal(1, results[1]);
        Assert.Equal(2, results[2]);
    }
    
    [Fact]
    public async Task ValueTask_SupportsConfigureAwait()
    {
        // This tests that ConfigureAwait works with the shim
        
        // Act
        ValueTask<int> task = GetValueAsync(42);
        int result = await task.ConfigureAwait(false);
        
        // Assert
        Assert.Equal(42, result);
    }
    
    private static ValueTask<int> GetValueAsync(int value)
    {
        return new ValueTask<int>(value);
    }

    #endregion
    
    #region Cancellation Tests

    [Fact]
    public async Task CancellationToken_WorksWithAsyncInterfaces()
    {
        // Setup
        using var cts = new CancellationTokenSource();
        
        // Act
        var result = await ProcessWithCancellationAsync(10, cts.Token);
        
        // Assert
        Assert.Equal(10, result);
    }
    
    [Fact]
    public async Task CancellationToken_ThrowsWhenCancelled()
    {
        // Setup
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        
        // Act/Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await ProcessWithCancellationAsync(10, cts.Token));
    }
    
    private static async ValueTask<int> ProcessWithCancellationAsync(int value, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Task.Delay(1, cancellationToken);
        return value;
    }

    #endregion
    
    #region Async Interface Compatibility Tests
    
    // Interface that uses async methods with ValueTask
    private interface IAsyncProcessor
    {
        ValueTask<int> ProcessAsync(int value, CancellationToken cancellationToken = default);
    }
    
    // Implementation of the async interface
    private class AsyncProcessor : IAsyncProcessor
    {
        public ValueTask<int> ProcessAsync(int value, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return new ValueTask<int>(value * 2);
        }
    }
    
    [Fact]
    public async Task AsyncInterface_CanBeImplementedAndUsed_InNetFramework48()
    {
        // Setup
        IAsyncProcessor processor = new AsyncProcessor();
        
        // Act
        int result = await processor.ProcessAsync(21);
        
        // Assert
        Assert.Equal(42, result);
    }
    
    #endregion
}
