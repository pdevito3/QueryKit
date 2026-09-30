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

    public static List<SortExpressionInfo<T>> ParseSort<T>(string input, IQueryKitConfiguration? config = null)
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
        var parts = sortClause.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

        var propertyName = parts.Length > 0 ? parts[0] : sortClause;
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

        var reference = PropertyResolver.Resolve(typeof(T), propertyName, config);
        if (reference.Kind != PropertyReferenceKind.CustomOperation && !reference.CanSort)
        {
            return new SortExpressionInfo<T>
            {
                Expression = null,
                IsAscending = true
            };
        }

        var parameter = Expression.Parameter(typeof(T), "x");
        var sortExpressionBody = CreateSortExpressionBody(parameter, reference);
        
        var isAscending = direction == Ascending;
        return new SortExpressionInfo<T>
        {
            Expression = Expression.Lambda<Func<T, object>>(Expression.Convert(sortExpressionBody, typeof(object)), parameter),
            IsAscending = isAscending
        };
    }

    // Builds the sort from the same resolved reference that the CanSort check used.
    private static Expression CreateSortExpressionBody(ParameterExpression parameter, PropertyReference reference)
    {
        if (reference.Kind == PropertyReferenceKind.DerivedProperty)
        {
            // Replace the parameter in the derived expression with our current parameter
            var parameterReplacer = new ParameterReplacer(parameter);
            return parameterReplacer.Visit(reference.Mapping!.DerivedExpression!);
        }

        // Handle regular properties with null-safe navigation
        var result = reference.Kind == PropertyReferenceKind.Member
            ? CreateNullSafePropertyExpression(parameter, reference.Path.Split('.'), 0)
            : null;
        if (result == null)
            throw new SortParsingException(reference.Text);

        return result;
    }

    private static Expression? CreateNullSafePropertyExpression(Expression currentExpr, string[] propertyNames, int index)
    {
        if (index >= propertyNames.Length)
            return currentExpr;

        var propName = propertyNames[index];
        var propertyInfo = GetPropertyInfo(currentExpr.Type, propName);
        if (propertyInfo == null)
            return null;

        var propertyAccess = Expression.PropertyOrField(currentExpr, propertyInfo.Name);

        // If this is the last property in the chain, just return the access
        if (index == propertyNames.Length - 1)
            return propertyAccess;

        // Get the rest of the property chain
        var restOfChain = CreateNullSafePropertyExpression(propertyAccess, propertyNames, index + 1);
        if (restOfChain == null)
            return null;

        // Check if the current property type is a reference type (could be null)
        var propertyType = propertyInfo.PropertyType;
        var isNullableType = !propertyType.IsValueType || Nullable.GetUnderlyingType(propertyType) != null;

        if (isNullableType)
        {
            // It's a reference type or nullable value type - add null check
            // Generate: property == null ? (TargetType)null : <rest of chain>
            var targetType = restOfChain.Type;
            var nullableTargetType = targetType.IsValueType && Nullable.GetUnderlyingType(targetType) == null
                ? typeof(Nullable<>).MakeGenericType(targetType)
                : targetType;

            var nullCheck = Expression.Equal(propertyAccess, Expression.Constant(null, propertyType));
            var nullValue = Expression.Constant(null, nullableTargetType);

            // Convert rest of chain to nullable if needed
            var convertedRest = targetType != nullableTargetType
                ? Expression.Convert(restOfChain, nullableTargetType)
                : restOfChain;

            return Expression.Condition(nullCheck, nullValue, convertedRest);
        }

        // Non-nullable value type - continue without null check
        return restOfChain;
    }

    private static PropertyInfo? GetPropertyInfo(Type type, string propertyName)
    {
        return type.GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
    }
}
