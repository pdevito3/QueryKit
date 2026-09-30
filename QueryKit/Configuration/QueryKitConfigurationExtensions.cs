namespace QueryKit.Configuration;

using QueryKit.Exceptions;

internal static class QueryKitConfigurationExtensions
{
    internal static void ValidatePropertyDepth(this IQueryKitConfiguration? configuration, string? propertyPath)
    {
        if (configuration == null || string.IsNullOrEmpty(propertyPath))
            return;

        var depth = propertyPath.Count(c => c == '.');
        if (depth == 0)
            return;

        // Check for per-property override first
        var propertyMaxDepth = configuration.PropertyMappings?.GetMaxDepthForProperty(propertyPath);
        var effectiveMaxDepth = propertyMaxDepth ?? configuration.MaxPropertyDepth;

        if (effectiveMaxDepth.HasValue && depth > effectiveMaxDepth.Value)
        {
            throw new QueryKitPropertyDepthExceededException(propertyPath, depth, effectiveMaxDepth.Value);
        }
    }
}