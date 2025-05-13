namespace Open.AsyncToolkit.HashedRepository;

/// <summary>
/// Validates and encapsulates a <see cref="Guid"/> value.
/// </summary>
#pragma warning disable IDE0079 // Remove unnecessary suppression
[SuppressMessage("Usage", "CA2225:Operator overloads have named alternates",
	Justification = "Unnecessary for this implementation.")]
#pragma warning restore IDE0079 // Remove unnecessary suppression
public readonly record struct GuidString : IEquatable<GuidString>, IEquatable<Guid>, IEquatable<string>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="GuidString"/> struct.
	/// </summary>
	/// <param name="value">The <see cref="Guid"/> value.</param>
	public GuidString(Guid value)
		=> Value = value;

	/// <summary>
	/// The <see cref="Guid"/> value.
	/// </summary>
	public Guid Value { get; }

	/// <inheritdoc />
	public override string ToString()
		=> Value.ToString();

	/// <summary>
	/// Creates a new instance of the <see cref="GuidString"/> struct from a <see cref="string"/>.
	/// </summary>
	/// <param name="value"></param>
	/// <returns>A new instance of the <see cref="GuidString"/> struct.</returns>
	/// <exception cref="ArgumentNullException">If <paramref name="value"/> is <see langword="null"/>.</exception>
	/// <exception cref="FormatException">If <paramref name="value"/> is not a valid <see cref="Guid"/>.</exception>
	public static GuidString FromString(string value)
	{
		if (value is null) throw new ArgumentNullException(nameof(value));
		return new(Guid.Parse(value));
	}

	/// <inheritdoc />
	public bool Equals(Guid other)
		=> Value == other;

	/// <inheritdoc />
	public bool Equals(string? other)
		=> !string.IsNullOrWhiteSpace(other)
		&& Guid.TryParse(other, out var g) && Equals(g);

	/// <summary>
	/// Implicitly converts a <see cref="Guid"/> to a <see cref="GuidString"/>.
	/// </summary>
	public static implicit operator GuidString(Guid value) => new(value);

	/// <summary>
	/// Implicitly converts a <see cref="string"/> to a <see cref="GuidString"/>.
	/// </summary>
	/// <param name="value"></param>
	public static implicit operator GuidString(string value) => FromString(value);

	/// <summary>
	/// Implicitly converts a <see cref="GuidString"/> to a <see cref="Guid"/> or <see langword="string"/>.
	/// </summary>
	/// <param name="value"></param>
	public static implicit operator Guid(GuidString value) => value.Value;

	/// <summary>
	/// Implicitly converts a <see cref="GuidString"/> to a <see langword="string"/>.
	/// </summary>
	/// <param name="value"></param>
	public static implicit operator string(GuidString value) => value.ToString();
}
