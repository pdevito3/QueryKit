namespace QueryKit;

using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
using Operators;

public class QueryKitPropertyMappings
{
    private readonly Dictionary<string, QueryKitPropertyInfo> _propertyMappings = new();
    private readonly Dictionary<string, QueryKitPropertyInfo> _derivedPropertyMappings = new();
    private readonly Dictionary<string, QueryKitPropertyInfo> _customOperationMappings = new();
    internal IReadOnlyDictionary<string, QueryKitPropertyInfo> PropertyMappings => _propertyMappings;
    internal IReadOnlyDictionary<string, QueryKitPropertyInfo> DerivedPropertyMappings => _derivedPropertyMappings;
    internal IReadOnlyDictionary<string, QueryKitPropertyInfo> CustomOperationMappings => _customOperationMappings;

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

    public QueryKitCustomOperationMapping<TModel> CustomOperation<TModel>(Expression<Func<TModel, ComparisonOperator, object, bool>> operationExpression)
    {
        if (operationExpression == null)
            throw new ArgumentNullException(nameof(operationExpression));
        if (operationExpression.NodeType != ExpressionType.Lambda)
            throw new ArgumentException("Operation expression must be a lambda expression", nameof(operationExpression));

        // Create a unique name for this custom operation (will be replaced by HasQueryName)
        var operationName = $"CustomOperation_{Guid.NewGuid():N}";
        
        var propertyInfo = new QueryKitPropertyInfo
        {
            Name = operationName,
            CanFilter = true,
            CanSort = false, // Custom operations are typically not sortable
            QueryName = operationName,
            CustomOperation = ConvertToObjectExpression(operationExpression),
            CustomOperationEntityType = typeof(TModel)
        };

        _customOperationMappings[operationName] = propertyInfo;

        return new QueryKitCustomOperationMapping<TModel>(propertyInfo);
    }

    private static Expression<Func<object, ComparisonOperator, object, bool>> ConvertToObjectExpression<TModel>(
        Expression<Func<TModel, ComparisonOperator, object, bool>> typedExpression)
    {
        // Convert the typed expression to use object parameters for storage
        var entityParam = Expression.Parameter(typeof(object), "entity");
        var operatorParam = Expression.Parameter(typeof(ComparisonOperator), "op");
        var valueParam = Expression.Parameter(typeof(object), "value");

        // Cast the entity parameter to the correct type
        var castEntity = Expression.Convert(entityParam, typeof(TModel));

        // Replace parameters in the original expression body
        var visitor = new ParameterReplacerVisitor(typedExpression.Parameters[0], castEntity);
        var newBody = visitor.Visit(typedExpression.Body);

        // Replace the operator and value parameters
        visitor = new ParameterReplacerVisitor(typedExpression.Parameters[1], operatorParam);
        newBody = visitor.Visit(newBody);

        visitor = new ParameterReplacerVisitor(typedExpression.Parameters[2], valueParam);
        newBody = visitor.Visit(newBody);

        return Expression.Lambda<Func<object, ComparisonOperator, object, bool>>(
            newBody, entityParam, operatorParam, valueParam);
    }

    private class ParameterReplacerVisitor : ExpressionVisitor
    {
        private readonly ParameterExpression _oldParameter;
        private readonly Expression _newExpression;

        public ParameterReplacerVisitor(ParameterExpression oldParameter, Expression newExpression)
        {
            _oldParameter = oldParameter;
            _newExpression = newExpression;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            return node == _oldParameter ? _newExpression : base.VisitParameter(node);
        }
    }

    public string ReplaceAliasesWithPropertyPaths(string input)
    {
        var operators = ComparisonOperator.List.Select(x => x.Operator()).ToList();
        
        foreach (QueryKitPropertyInfo queryKitPropertyInfo in _propertyMappings.Values)
        {
            var propertyPath = GetPropertyPathByQueryName(queryKitPropertyInfo.QueryName);
            if (!string.IsNullOrEmpty(propertyPath))
            {
                foreach (var op in operators)
                {
                    // Use regular expression to isolate left side of the expression
                    var regex = new Regex($@"\b{queryKitPropertyInfo.QueryName}\b(?=\s*{op})", RegexOptions.IgnoreCase);

                    if (queryKitPropertyInfo is { CanSort: false, CanFilter: false} && regex.IsMatch(input))
                    {
                        throw new InvalidOperationException($"'{queryKitPropertyInfo.Name}' is not allowed for filtering or sorting.");
                    }
                    
                    input = regex.Replace(input, propertyPath);
                }
            }
        }
        
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
                else
                {
                    // Handle general method calls
                    var argumentsList = new List<string>();
                    foreach (var arg in call.Arguments)
                    {
                        argumentsList.Add(arg != null ? GetFullPropertyPath(arg) : "null");
                    }
                    var argumentsString = string.Join(", ", argumentsList);

                    if (call.Object != null)
                    {
                        // Instance method call
                        var callObjectPath = GetFullPropertyPath(call.Object);
                        return $"{callObjectPath}.{call.Method.Name}({argumentsString})";
                    }
                    else
                    {
                        // Static method call
                        return $"{call.Method.DeclaringType?.Name}.{call.Method.Name}({argumentsString})";
                    }
                }
            case ExpressionType.Lambda:
                var lambda = (LambdaExpression)expression;
                return GetFullPropertyPath(lambda.Body);
            case ExpressionType.Convert:
                var unary = (UnaryExpression)expression;
                return unary.Operand != null ? GetFullPropertyPath(unary.Operand) : "null";
            case ExpressionType.MemberAccess:
                var memberExpression = (MemberExpression)expression;
                if (memberExpression?.Expression == null)
                    return memberExpression?.Member?.Name ?? "null";
                return memberExpression.Expression.NodeType == ExpressionType.Parameter
                    ? memberExpression.Member.Name
                    : $"{GetFullPropertyPath(memberExpression.Expression)}.{memberExpression.Member?.Name}";
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
            case ExpressionType.NotEqual:
            case ExpressionType.AddChecked:
            case ExpressionType.MultiplyChecked:
            case ExpressionType.SubtractChecked:
                var binary = (BinaryExpression)expression;
                var left = binary.Left != null ? GetFullPropertyPath(binary.Left) : "null";
                var right = binary.Right != null ? GetFullPropertyPath(binary.Right) : "null";
                var op = GetOperator(binary.NodeType);
                return $"{left} {op} {right}";
            case ExpressionType.Constant:
                var constant = (ConstantExpression)expression;
                return constant.Value?.ToString() ?? "";
            
            case ExpressionType.Conditional:
                var conditional = (ConditionalExpression)expression;
                var test = conditional.Test != null ? GetFullPropertyPath(conditional.Test) : "null";
                var ifTrue = conditional.IfTrue != null ? GetFullPropertyPath(conditional.IfTrue) : "null";
                var ifFalse = conditional.IfFalse != null ? GetFullPropertyPath(conditional.IfFalse) : "null";
                return $"({test}) ? ({ifTrue}) : ({ifFalse})";
            
            case ExpressionType.New:
                var newExpression = (NewExpression)expression;
                var arguments = newExpression.Arguments.Select(arg => arg != null ? GetFullPropertyPath(arg) : "null");
                return newExpression.Constructor != null
                    ? $"{newExpression.Constructor.DeclaringType.Name}({string.Join(", ", arguments)})"
                    : $"new({string.Join(", ", arguments)})";
            
            case ExpressionType.Invoke:
                var invocation = (InvocationExpression)expression;
                var invocationArguments = invocation.Arguments.Select(arg => arg != null ? GetFullPropertyPath(arg) : "null");
                var invocationExpr = invocation.Expression != null ? GetFullPropertyPath(invocation.Expression) : "null";
                return $"{invocationExpr}({string.Join(", ", invocationArguments)})";
            
            case ExpressionType.ListInit:
                var listInit = (ListInitExpression)expression;
                var newExpr = GetFullPropertyPath(listInit.NewExpression);
                var initializers = listInit.Initializers.Select(init => string.Join(", ", init.Arguments.Select(GetFullPropertyPath)));
                return $"{newExpr} {{ {string.Join(", ", initializers)} }}";
            
            case ExpressionType.MemberInit:
                var memberInit = (MemberInitExpression)expression;
                var newExpressionPath = GetFullPropertyPath(memberInit.NewExpression);
                var bindings = memberInit.Bindings.Select(binding => $"{binding.Member.Name} = {GetFullPropertyPath(((MemberAssignment)binding).Expression)}");
                return $"{newExpressionPath} {{ {string.Join(", ", bindings)} }}";


            case ExpressionType.ArrayLength:
                var unaryArrayLength = (UnaryExpression)expression;
                return $"Length({GetFullPropertyPath(unaryArrayLength.Operand)})";

            case ExpressionType.ArrayIndex:
                var binaryArrayIndex = (BinaryExpression)expression;
                return $"{GetFullPropertyPath(binaryArrayIndex.Left)}[{GetFullPropertyPath(binaryArrayIndex.Right)}]";

            case ExpressionType.TypeAs:
            case ExpressionType.TypeIs:
                var typeBinary = (TypeBinaryExpression)expression;
                return $"{GetFullPropertyPath(typeBinary.Expression)} is {typeBinary.TypeOperand.Name}";

            case ExpressionType.Coalesce:
                var binaryCoalesce = (BinaryExpression)expression;
                return $"{GetFullPropertyPath(binaryCoalesce.Left)} ?? {GetFullPropertyPath(binaryCoalesce.Right)}";

            case ExpressionType.Block:
                var block = (BlockExpression)expression;
                var blockExpressions = block.Expressions.Select(GetFullPropertyPath);
                return $"{{ {string.Join("; ", blockExpressions)} }}";

            case ExpressionType.Throw:
                var unaryThrow = (UnaryExpression)expression;
                return $"throw {GetFullPropertyPath(unaryThrow.Operand)}";

            case ExpressionType.Try:
                var tryExpression = (TryExpression)expression;
                var body = GetFullPropertyPath(tryExpression.Body);
                var handlers = string.Join(" ", tryExpression.Handlers.Select(handler => $"catch ({handler.Test.Name}) {{ {GetFullPropertyPath(handler.Body)} }}"));
                var @finally = tryExpression.Finally != null ? $"finally {{ {GetFullPropertyPath(tryExpression.Finally)} }}" : "";
                return $"try {{ {body} }} {handlers} {@finally}";

            case ExpressionType.Index:
                var index = (IndexExpression)expression;
                var objectPath = GetFullPropertyPath(index.Object);
                var argumentsPath = string.Join(", ", index.Arguments.Select(GetFullPropertyPath));
                return $"{objectPath}[{argumentsPath}]";

            case ExpressionType.NewArrayInit:
                var newArrayInit = (NewArrayExpression)expression;
                var arrayInitElements = newArrayInit.Expressions.Select(GetFullPropertyPath);
                return $"new {newArrayInit.Type.GetElementType().Name}[] {{ {string.Join(", ", arrayInitElements)} }}";

            case ExpressionType.NewArrayBounds:
                var newArrayBounds = (NewArrayExpression)expression;
                var arrayBounds = newArrayBounds.Expressions.Select(GetFullPropertyPath);
                return $"new {newArrayBounds.Type.GetElementType().Name}[{string.Join(", ", arrayBounds)}]";

            case ExpressionType.ConvertChecked:
            case ExpressionType.ExclusiveOr:
            case ExpressionType.LeftShift:
            case ExpressionType.Negate:
            case ExpressionType.UnaryPlus:
            case ExpressionType.NegateChecked:
            case ExpressionType.Not:
            case ExpressionType.Parameter:
            case ExpressionType.Power:
            case ExpressionType.Quote:
            case ExpressionType.RightShift:
            case ExpressionType.Assign:
            case ExpressionType.DebugInfo:
            case ExpressionType.Decrement:
            case ExpressionType.Dynamic:
            case ExpressionType.Default:
            case ExpressionType.Extension:
            case ExpressionType.Goto:
            case ExpressionType.Increment:
            case ExpressionType.Label:
            case ExpressionType.RuntimeVariables:
            case ExpressionType.Loop:
            case ExpressionType.Switch:
            case ExpressionType.Unbox:
            case ExpressionType.AddAssign:
            case ExpressionType.AndAssign:
            case ExpressionType.DivideAssign:
            case ExpressionType.ExclusiveOrAssign:
            case ExpressionType.LeftShiftAssign:
            case ExpressionType.ModuloAssign:
            case ExpressionType.MultiplyAssign:
            case ExpressionType.OrAssign:
            case ExpressionType.PowerAssign:
            case ExpressionType.RightShiftAssign:
            case ExpressionType.SubtractAssign:
            case ExpressionType.AddAssignChecked:
            case ExpressionType.MultiplyAssignChecked:
            case ExpressionType.SubtractAssignChecked:
            case ExpressionType.PreIncrementAssign:
            case ExpressionType.PreDecrementAssign:
            case ExpressionType.PostIncrementAssign:
            case ExpressionType.PostDecrementAssign:
            case ExpressionType.TypeEqual:
            case ExpressionType.OnesComplement:
            case ExpressionType.IsTrue:
            case ExpressionType.IsFalse:
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
            ExpressionType.NotEqual => "!=",
            _ => throw new NotSupportedException($"Operator for expression type '{nodeType}' is not supported.")
        };
    }



    public QueryKitPropertyInfo? GetPropertyInfo(string? propertyName)
        =>  _propertyMappings.TryGetValue(propertyName, out var info) ? info : null;

    public QueryKitPropertyInfo? GetPropertyInfoByQueryName(string? queryName)
        => _propertyMappings.Values.FirstOrDefault(info => info.QueryName != null && info.QueryName.Equals(queryName, StringComparison.InvariantCultureIgnoreCase));

    public QueryKitPropertyInfo? GetDerivedPropertyInfoByQueryName(string? queryName)
        => _derivedPropertyMappings.Values.FirstOrDefault(info => info.QueryName != null && info.QueryName.Equals(queryName, StringComparison.InvariantCultureIgnoreCase));

    public QueryKitPropertyInfo? GetCustomOperationInfoByQueryName(string? queryName)
        => _customOperationMappings.Values.FirstOrDefault(info => info.QueryName != null && info.QueryName.Equals(queryName, StringComparison.InvariantCultureIgnoreCase));

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

    public QueryKitPropertyMapping<TModel> HasConversion<TTarget>()
    {
        _propertyInfo.UsesConversion = true;
        _propertyInfo.ConversionTargetType = typeof(TTarget);
        return this;
    }
}

public class QueryKitCustomOperationMapping<TModel>
{
    private readonly QueryKitPropertyInfo _propertyInfo;

    internal QueryKitCustomOperationMapping(QueryKitPropertyInfo propertyInfo)
    {
        _propertyInfo = propertyInfo;
    }

    public QueryKitCustomOperationMapping<TModel> HasQueryName(string queryName)
    {
        _propertyInfo.QueryName = queryName;
        return this;
    }

    public QueryKitCustomOperationMapping<TModel> PreventFilter()
    {
        _propertyInfo.CanFilter = false;
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
    internal Expression<Func<object, ComparisonOperator, object, bool>>? CustomOperation { get; set; }
    internal Type? CustomOperationEntityType { get; set; }
    internal bool UsesConversion { get; set; }
    internal Type? ConversionTargetType { get; set; }
}