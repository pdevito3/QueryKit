namespace QueryKit;

using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
using Operators;

public class QueryKitPropertyMappings
{
    private readonly Dictionary<string, QueryKitPropertyInfo> _propertyMappings = new();
    private readonly Dictionary<string, QueryKitPropertyInfo> _derivedPropertyMappings = new();
    internal IReadOnlyDictionary<string, QueryKitPropertyInfo> PropertyMappings => _propertyMappings;
    internal IReadOnlyDictionary<string, QueryKitPropertyInfo> DerivedPropertyMappings => _derivedPropertyMappings;

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

    public QueryKitPropertyMapping<TModel> DerivedProperty<TModel>(Expression<Func<TModel, object>>? propertySelector)
    {
        var fullPath = GetFullPropertyPath(propertySelector);
        
        if (propertySelector == null)
            throw new ArgumentNullException(nameof(propertySelector));
        if(propertySelector.NodeType != ExpressionType.Lambda)
            throw new ArgumentException("Property selector must be a lambda expression", nameof(propertySelector));
        
        var body = propertySelector.Body;
        
        var propertyInfo = new QueryKitPropertyInfo
        {
            Name = fullPath,
            CanFilter = true,
            CanSort = true,
            QueryName = fullPath,
            DerivedExpression = body
        };

        _derivedPropertyMappings[fullPath] = propertyInfo;

        return new QueryKitPropertyMapping<TModel>(propertyInfo);
    }

    public string ReplaceAliasesWithPropertyPaths(string input)
    {
        var operators = ComparisonOperator.List.Select(x => x.Operator()).ToList();
        foreach (var alias in _propertyMappings.Values)
        {
            var propertyPath = GetPropertyPathByQueryName(alias.QueryName);
            if (!string.IsNullOrEmpty(propertyPath))
            {
                foreach (var op in operators)
                {
                    // Use regular expression to isolate left side of the expression
                    var regex = new Regex($@"\b{alias.QueryName}\b(?=\s*{op})", RegexOptions.IgnoreCase);
                    input = regex.Replace(input, propertyPath);
                }
            }
        }
        
        // foreach (var alias in _derivedPropertyMappings.Values)
        // {
        //     foreach (var op in operators)
        //     {
        //         // Use regular expression to isolate left side of the expression
        //         var regex = new Regex($@"\b{alias.QueryName}\b(?=\s*{op})", RegexOptions.IgnoreCase);
        //         input = regex.Replace(input, $"~||~||~{alias.QueryName}");
        //     }
        // }
        return input;
    }

    private static string GetFullPropertyPath(Expression? expression)
    {
        if (expression == null)
            throw new ArgumentNullException(nameof(expression));

        switch (expression.NodeType)
        {
            case ExpressionType.Call:
                var call = (MethodCallExpression)expression;
                if (call.Method.DeclaringType == typeof(Enumerable) && call.Method.Name == "Select" ||
                    call.Method.DeclaringType == typeof(Queryable) && call.Method.Name == "Select" ||
                    call.Method.DeclaringType == typeof(Enumerable) && call.Method.Name == "SelectMany" ||
                    call.Method.DeclaringType == typeof(Queryable) && call.Method.Name == "SelectMany")
                {
                    var propertyPath = GetFullPropertyPath(call.Arguments[1]);
                    var prevPath = GetFullPropertyPath(call.Arguments[0]);
                    return $"{prevPath}.{propertyPath}";
                }
                break;
            case ExpressionType.Lambda:
                var lambda = (LambdaExpression)expression;
                return GetFullPropertyPath(lambda.Body);
            case ExpressionType.Convert:
                var unary = (UnaryExpression)expression;
                return GetFullPropertyPath(unary.Operand);
            case ExpressionType.MemberAccess:
                var memberExpression = (MemberExpression)expression;
                return memberExpression?.Expression?.NodeType == ExpressionType.Parameter 
                    ? memberExpression.Member.Name 
                    : $"{GetFullPropertyPath(memberExpression?.Expression)}.{memberExpression?.Member?.Name}";
            case ExpressionType.Add:
            case ExpressionType.Subtract:
            case ExpressionType.Multiply:
            case ExpressionType.Divide:
            case ExpressionType.Modulo:
            case ExpressionType.And:
            case ExpressionType.Or:
            case ExpressionType.AndAlso:
            case ExpressionType.OrElse:
            case ExpressionType.GreaterThan:
            case ExpressionType.LessThan:
            case ExpressionType.GreaterThanOrEqual:
            case ExpressionType.LessThanOrEqual:
            case ExpressionType.Equal:
                var binary = (BinaryExpression)expression;
                var left = GetFullPropertyPath(binary.Left);
                var right = GetFullPropertyPath(binary.Right);
                var op = GetOperator(binary.NodeType);
                return $"{left} {op} {right}";
            case ExpressionType.Constant:
                var constant = (ConstantExpression)expression;
                return constant.Value?.ToString() ?? "";
            default:
                throw new NotSupportedException($"Expression type '{expression.NodeType}' is not supported.");
        }

        throw new NotSupportedException($"Expression type '{expression.NodeType}' is not supported.");
    }

    private static string GetOperator(ExpressionType nodeType)
    {
        return nodeType switch
        {
            ExpressionType.Add => "+",
            ExpressionType.Subtract => "-",
            ExpressionType.Multiply => "*",
            ExpressionType.Divide => "/",
            ExpressionType.Modulo => "%",
            ExpressionType.And => "&",
            ExpressionType.Or => "|",
            ExpressionType.AndAlso => "&&",
            ExpressionType.OrElse => "||",
            ExpressionType.GreaterThan => ">",
            ExpressionType.LessThan => "<",
            ExpressionType.GreaterThanOrEqual => ">=",
            ExpressionType.LessThanOrEqual => "<=",
            ExpressionType.Equal => "==",
            _ => throw new NotSupportedException($"Operator for expression type '{nodeType}' is not supported.")
        };
    }



    public QueryKitPropertyInfo? GetPropertyInfo(string? propertyName)
        =>  _propertyMappings.TryGetValue(propertyName, out var info) ? info : null;

    public QueryKitPropertyInfo? GetPropertyInfoByQueryName(string? queryName)
        => _propertyMappings.Values.FirstOrDefault(info => info.QueryName != null && info.QueryName.Equals(queryName, StringComparison.InvariantCultureIgnoreCase));

    public QueryKitPropertyInfo? GetDerivedPropertyInfoByQueryName(string? queryName)
        => _derivedPropertyMappings.Values.FirstOrDefault(info => info.QueryName != null && info.QueryName.Equals(queryName, StringComparison.InvariantCultureIgnoreCase));

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
    internal Expression DerivedExpression { get; set; }
}