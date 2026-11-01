namespace QueryKit.Configuration;

/// <summary>
/// The setting that limits a filter and a sort to the properties that the configuration names. A configuration
/// that does not implement this interface lets a filter and a sort use every property of the type.
/// </summary>
public interface IQueryKitAllowList
{
    /// <summary>
    /// When true, a filter or a sort can use only a property that has a Property mapping, a derived property, or a
    /// custom operation. A mapping allows only its own path, so Author.Name does not allow Author or StartsAt.Ticks.
    /// Any other property is unknown.
    /// </summary>
    bool UseAllowList { get; }
}
