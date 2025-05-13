namespace Open.AsyncToolkit.HashedRepository;

/// <summary>
/// Provides a unique ID as a <see langword="string"/>.
/// </summary>
public class GuidStringProvider
	: IUniqueIdProvider<GuidString>
{
	/// <inheritdoc />
	public GuidString NewId() => Guid.NewGuid();

	/// <summary>
	/// Singleton instance of <see cref="GuidStringProvider"/>.
	/// </summary>
	public static GuidStringProvider Instance { get; } = new();
}