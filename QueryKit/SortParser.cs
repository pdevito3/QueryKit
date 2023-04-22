namespace QueryKit;

using System.Linq.Expressions;
using System.Reflection;
using Sprache;

public static class SortParser
{
    public class SortExpressionInfo<T>
    {
        public Expression<Func<T, object>> Expression { get; set; }
        public bool IsAscending { get; set; }
    }

    public static List<SortExpressionInfo<T>> ParseSort<T>(string input, IQueryKitProcessorConfiguration config = null)
    {
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

    private static readonly Parser<string> Identifier =
        from first in Parse.Letter.Once()
        from rest in Parse.LetterOrDigit.XOr(Parse.Char('_')).Many()
        select new string(first.Concat(rest).ToArray());

    private static SortExpressionInfo<T> CreateSortExpression<T>(string sortClause, IQueryKitProcessorConfiguration config)
    {
        var parts = sortClause.Split();

        var propertyName = parts[0];
        var direction = parts.Length > 1 ? parts[1].ToLowerInvariant() : "asc";

        if (direction != "asc" && direction != "desc")
        {
            throw new ArgumentException($"Invalid direction: {direction}. Allowed values are 'asc' and 'desc'.");
        }

        var parameter = Expression.Parameter(typeof(T), "x");
        var sortExpressionBody = CreateSortExpressionBody(parameter, propertyName, config);

        if (sortExpressionBody == null)
        {
            return new SortExpressionInfo<T> { Expression = null, IsAscending = false };
        }

        var isAscending = direction == "asc";
        return new SortExpressionInfo<T>
        {
            Expression = Expression.Lambda<Func<T, object>>(Expression.Convert(sortExpressionBody, typeof(object)), parameter),
            IsAscending = isAscending
        };
    }

    private static Expression CreateSortExpressionBody(Expression parameter, string propertyName, IQueryKitProcessorConfiguration config)
    {
        var propertyPath = config?.GetPropertyPathByQueryName(propertyName) ?? propertyName;
        var propertyNames = propertyPath.Split('.');

        var propertyExpression = propertyNames.Aggregate(parameter, (expr, propName) =>
        {
            var propertyInfo = GetPropertyInfo(expr.Type, propName);
            var actualPropertyName = propertyInfo?.Name ?? propName;
            return Expression.PropertyOrField(expr, actualPropertyName);
        });

        return propertyExpression;
    }

    private static PropertyInfo GetPropertyInfo(Type type, string propertyName)
    {
        return type.GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
    }
}
