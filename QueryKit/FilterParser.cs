
namespace QueryKit;

using System.Linq.Expressions;
using Sprache;

public static class FilterParser
{
    public static Expression<Func<T, bool>> ParseFilter<T>(string input, QueryKitProcessorConfiguration config = null)
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

    private static Parser<Expression> ComparisonExprParser<T>(ParameterExpression parameter, QueryKitProcessorConfiguration config)
    {
        return
            from left in Identifier.Token()
            from op in ComparisonOperatorParser.Token()
            from right in RightSideValueParser.Token()
            let propertyInfo = config?.PropertyMappings?.GetPropertyInfoByQueryName(left)
            let actualPropertyName = propertyInfo != null ? propertyInfo.Name : left
            let leftExpr = Expression.PropertyOrField(parameter, actualPropertyName)
            let rightExpr = CreateRightExpr(leftExpr, right)
            select op.GetExpression<T>(leftExpr, rightExpr);
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

    private static Parser<Expression> AtomicExprParser<T>(ParameterExpression parameter, QueryKitProcessorConfiguration config = null)
        => ComparisonExprParser<T>(parameter, config)
            .Or(Parse.Ref(() => ExprParser<T>(parameter, config)).Contained(Parse.Char('('), Parse.Char(')')));

    private static Parser<Expression> ExprParser<T>(ParameterExpression parameter, QueryKitProcessorConfiguration config = null)
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


    private static Parser<Expression> AndExprParser<T>(ParameterExpression parameter, QueryKitProcessorConfiguration config = null)
        => Parse.ChainOperator(
            LogicalOperatorParser.Where(x => x.Name == "&&"),
            AtomicExprParser<T>(parameter, config),
            (op, left, right) => op.GetExpression<T>(left, right)
        );

    private static Parser<Expression> OrExprParser<T>(ParameterExpression parameter, QueryKitProcessorConfiguration config = null)
        => Parse.ChainOperator(
            LogicalOperatorParser.Where(x => x.Name == "||"),
            AndExprParser<T>(parameter, config),
            (op, left, right) => op.GetExpression<T>(left, right)
        );
}