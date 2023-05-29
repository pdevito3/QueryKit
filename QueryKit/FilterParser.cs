
namespace QueryKit;

using System.Collections;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using Exceptions;
using Operators;
using Sprache;

public static class FilterParser
{
    internal static Expression<Func<T, bool>> ParseFilter<T>(string input, IQueryKitConfiguration? config = null)
    {
        input = config?.ReplaceComparisonAliases(input) ?? input;
        input = config?.PropertyMappings?.ReplaceAliasesWithPropertyPaths(input) ?? input;
        
        var parameter = Expression.Parameter(typeof(T), "x");
        Expression expr; 
        try
        {
            expr = ExprParser<T>(parameter, config).End().Parse(input);
        }
        catch (ParseException e)
        {
            throw new ParsingException(e);
        }
        return Expression.Lambda<Func<T, bool>>(expr, parameter);
    }

    private static readonly Parser<string> Identifier =
        from first in Parse.Letter.Once()
        from rest in Parse.LetterOrDigit.XOr(Parse.Char('_')).Many()
        select new string(first.Concat(rest).ToArray());
    
    private static Parser<ComparisonOperator> ComparisonOperatorParser =>
        Parse.String(ComparisonOperator.EqualsOperator().Operator()).Text()
            .Or(Parse.String(ComparisonOperator.NotEqualsOperator().Operator()).Text())
            .Or(Parse.String(ComparisonOperator.GreaterThanOrEqualOperator().Operator()).Text())
            .Or(Parse.String(ComparisonOperator.LessThanOrEqualOperator().Operator()).Text())
            .Or(Parse.String(ComparisonOperator.GreaterThanOperator().Operator()).Text())
            .Or(Parse.String(ComparisonOperator.LessThanOperator().Operator()).Text())
            .Or(Parse.String(ComparisonOperator.ContainsOperator().Operator()).Text())
            .Or(Parse.String(ComparisonOperator.StartsWithOperator().Operator()).Text())
            .Or(Parse.String(ComparisonOperator.EndsWithOperator().Operator()).Text())
            .Or(Parse.String(ComparisonOperator.NotContainsOperator().Operator()).Text())
            .Or(Parse.String(ComparisonOperator.NotStartsWithOperator().Operator()).Text())
            .Or(Parse.String(ComparisonOperator.NotEndsWithOperator().Operator()).Text())
            .Or(Parse.String(ComparisonOperator.InOperator().Operator()).Text())
        .SelectMany(op => Parse.Char('*').Optional(), (op, caseInsensitive) => new { op, caseInsensitive })
        .Select(x => ComparisonOperator.GetByOperatorString(x.op, x.caseInsensitive.IsDefined));
    
    private static Parser<Expression> ComparisonExprParser<T>(ParameterExpression parameter, IQueryKitConfiguration? config)
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

    private static Parser<Expression?>? CreateLeftExprParser(ParameterExpression parameter, IQueryKitConfiguration? config)
    {
        var leftIdentifierParser = Identifier.DelimitedBy(Parse.Char('.')).Token();

        return leftIdentifierParser?.Select(left =>
        {
            var leftList = left.ToList();

            var fullPropPath = leftList?.First();
            var propertyExpression = leftList?.Aggregate((Expression)parameter, (expr, propName) =>
            {
                var propertyInfo = GetPropertyInfo(expr.Type, propName);
                var actualPropertyName = propertyInfo?.Name ?? propName;
                try
                {
                    return Expression.PropertyOrField(expr, actualPropertyName);
                }
                catch(ArgumentException)
                {
                    throw new UnknownFilterPropertyException(actualPropertyName);
                }
                // if i want to allow for a property to be missing, i can do this:
                // catch
                // {
                //     return Expression.Constant(true, typeof(bool));
                // }
            });

            var propertyConfig = config?.PropertyMappings?.GetPropertyInfo(fullPropPath);
            if (propertyConfig != null && !propertyConfig.CanFilter)
            {
                return Expression.Constant(true, typeof(bool));
            }

            return propertyExpression;
        });
    }

    private static PropertyInfo? GetPropertyInfo(Type type, string propertyName)
    {
        return type.GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
    }

    public static Parser<LogicalOperator> LogicalOperatorParser =>
        from leadingSpaces in Parse.WhiteSpace.Many()
        from op in Parse.String(LogicalOperator.AndOperator.Operator()).Text().Or(Parse.String(LogicalOperator.OrOperator.Operator()).Text())
        from trailingSpaces in Parse.WhiteSpace.Many()
        select LogicalOperator.GetByOperatorString(op);
    
    private static Parser<string> DoubleQuoteParser
        => Parse.Char('"').Then(_ => Parse.AnyChar.Except(Parse.Char('"')).Many().Text().Then(innerValue => Parse.Char('"').Return(innerValue)));


    private static Parser<string> TimeFormatParser => Parse.Regex(@"\d{2}:\d{2}:\d{2}").Text();
    private static Parser<string> DateTimeFormatParser => 
        from dateFormat in Parse.Regex(@"\d{4}-\d{2}-\d{2}").Text()
        from timeFormat in Parse.Regex(@"T\d{2}:\d{2}:\d{2}").Text().Optional().Select(x => x.GetOrElse(""))
        from timeZone in Parse.Regex(@"(Z|[+-]\d{2}(:\d{2})?)").Text().Optional().Select(x => x.GetOrElse(""))
        from millis in Parse.Regex(@"\.\d{3}").Text().Optional().Select(x => x.GetOrElse(""))
        select dateFormat + timeFormat + timeZone + millis;

    private static Parser<string> GuidFormatParser => Parse.Regex(@"[a-fA-F0-9]{8}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{12}").Text();
    
    private static Parser<string> RawStringLiteralParser =>
        from openingQuotes in Parse.Regex("\"{3,}").Text()
        let count = openingQuotes.Length
        from content in Parse.AnyChar.Except(Parse.Char('"').Repeat(count)).Many().Text()
        from closingQuotes in Parse.Char('"').Repeat(count).Text()
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
            .XOr(SquareBracketParser) 
        from trailingSpaces in Parse.WhiteSpace.Many()
        select atSign.IsDefined ? "@" + value : value;
    
    private static Parser<string> SquareBracketParser =>
        from openingBracket in Parse.Char('[')
        from content in DoubleQuoteParser
            .Or(GuidFormatParser)
            .Or(Parse.Decimal)
            .Or(Parse.Number)
            .Or(Identifier)
            .DelimitedBy(Parse.Char(',').Token())
        from closingBracket in Parse.Char(']')
        select "[" + string.Join(",", content) + "]";

    private static readonly Dictionary<Type, Func<string, object>> TypeConversionFunctions = new()
    {
        { typeof(string), value => value },
        { typeof(bool), value => bool.Parse(value) },
        { typeof(Guid), value => Guid.Parse(value) },
        { typeof(char), value => char.Parse(value) },
        { typeof(int), value => int.Parse(value, CultureInfo.InvariantCulture) },
        { typeof(float), x => float.Parse(x, CultureInfo.InvariantCulture) },
        { typeof(double), x => double.Parse(x, CultureInfo.InvariantCulture) },
        { typeof(decimal), x => decimal.Parse(x, CultureInfo.InvariantCulture) },
        { typeof(long), value => long.Parse(value, CultureInfo.InvariantCulture) },
        { typeof(short), value => short.Parse(value, CultureInfo.InvariantCulture) },
        { typeof(byte), value => byte.Parse(value, CultureInfo.InvariantCulture) },
        { typeof(DateTime), value => DateTime.Parse(value, CultureInfo.InvariantCulture) },
        { typeof(DateTimeOffset), value => DateTimeOffset.Parse(value, CultureInfo.InvariantCulture) },
        { typeof(DateOnly), value => DateOnly.Parse(value, CultureInfo.InvariantCulture) },
        { typeof(TimeOnly), value => TimeOnly.Parse(value, CultureInfo.InvariantCulture) },
        { typeof(TimeSpan), value => TimeSpan.Parse(value, CultureInfo.InvariantCulture) },
        { typeof(uint), value => uint.Parse(value, CultureInfo.InvariantCulture) },
        { typeof(ulong), value => ulong.Parse(value, CultureInfo.InvariantCulture) },
        { typeof(ushort), value => ushort.Parse(value, CultureInfo.InvariantCulture) },
        { typeof(sbyte), value => sbyte.Parse(value, CultureInfo.InvariantCulture) },
        // { typeof(Enum), value => Enum.Parse(typeof(T), value) },
    };

    private static Expression CreateRightExpr(Expression leftExpr, string right)
    {
        var targetType = leftExpr.Type;

        targetType = TransformTargetTypeIfNullable(targetType);

        if (TypeConversionFunctions.TryGetValue(targetType, out var conversionFunction))
        {
            if (right == "null")
            {
                return Expression.Constant(null, leftExpr.Type);
            }

            if (right.StartsWith("[") && right.EndsWith("]"))
            {
                var values = right.Trim('[', ']').Split(',').Select(x => x.Trim()).ToList();
                var elementType = targetType.IsArray ? targetType.GetElementType() : targetType;
            
                var expressions = values.Select(x =>
                {
                    if (elementType == typeof(string) && x.StartsWith("\"") && x.EndsWith("\""))
                    {
                        x = x.Trim('"');
                    }
            
                    var convertedValue = TypeConversionFunctions[elementType](x);
                    return Expression.Constant(convertedValue, elementType);
                }).ToArray();
            
                var newArrayExpression = Expression.NewArrayInit(elementType, expressions);
                return newArrayExpression;
            }

            if (targetType == typeof(string))
            {
                right = right.Trim('"');
            }
            
            var convertedValue = conversionFunction(right);

            if (targetType == typeof(Guid))
            {
                var guidParseMethod = typeof(Guid).GetMethod("Parse", new[] { typeof(string) });
                return Expression.Call(guidParseMethod, Expression.Constant(convertedValue.ToString(), typeof(string)));
            }

            return Expression.Constant(convertedValue, leftExpr.Type);
        }

        throw new InvalidOperationException($"Unsupported value '{right}' for type '{targetType.Name}'");
    }

    private static Type TransformTargetTypeIfNullable(Type targetType)
    {
        if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            targetType = Nullable.GetUnderlyingType(targetType);
        }

        return targetType;
    }

    private static Parser<Expression> AtomicExprParser<T>(ParameterExpression parameter,
        IQueryKitConfiguration? config = null)
        => ComparisonExprParser<T>(parameter, config)
            .Or(Parse.Ref(() => ExprParser<T>(parameter, config)).Contained(Parse.Char('('), Parse.Char(')')));

    private static Parser<Expression> ExprParser<T>(ParameterExpression parameter, IQueryKitConfiguration? config = null)
        => OrExprParser<T>(parameter, config);
    
    private static Parser<Expression> AndExprParser<T>(ParameterExpression parameter, IQueryKitConfiguration? config = null)
        => Parse.ChainOperator(
            LogicalOperatorParser.Where(x => x.Name == LogicalOperator.AndOperator.Operator()),
            AtomicExprParser<T>(parameter, config),
            (op, left, right) => op.GetExpression<T>(left, right)
        );

    private static Parser<Expression> OrExprParser<T>(ParameterExpression parameter, IQueryKitConfiguration? config = null)
        => Parse.ChainOperator(
            LogicalOperatorParser.Where(x => x.Name == LogicalOperator.OrOperator.Operator()),
            AndExprParser<T>(parameter, config),
            (op, left, right) => op.GetExpression<T>(left, right)
        );
}
