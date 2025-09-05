namespace QueryKit;

using System.Linq.Expressions;
using System.Reflection;
using Configuration;
using Exceptions;
using Sprache;

public static class SortParser
{
    public class SortExpressionInfo<T>
    {
        public Expression<Func<T, object>>? Expression { get; set; }
        public bool IsAscending { get; set; }
    }
    
    private const string Ascending = "asc";
    private const string Descending = "desc";

    internal static List<SortExpressionInfo<T>> ParseSort<T>(string input, IQueryKitConfiguration? config = null)
    {
        if(string.IsNullOrWhiteSpace(input))
            return new List<SortExpressionInfo<T>>();
        
        var sortClauses = input.Split(',');

        var sortExpressions = new List<SortExpressionInfo<T>>();

        foreach (var sortClause in sortClauses)
        {
            var sortExpression = CreateSortExpression<T>(sortClause.Trim(), config);
            if (sortExpression.Expression != null)
            {
                sortExpressions.Add(sortExpression);
            }
        }

        return sortExpressions;
    }

    private static SortExpressionInfo<T> CreateSortExpression<T>(string sortClause, IQueryKitConfiguration? config = null)
    {
        var parts = sortClause.Split();

        var propertyName = parts[0];
        var direction = parts.Length > 1 ? parts[1].ToLowerInvariant() : Ascending;
        if (sortClause.StartsWith("-"))
        {
            direction = Descending;
            propertyName = propertyName.Substring(1);
        }

        if (direction != Ascending && direction != Descending)
        {
            throw new ArgumentException($"Invalid direction: {direction}. Allowed values are '{Ascending}' and '{Descending}'.");
        }

        var propertyPath = config?.GetPropertyPathByQueryName(propertyName) ?? propertyName;
        if (config != null && config.IsPropertySortable(propertyPath) == false)
        {
            return new SortExpressionInfo<T>
            {
                Expression = null,
                IsAscending = true
            };
        }

        var parameter = Expression.Parameter(typeof(T), "x");
        var sortExpressionBody = CreateSortExpressionBody(parameter, propertyName, config);
        
        if (sortExpressionBody == null)
        {
            return new SortExpressionInfo<T>
            {
                Expression = null,
                IsAscending = true
            };
        }
        
        var isAscending = direction == Ascending;
        return new SortExpressionInfo<T>
        {
            Expression = Expression.Lambda<Func<T, object>>(Expression.Convert(sortExpressionBody, typeof(object)), parameter),
            IsAscending = isAscending
        };
    }

    private static Expression? CreateSortExpressionBody(Expression parameter, string propertyName, IQueryKitConfiguration? config)
    {
        // First check if this is a derived property
        var derivedPropertyInfo = config?.PropertyMappings?.GetDerivedPropertyInfoByQueryName(propertyName);
        if (derivedPropertyInfo?.DerivedExpression != null)
        {
            // Replace the parameter in the derived expression with our current parameter
            var parameterReplacer = new ParameterReplacer((ParameterExpression)parameter);
            return parameterReplacer.Visit(derivedPropertyInfo.DerivedExpression);
        }

        // Handle regular properties
        var propertyPath = config?.GetPropertyPathByQueryName(propertyName) ?? propertyName;
        var propertyNames = propertyPath.Split('.');

        var propertyExpression = propertyNames.Aggregate(parameter, (expr, propName) =>
        {
            var propertyInfo = GetPropertyInfo(expr.Type, propName);
            if (propertyInfo == null)
            {
                return null;
            }
            var actualPropertyName = propertyInfo?.Name ?? propName;
            return Expression.PropertyOrField(expr, actualPropertyName);
        });

        if(propertyExpression == null)
            throw new SortParsingException(propertyName);
        
        return propertyExpression;
    }

    private static PropertyInfo? GetPropertyInfo(Type type, string propertyName)
    {
        return type.GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
    }
}
