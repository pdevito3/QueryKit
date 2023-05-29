namespace QueryKit.Configuration;

using System.Text.RegularExpressions;
using QueryKit.Operators;

internal static class QueryKitConfigurationExtensions
{
    internal static string ReplaceComparisonAliases(this IQueryKitConfiguration configuration, string input)
    {
        var aliasedOperators = ComparisonOperator.GetAliasMatches(configuration);
        foreach (var comparisonAliasMatch in aliasedOperators)
        {
            var escapedAlias = Regex.Escape(comparisonAliasMatch.Alias);
            var regex = new Regex($@"(?<=\s|^){escapedAlias}(?=\s|$)", RegexOptions.IgnoreCase);
            input = regex.Replace(input, comparisonAliasMatch.Operator);
        }
        
        return input;
    }
    
    internal static string ReplaceLogicalAliases(this IQueryKitConfiguration configuration, string input)
    {
        var aliasedOperators = LogicalOperator.GetAliasMatches(configuration);
        foreach (var logicalAliasMatch in aliasedOperators)
        {
            var escapedAlias = Regex.Escape(logicalAliasMatch.Alias);
            var regex = new Regex($@"(?<=\s|^){escapedAlias}(?=\s|$)", RegexOptions.IgnoreCase);
            input = regex.Replace(input, logicalAliasMatch.Operator);
        }
        
        return input;
    }    
    
    internal static string? GetPropertyPathByQueryName(this IQueryKitConfiguration configuration, string? queryName)
    {
        return configuration.PropertyMappings.GetPropertyPathByQueryName(queryName);
    }

    internal static bool IsPropertySortable(this IQueryKitConfiguration configuration, string? propertyName)
    {
        return configuration.PropertyMappings.GetPropertyInfo(propertyName)?.CanSort ?? true;
    }
}