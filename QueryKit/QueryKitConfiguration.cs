namespace QueryKit;

using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using Operators;

public interface IQueryKitConfiguration
{
    QueryKitPropertyMappings PropertyMappings { get; }
    public string EqualsOperator { get; set; }
    public string NotEqualsOperator { get; set; }
    public string GreaterThanOperator { get; set; }
    public string LessThanOperator { get; set; }
    public string GreaterThanOrEqualOperator { get; set; }
    public string LessThanOrEqualOperator { get; set; }
    public string ContainsOperator { get; set; }
    public string StartsWithOperator { get; set; }
    public string EndsWithOperator { get; set; }
    public string NotContainsOperator { get; set; }
    public string NotStartsWithOperator { get; set; }
    public string NotEndsWithOperator { get; set; }
    public string InOperator { get; set; }
    public string CaseInsensitiveAppendix { get; set; }
    public bool AllowUnknownProperties { get; set; }
    
    internal string? GetPropertyPathByQueryName(string? propPath);
    internal bool IsPropertySortable(string? propertyName);
    internal string ReplaceComparisonAliases(string input);
    internal string ReplaceLogicalAliases(string input);
}

public class QueryKitSettings
{
    public QueryKitPropertyMappings PropertyMappings { get; set; } = new QueryKitPropertyMappings();
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
    public string AndOperator { get; set; } = LogicalOperator.AndOperator.Operator();
    public string OrOperator { get; set; } = LogicalOperator.OrOperator.Operator();
    public string CaseInsensitiveAppendix { get; set; } = "*";
    public bool AllowUnknownProperties { get; set; }
    
    public QueryKitPropertyMapping<TModel> Property<TModel>(Expression<Func<TModel, object>>? propertySelector)
    {
        return PropertyMappings.Property(propertySelector);
    }
}

public class QueryKitConfiguration : IQueryKitConfiguration
{
    public QueryKitPropertyMappings PropertyMappings { get; }
    public string EqualsOperator { get; set; }
    public string NotEqualsOperator { get; set; }
    public string GreaterThanOperator { get; set; }
    public string LessThanOperator { get; set; }
    public string GreaterThanOrEqualOperator { get; set; }
    public string LessThanOrEqualOperator { get; set; }
    public string ContainsOperator { get; set; }
    public string StartsWithOperator { get; set; }
    public string EndsWithOperator { get; set; }
    public string NotContainsOperator { get; set; }
    public string NotStartsWithOperator { get; set; }
    public string NotEndsWithOperator { get; set; }
    public string InOperator { get; set; }
    public string CaseInsensitiveAppendix { get; set; }
    public string AndOperator { get; set; }
    public string OrOperator { get; set; }
    public bool AllowUnknownProperties { get; set; } = false;

    public string ReplaceComparisonAliases(string input)
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
    
    public string ReplaceLogicalAliases(string input)
    {
        var aliasedOperators = LogicalOperator.GetAliasMatches(this);
        foreach (var logicalAliasMatch in aliasedOperators)
        {
            var escapedAlias = Regex.Escape(logicalAliasMatch.Alias);
            var regex = new Regex($@"(?<=\s|^){escapedAlias}(?=\s|$)", RegexOptions.IgnoreCase);
            input = regex.Replace(input, logicalAliasMatch.Operator);
        }
        
        return input;
    }

    public QueryKitConfiguration(Action<QueryKitSettings> configureSettings)
    {
        var settings = new QueryKitSettings();
        configureSettings(settings);

        PropertyMappings = settings.PropertyMappings;
        EqualsOperator = settings.EqualsOperator;
        NotEqualsOperator = settings.NotEqualsOperator;
        GreaterThanOperator = settings.GreaterThanOperator;
        LessThanOperator = settings.LessThanOperator;
        GreaterThanOrEqualOperator = settings.GreaterThanOrEqualOperator;
        LessThanOrEqualOperator = settings.LessThanOrEqualOperator;
        ContainsOperator = settings.ContainsOperator;
        StartsWithOperator = settings.StartsWithOperator;
        EndsWithOperator = settings.EndsWithOperator;
        NotContainsOperator = settings.NotContainsOperator;
        NotStartsWithOperator = settings.NotStartsWithOperator;
        NotEndsWithOperator = settings.NotEndsWithOperator;
        InOperator = settings.InOperator;
        CaseInsensitiveAppendix = settings.CaseInsensitiveAppendix;
        AndOperator = settings.AndOperator;
        OrOperator = settings.OrOperator;
        AllowUnknownProperties = settings.AllowUnknownProperties;
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
