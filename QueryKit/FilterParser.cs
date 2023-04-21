
namespace QueryKit;

using System.Linq.Expressions;
using System.Reflection;
using Sprache;

public static class FilterParser
{
    public static Expression<Func<T, bool>> ParseFilter<T>(string input, IQueryKitProcessorConfiguration config = null)
    {
        var parameter = Expression.Parameter(typeof(T), "x");
        var expr = ExprParser<T>(parameter, config).End().Parse(input);
        return Expression.Lambda<Func<T, bool>>(expr, parameter);
    }

    internal static readonly Parser<string> Identifier =
        from first in Parse.Letter.Once()
        from rest in Parse.LetterOrDigit.XOr(Parse.Char('_')).Many()
        select new string(first.Concat(rest).ToArray());

    public static Parser<ComparisonOperator> ComparisonOperatorParser =>
        from op in
            Parse.String(ComparisonOperator.EqualsOperator().Operator()).Text()
                .Or(Parse.String(ComparisonOperator.NotEqualsOperator().Operator()).Text())
                .Or(Parse.String(ComparisonOperator.GreaterThanOrEqualOperator().Operator()).Text())
                .Or(Parse.String(ComparisonOperator.LessThanOrEqualOperator().Operator()).Text())
                
                // > and < must come after >= and <=
                .Or(Parse.String(ComparisonOperator.GreaterThanOperator().Operator()).Text())
                .Or(Parse.String(ComparisonOperator.LessThanOperator().Operator()).Text())
                .Or(Parse.String(ComparisonOperator.ContainsOperator().Operator()).Text())
                .Or(Parse.String(ComparisonOperator.StartsWithOperator().Operator()).Text())
                .Or(Parse.String(ComparisonOperator.EndsWithOperator().Operator()).Text())
                .Or(Parse.String(ComparisonOperator.NotContainsOperator().Operator()).Text())
                .Or(Parse.String(ComparisonOperator.NotStartsWithOperator().Operator()).Text())
                .Or(Parse.String(ComparisonOperator.NotEndsWithOperator().Operator()).Text())
        from caseInsensitive in Parse.Char('*').Optional()
        select ComparisonOperator.GetByOperatorString(op, caseInsensitive.IsDefined);

    private static Parser<Expression> ComparisonExprParser<T>(ParameterExpression parameter, IQueryKitProcessorConfiguration config)
    {
        var comparisonOperatorParser = ComparisonOperatorParser.Token();
        var rightSideValueParser = RightSideValueParser.Token();

        return CreateLeftExprParser(parameter, config)
            .SelectMany(leftExpr => comparisonOperatorParser, (leftExpr, op) => new { leftExpr, op })
            .SelectMany(temp => rightSideValueParser, (temp, right) => new { temp.leftExpr, temp.op, right })
            .Select(temp =>
            {
                if (temp.leftExpr.NodeType == ExpressionType.Constant && ((ConstantExpression)temp.leftExpr).Value!.Equals(true))
                {
                    return Expression.Equal(Expression.Constant(true), Expression.Constant(true));
                }

                var rightExpr = CreateRightExpr(temp.leftExpr, temp.right);
                return temp.op.GetExpression<T>(temp.leftExpr, rightExpr);
            });
    }

    private static Parser<Expression> CreateLeftExprParser(ParameterExpression parameter, IQueryKitProcessorConfiguration config)
    {
        var leftIdentifierParser = Identifier.DelimitedBy(Parse.Char('.')).Token();

        return leftIdentifierParser.Select(left =>
        {
            // If only a single identifier is present
            var leftList = left.ToList();
            if (leftList.Count == 1)
            {
                var propName = leftList?.First();
                var fullPropPath = config?.GetPropertyPathByQueryName(propName) ?? propName;
                var propNames = fullPropPath?.Split('.');

                var propertyExpression = propNames?.Aggregate((Expression)parameter, (expr, pn) =>
                {
                    var propertyInfo = GetPropertyInfo(expr.Type, pn);
                    var actualPropertyName = propertyInfo?.Name ?? pn;
                    return Expression.PropertyOrField(expr, actualPropertyName);
                });

                // Check if property is filterable
                var propertyConfig = config?.PropertyMappings?.GetPropertyInfo(fullPropPath);
                if (propertyConfig != null && !propertyConfig.CanFilter)
                {
                    return Expression.Constant(true, typeof(bool));
                }

                return propertyExpression;
            }

            // If the property is nested with a dot ('.') separator
            var nestedPropertyExpression = leftList.Aggregate((Expression)parameter, (expr, propName) =>
            {
                var propertyInfo = GetPropertyInfo(expr.Type, propName);
                var mappedPropertyInfo = config?.PropertyMappings?.GetPropertyInfoByQueryName(propName);
                var actualPropertyName = mappedPropertyInfo?.Name ?? propertyInfo?.Name ?? propName;
                return Expression.PropertyOrField(expr, actualPropertyName);
            });

            // Check if nested property is filterable
            var nestedPropertyConfig = config?.PropertyMappings?.GetPropertyInfo(leftList.Last());
            if (nestedPropertyConfig != null && !nestedPropertyConfig.CanFilter)
            {
                return Expression.Constant(true, typeof(bool));
            }

            return nestedPropertyExpression;
        });
    }

    private static PropertyInfo? GetPropertyInfo(Type type, string propertyName)
    {
        return type.GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
    }

    public static Parser<LogicalOperator> LogicalOperatorParser =>
        from leadingSpaces in Parse.WhiteSpace.Many()
        from op in Parse.String("&&").Text().Or(Parse.String("||").Text())
        from trailingSpaces in Parse.WhiteSpace.Many()
        select LogicalOperator.GetByOperatorString(op);
    
    private static Parser<string> DoubleQuoteParser
        => Parse.Char('"').Then(_ => Parse.AnyChar.Except(Parse.Char('"')).Many().Text().Then(innerValue => Parse.Char('"').Return(innerValue)));

    private static Parser<string> TimeFormatParser => Parse.Regex(@"\d{2}:\d{2}:\d{2}").Text();
    private static Parser<string> DateTimeFormatParser => Parse.Regex(@"\d{4}-\d{2}-\d{2}(T\d{2}:\d{2}:\d{2}Z)?").Text();
    private static Parser<string> GuidFormatParser => Parse.Regex(@"[a-fA-F0-9]{8}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{12}").Text();
    
    private static Parser<string> RawStringLiteralParser =>
        from openingQuotes in Parse.Regex("\"{3,}").Text()
        let count = openingQuotes.Length
        from content in Parse.AnyChar.Except(Parse.Repeat(Parse.Char('"'), count)).Many().Text()
        from closingQuotes in Parse.Repeat(Parse.Char('"'), count).Text()
        select content;

    private static Parser<string> RightSideValueParser =>
        from atSign in Parse.Char('@').Optional()
        from leadingSpaces in Parse.WhiteSpace.Many()
        from value in Parse.String("null").Text()
            .Or(GuidFormatParser)
            .XOr(Identifier)
            .XOr(DateTimeFormatParser)
            .XOr(TimeFormatParser)
            .XOr(Parse.Decimal)
            .XOr(Parse.Number)
            .XOr(RawStringLiteralParser.Or(DoubleQuoteParser))
        from trailingSpaces in Parse.WhiteSpace.Many()
        select atSign.IsDefined ? "@" + value : value;

    private static Parser<Expression> AtomicExprParser<T>(ParameterExpression parameter, IQueryKitProcessorConfiguration config = null)
        => ComparisonExprParser<T>(parameter, config)
            .Or(Parse.Ref(() => ExprParser<T>(parameter, config)).Contained(Parse.Char('('), Parse.Char(')')));

    private static Parser<Expression> ExprParser<T>(ParameterExpression parameter, IQueryKitProcessorConfiguration config = null)
        => OrExprParser<T>(parameter, config);

    private static readonly Dictionary<Type, Func<string, object>> TypeConversionFunctions = new()
    {
        { typeof(string), value => value },
        { typeof(int), value => int.Parse(value) },
        { typeof(decimal), value => decimal.Parse(value) },
        { typeof(DateTime), value => DateTime.Parse(value) },
        { typeof(DateTimeOffset), value => DateTimeOffset.Parse(value) },
        { typeof(bool), value => bool.Parse(value) },
        { typeof(Guid), value => Guid.Parse(value) },
        { typeof(TimeSpan), value => TimeSpan.Parse(value) },
        { typeof(double), value => double.Parse(value) },
        { typeof(float), value => float.Parse(value) },
        { typeof(long), value => long.Parse(value) },
        { typeof(short), value => short.Parse(value) },
        { typeof(byte), value => byte.Parse(value) },
        { typeof(char), value => char.Parse(value) },
        { typeof(DateOnly), value => DateOnly.Parse(value) },
        { typeof(TimeOnly), value => TimeOnly.Parse(value) },
    };
    
    private static Expression CreateRightExpr(Expression leftExpr, string right)
    {
        var targetType = leftExpr.Type;

        if (TypeConversionFunctions.TryGetValue(targetType, out var conversionFunction))
        {
            if (right == "null")
            {
                return Expression.Constant(null, targetType);
            }

            if (targetType == typeof(string))
            {
                right = right.Replace("\"", "\\\"");
            }
            
            if (targetType == typeof(bool) && !bool.TryParse(right, out _))
            {
                return Expression.Constant(true, typeof(bool));
            }

            var convertedValue = conversionFunction(right);

            if (targetType == typeof(Guid))
            {
                var guidParseMethod = typeof(Guid).GetMethod("Parse", new[] { typeof(string) });
                return Expression.Call(guidParseMethod, Expression.Constant(convertedValue.ToString(), typeof(string)));
            }

            return Expression.Constant(convertedValue, targetType);
        }

        throw new InvalidOperationException($"Unsupported value '{right}' for type '{targetType.Name}'");
    }


    private static Parser<Expression> AndExprParser<T>(ParameterExpression parameter, IQueryKitProcessorConfiguration config = null)
        => Parse.ChainOperator(
            LogicalOperatorParser.Where(x => x.Name == "&&"),
            AtomicExprParser<T>(parameter, config),
            (op, left, right) => op.GetExpression<T>(left, right)
        );

    private static Parser<Expression> OrExprParser<T>(ParameterExpression parameter, IQueryKitProcessorConfiguration config = null)
        => Parse.ChainOperator(
            LogicalOperatorParser.Where(x => x.Name == "||"),
            AndExprParser<T>(parameter, config),
            (op, left, right) => op.GetExpression<T>(left, right)
        );
}
