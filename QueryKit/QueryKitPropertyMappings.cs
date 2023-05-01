namespace QueryKit;

using System.Linq.Expressions;

public class QueryKitPropertyMappings
{
    private readonly Dictionary<string, QueryKitPropertyInfo> _propertyMappings = new();

    public QueryKitPropertyMapping<TModel> Property<TModel>(Expression<Func<TModel, object>>? propertySelector)
    {
        var fullPath = GetFullPropertyPath(propertySelector);
        var propertyInfo = new QueryKitPropertyInfo
        {
            Name = fullPath,
            CanFilter = true,
            CanSort = true,
            QueryName = fullPath
        };

        _propertyMappings[fullPath] = propertyInfo;

        return new QueryKitPropertyMapping<TModel>(propertyInfo);
    }

    private static string GetFullPropertyPath(Expression? expression)
    {
        if (expression!.NodeType == ExpressionType.Lambda)
        {
            var lambda = (LambdaExpression)expression;
            return GetFullPropertyPath(lambda.Body);
        }
        if (expression.NodeType == ExpressionType.Convert)
        {
            var unary = (UnaryExpression)expression;
            return GetFullPropertyPath(unary.Operand);
        }
        if (expression.NodeType == ExpressionType.MemberAccess)
        {
            var memberExpression = (MemberExpression)expression;
            return memberExpression?.Expression?.NodeType == ExpressionType.Parameter 
                ? memberExpression.Member.Name 
                : $"{GetFullPropertyPath(memberExpression?.Expression)}.{memberExpression?.Member?.Name}";
        }
        throw new NotSupportedException($"Expression type '{expression.NodeType}' is not supported.");
    }

    public QueryKitPropertyInfo? GetPropertyInfo(string? propertyName)
        =>  _propertyMappings.TryGetValue(propertyName, out var info) ? info : null;

    public QueryKitPropertyInfo? GetPropertyInfoByQueryName(string? queryName)
        => _propertyMappings.Values.FirstOrDefault(info => info.QueryName != null && info.QueryName.Equals(queryName, StringComparison.InvariantCultureIgnoreCase));

    public string? GetPropertyPathByQueryName(string? queryName)
        => GetPropertyInfoByQueryName(queryName)?.Name ?? null;
}

public class QueryKitPropertyMapping<TModel>
{
    private readonly QueryKitPropertyInfo _propertyInfo;

    public QueryKitPropertyMapping(QueryKitPropertyInfo propertyInfo)
    {
        _propertyInfo = propertyInfo;
    }

    public QueryKitPropertyMapping<TModel> PreventFilter()
    {
        _propertyInfo.CanFilter = false;
        return this;
    }

    public QueryKitPropertyMapping<TModel> PreventSort()
    {
        _propertyInfo.CanSort = false;
        return this;
    }

    public QueryKitPropertyMapping<TModel> HasQueryName(string queryName)
    {
        _propertyInfo.QueryName = queryName;
        return this;
    }
}

public class QueryKitPropertyInfo
{
    public string? Name { get; set; }
    public bool CanFilter { get; set; }
    public bool CanSort { get; set; }
    public string? QueryName { get; set; }
}