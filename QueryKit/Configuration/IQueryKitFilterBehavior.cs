namespace QueryKit.Configuration;

/// <summary>
/// The settings that control how the filter parser treats constant values and ignored clauses. A configuration
/// that does not implement this interface uses the defaults: values sent as parameters,
/// <see cref="Configuration.IgnoredClauseBehavior.Remove"/> for an ignored clause, and UTC for a date and time
/// value without an offset.
/// </summary>
public interface IQueryKitFilterBehavior
{
    bool ParameterizeFilterValues { get; }
    IgnoredClauseBehavior IgnoredClauseBehavior { get; }

    /// <summary>
    /// The kind of a <see cref="DateTime"/> filter value that has no offset. See
    /// <see cref="QueryKitSettings.DateTimeKindForValuesWithoutOffset"/>.
    /// </summary>
    DateTimeKind DateTimeKindForValuesWithoutOffset => DateTimeKind.Utc;
}
