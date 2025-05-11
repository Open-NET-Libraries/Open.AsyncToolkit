namespace Open.AsyncToolkit.HashedRepository;

/// <summary>
/// Provides a unique ID as a <see langword="string"/>.
/// </summary>
public class GuidStringProvider
	: IUniqueIdProvider<string>
{
	/// <inheritdoc />
	public string NewId() => Guid.NewGuid().ToString();

	/// <summary>
	/// Singleton instance of <see cref="GuidStringProvider"/>.
	/// </summary>
	public static GuidStringProvider Instance { get; } = new();
}