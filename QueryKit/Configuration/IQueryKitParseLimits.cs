namespace QueryKit.Configuration;

/// <summary>
/// The limits that the filter parser applies to a filter. A configuration that does not
/// implement this interface uses <see cref="QueryKitSettings.DefaultMaxInputLength"/> and
/// <see cref="QueryKitSettings.DefaultMaxNestingDepth"/>.
/// </summary>
public interface IQueryKitParseLimits
{
    int MaxNestingDepth { get; }
    int MaxInputLength { get; }
}
