using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Open.BlobStorageAdapter;

/// <summary>
/// A lightweight, immutable struct that functions like an async lazy loader.
/// Can be awaited to retrieve the value, and includes an Exists property to determine if a value is available.
/// </summary>
/// <typeparam name="T">The type of value that will be lazily loaded.</typeparam>
public readonly record struct AsyncLazyValue<T> : IEquatable<AsyncLazyValue<T>>
{
    private readonly Lazy<Task<T?>>? _valueFactory;

    /// <summary>
    /// Gets whether a value exists for this instance.
    /// </summary>
    public bool Exists => _valueFactory != null;

    /// <summary>
    /// Creates a new instance of <see cref="AsyncLazyValue{T}"/> with no value.
    /// </summary>
    public static AsyncLazyValue<T> Empty => new();

    /// <summary>
    /// Creates a new instance of <see cref="AsyncLazyValue{T}"/> with the specified value factory.
    /// </summary>
    /// <param name="valueFactory">A function that returns a Task yielding the value.</param>
    public AsyncLazyValue(Func<Task<T?>> valueFactory)
    {
        _valueFactory = valueFactory != null ? new Lazy<Task<T?>>(valueFactory) : null;
    }

    /// <summary>
    /// Creates a new instance of <see cref="AsyncLazyValue{T}"/> with the specified value.
    /// </summary>
    /// <param name="value">The value to wrap.</param>
    public AsyncLazyValue(T? value)
    {
        _valueFactory = value != null ? new Lazy<Task<T?>>(Task.FromResult(value)) : null;
    }

    /// <summary>
    /// Gets the awaiter for this AsyncLazyValue.
    /// </summary>
    public TaskAwaiter<T?> GetAwaiter()
    {
        return _valueFactory != null
            ? _valueFactory.Value.GetAwaiter()
            : Task.FromResult<T?>(default).GetAwaiter();
    }

    /// <summary>
    /// Configures an awaiter for this AsyncLazyValue.
    /// </summary>
    /// <param name="continueOnCapturedContext">Whether to continue on the captured context.</param>
    public ConfiguredTaskAwaitable<T?> ConfigureAwait(bool continueOnCapturedContext)
    {
        return _valueFactory != null
            ? _valueFactory.Value.ConfigureAwait(continueOnCapturedContext)
            : Task.FromResult<T?>(default).ConfigureAwait(continueOnCapturedContext);
    }

    /// <summary>
    /// Tries to get the value if it exists.
    /// </summary>
    /// <param name="value">When this method returns, contains the value if it exists, or default if it doesn't.</param>
    /// <returns>True if the value exists; otherwise, false.</returns>
    public async ValueTask<(bool exists, T? value)> TryGetValueAsync(CancellationToken cancellationToken = default)
    {
        if (!Exists)
            return (false, default);

        cancellationToken.ThrowIfCancellationRequested();
        T? result = await _valueFactory!.Value.ConfigureAwait(false);
        return (result != null, result);
    }

    /// <summary>
    /// Creates an AsyncLazyValue from a value.
    /// </summary>
    public static implicit operator AsyncLazyValue<T>(T? value) => new(value);

    /// <summary>
    /// Returns a string representation of this instance.
    /// </summary>
    public override string ToString() => Exists ? "Value exists (not yet loaded)" : "No value";
}