namespace QueryKit.Configuration;

/// <summary>
/// The settings that control how the filter parser treats constant values and ignored clauses. A configuration
/// that does not implement this interface uses the defaults: values sent as constants, and
/// <see cref="Configuration.IgnoredClauseBehavior.ReplaceWithTrue"/> for an ignored clause.
/// </summary>
public interface IQueryKitFilterBehavior
{
    bool ParameterizeFilterValues { get; }
    IgnoredClauseBehavior IgnoredClauseBehavior { get; }
}
