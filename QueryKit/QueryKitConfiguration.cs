namespace QueryKit;

using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using Operators;

public interface IQueryKitConfiguration
{
    QueryKitPropertyMappings PropertyMappings { get; }
    ComparisonOperatorAliases? ComparisonOperatorAliases { get; }
    string? GetPropertyPathByQueryName(string? propPath);
    bool IsPropertySortable(string? propertyName);
}

public class ComparisonOperatorAliases
{
    public string EqualsOperator { get; set; } = ComparisonOperator.EqualsOperator().Operator();
    public string NotEqualsOperator { get; set; } = ComparisonOperator.NotEqualsOperator().Operator();
    public string GreaterThanOperator { get; set; } = ComparisonOperator.GreaterThanOperator().Operator();
    public string LessThanOperator { get; set; } = ComparisonOperator.LessThanOperator().Operator();
    public string GreaterThanOrEqualOperator { get; set; } = ComparisonOperator.GreaterThanOrEqualOperator().Operator();
    public string LessThanOrEqualOperator { get; set; } = ComparisonOperator.LessThanOrEqualOperator().Operator();
    public string ContainsOperator { get; set; } = ComparisonOperator.ContainsOperator().Operator();
    public string StartsWithOperator { get; set; } = ComparisonOperator.StartsWithOperator().Operator();
    public string EndsWithOperator { get; set; } = ComparisonOperator.EndsWithOperator().Operator();
    public string NotContainsOperator { get; set; } = ComparisonOperator.NotContainsOperator().Operator();
    public string NotStartsWithOperator { get; set; } = ComparisonOperator.NotStartsWithOperator().Operator();
    public string NotEndsWithOperator { get; set; } = ComparisonOperator.NotEndsWithOperator().Operator();
    public string InOperator { get; set; } = ComparisonOperator.InOperator().Operator();
    public string CaseInsensitiveAppendix { get; set; } = "*";

    public string ReplaceAliases(string input)
    {
        var aliasedOperators = ComparisonOperator.GetAliasMatches(this);
        foreach (var comparisonAliasMatch in aliasedOperators)
        {
            var escapedAlias = Regex.Escape(comparisonAliasMatch.Alias);
            var regex = new Regex($@"(?<=\s|^){escapedAlias}(?=\s|$)", RegexOptions.IgnoreCase);
            input = regex.Replace(input, comparisonAliasMatch.Operator);
        }
        
        return input;
    }
}

public class QueryKitSettings
{
    public QueryKitPropertyMappings PropertyMappings { get; set; } = new QueryKitPropertyMappings();
    public ComparisonOperatorAliases ComparisonAliases { get; set; } = new ComparisonOperatorAliases();
    
    public QueryKitPropertyMapping<TModel> Property<TModel>(Expression<Func<TModel, object>>? propertySelector)
    {
        return PropertyMappings.Property(propertySelector);
    }
}

public class QueryKitConfiguration : IQueryKitConfiguration
{
    public QueryKitPropertyMappings PropertyMappings { get; }
    public ComparisonOperatorAliases ComparisonOperatorAliases { get; }

    public QueryKitConfiguration(Action<QueryKitSettings> configureSettings)
    {
        var settings = new QueryKitSettings();
        configureSettings(settings);

        PropertyMappings = settings.PropertyMappings;
        ComparisonOperatorAliases = settings.ComparisonAliases;
    }
    
    public string? GetPropertyPathByQueryName(string? queryName)
    {
        return PropertyMappings.GetPropertyPathByQueryName(queryName);
    }

    public bool IsPropertySortable(string? propertyName)
    {
        return PropertyMappings.GetPropertyInfo(propertyName)?.CanSort ?? true;
    }
}
