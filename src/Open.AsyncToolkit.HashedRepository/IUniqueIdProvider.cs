namespace Open.AsyncToolkit.HashedRepository;

/// <summary>
/// Provides a unique ID for a given type.
/// </summary>
/// <typeparam name="TUniqueId">The type of the unique ID.</typeparam>
public interface IUniqueIdProvider<TUniqueId>
	where TUniqueId : notnull
{
	/// <summary>
	/// Returns a new unique ID.
	/// </summary>
	TUniqueId NewId();
}
