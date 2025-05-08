namespace Open.BlobStorageAdapter.AsyncItem;

public readonly record struct TryReadResult<TValue>
{
	/// <summary>
	/// Creates a successful result containing the specified value.
	/// </summary>
	/// <param name="value">The value that was successfully read.</param>
	/// <returns>A new <see cref="TryReadResult{TValue}"/> indicating success with the specified value.</returns>
	public static TryReadResult<TValue> Succeeded(TValue value) => new(true, value);

	/// <summary>
	/// A pre-initialized failed result with a default value.
	/// </summary>
	public static readonly TryReadResult<TValue> Failed = new(false, default!);

	/// <summary>
	/// Initializes a new instance of the <see cref="TryReadResult{TValue}"/> struct.
	/// </summary>
	/// <param name="success">A value indicating whether the operation succeeded.</param>
	/// <param name="value">The value obtained from the operation if successful; otherwise, default.</param>
	public TryReadResult(bool success, TValue value)
	{
		Success = success;
		Value = value;
	}

	/// <summary>
	/// Gets a value indicating whether the operation succeeded.
	/// </summary>
	public bool Success { get; }

	/// <summary>
	/// Gets the value obtained from the operation.
	/// </summary>
	/// <exception cref="InvalidOperationException">Thrown when attempting to access the value of a failed operation.</exception>
	public TValue Value
	{
		get
		{
			if (!Success)
				throw new InvalidOperationException("Cannot access Value when Success is false.");

			return field;
		}
	}
}
