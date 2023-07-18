
namespace QueryKit;

using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using Configuration;
using Exceptions;
using Microsoft.EntityFrameworkCore;
using Operators;
using Sprache;

public static class FilterParser
{
    /// <summary>
    /// Generates an expression parser to filter data of the specified type. This simplified version does not support DbContext specific operations like SoundEx.
    /// </summary>
    /// <param name="input">A string that defines the filter parameters.</param>
    /// <param name="config">An optional IQueryKitConfiguration object to provide configuration for parsing, including logical aliases, comparison aliases and property mappings. Defaults to null.</param>
    /// <typeparam name="T">The type of data to be filtered by the returned expression parser.</typeparam>
    /// <returns>Returns a Func delegate that represents a lambda expression that applies the filter defined by the input parameter.</returns>
    public static Expression<Func<T, bool>> SimpleParseFilter<T>(string input, IQueryKitConfiguration? config = null)
        => ParseFilter<T>(input, config, null);

    /// <summary>
    /// Generates an expression parser to filter data of the specified type. It can optionally utilize a DbContext for operations not supported in the simple version.
    /// </summary>
    /// <param name="input">A string that defines the filter parameters.</param>
    /// <param name="config">An optional IQueryKitConfiguration object to provide configuration for parsing, including logical aliases, comparison aliases and property mappings. Defaults to null.</param>
    /// <param name="dbContext">An optional DbContext object that enables the use of database context specific operations. Defaults to null.</param>
    /// <typeparam name="T">The type of data to be filtered by the returned expression parser.</typeparam>
    /// <returns>Returns a Func delegate that represents a lambda expression that applies the filter defined by the input parameter.</returns>
    public static Expression<Func<T, bool>> ParseFilter<T>(string input, IQueryKitConfiguration? config = null, DbContext? dbContext = null)
    {
        input = config?.ReplaceLogicalAliases(input) ?? input;
        input = config?.ReplaceComparisonAliases(input) ?? input;
        input = config?.PropertyMappings?.ReplaceAliasesWithPropertyPaths(input) ?? input;
        
        var parameter = Expression.Parameter(typeof(T), "x");
        Expression expr; 
        try
        {
            expr = ExprParser<T>(parameter, config, dbContext).End().Parse(input);
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
            .Or(Parse.String(ComparisonOperator.SoundsLikeOperator().Operator()).Text())
            .Or(Parse.String(ComparisonOperator.DoesNotSoundLikeOperator().Operator()).Text())
        .SelectMany(op => Parse.Char(ComparisonOperator.CaseSensitiveAppendix).Optional(), (op, caseInsensitive) => new { op, caseInsensitive })
        .Select(x => ComparisonOperator.GetByOperatorString(x.op, x.caseInsensitive.IsDefined));

    private static PropertyInfo? GetPropertyInfo(Type type, string propertyName)
        => type.GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

    public static Parser<LogicalOperator> LogicalOperatorParser =>
        from leadingSpaces in Parse.WhiteSpace.Many()
        from op in Parse.String(LogicalOperator.AndOperator.Operator()).Text().Or(Parse.String(LogicalOperator.OrOperator.Operator()).Text())
        from trailingSpaces in Parse.WhiteSpace.Many()
        select LogicalOperator.GetByOperatorString(op);
    
    private static Parser<string> DoubleQuoteParser
        => Parse.Char('"').Then(_ => Parse.AnyChar.Except(Parse.Char('"')).Many().Text().Then(innerValue => Parse.Char('"').Return(innerValue)));

    /* ISO 8601
     * DateTimeOffset (with offset): yyyy-MM-ddTHH:mm:ss.ffffffzzz
     * DateTimeOffset (in UTC): yyyy-MM-ddTHH:mm:ss.ffffffZ
     * DateTime (no offset information): yyyy-MM-ddTHH:mm:ss.ffffff
     * DateTime (in UTC): yyyy-MM-ddTHH:mm:ss.ffffffZ
     */
    private static Parser<string> TimeFormatParser => Parse.Regex(@"\d{2}:\d{2}:\d{2}").Text();
    private static Parser<string> DateTimeFormatParser => 
        from dateFormat in Parse.Regex(@"\d{4}-\d{2}-\d{2}").Text()
        from timeFormat in Parse.Regex(@"T\d{2}:\d{2}:\d{2}").Text().Optional().Select(x => x.GetOrElse(""))
        from timeZone in Parse.Regex(@"Z|[+-]\d{2}(:\d{2})?").Text().Optional().Select(x => x.GetOrElse(""))
        from micros in Parse.Regex(@"\.\d{1,6}").Text().Optional().Select(x => x.GetOrElse(""))
        select dateFormat + timeFormat + micros + timeZone;


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
        { typeof(DateTime), value => DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal) },
        { typeof(DateTimeOffset), value => DateTimeOffset.Parse(value) },
        { typeof(DateOnly), value => DateOnly.Parse(value) },
        { typeof(TimeOnly), value => TimeOnly.Parse(value) },
        { typeof(TimeSpan), value => TimeSpan.Parse(value) },
        { typeof(uint), value => uint.Parse(value, CultureInfo.InvariantCulture) },
        { typeof(ulong), value => ulong.Parse(value, CultureInfo.InvariantCulture) },
        { typeof(ushort), value => ushort.Parse(value, CultureInfo.InvariantCulture) },
        { typeof(sbyte), value => sbyte.Parse(value, CultureInfo.InvariantCulture) },
        // { typeof(Enum), value => Enum.Parse(typeof(T), value) },
    };

    private static Expression CreateRightExpr(Expression leftExpr, string right)
    {
        var targetType = leftExpr.Type;
        var rawType = targetType;

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
            
            if (targetType == typeof(DateTime))
            {
                var dtStyle = right.EndsWith("Z") ? DateTimeStyles.AdjustToUniversal : DateTimeStyles.AssumeLocal;
                var dt = DateTime.Parse(right, CultureInfo.InvariantCulture, dtStyle);
                if (right.EndsWith("Z"))
                {
                    dt = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
                }

                var dtCtor = typeof(DateTime).GetConstructor(new[] { typeof(long), typeof(DateTimeKind) });
                var newExpr = Expression.New(dtCtor, Expression.Constant(dt.Ticks), Expression.Constant(dt.Kind));

                var isNullable = rawType == typeof(DateTime?);
                if (!isNullable) return newExpr;
                
                var nullableDtCtor = typeof(DateTime?).GetConstructor(new[] { typeof(DateTime) });
                newExpr = Expression.New(nullableDtCtor, newExpr);
                return newExpr;
            }

            if (targetType == typeof(DateTimeOffset))
            {
                var dtStyle = right.EndsWith("Z") ? DateTimeStyles.AdjustToUniversal : DateTimeStyles.AssumeLocal;
                var dto = DateTimeOffset.Parse(right, CultureInfo.InvariantCulture, dtStyle);
    
                var dtoCtor = typeof(DateTimeOffset).GetConstructor(new[] { typeof(long), typeof(TimeSpan) });
                var newExpr = Expression.New(dtoCtor, Expression.Constant(dto.Ticks), Expression.Constant(dto.Offset));

                var isNullable = rawType == typeof(DateTimeOffset?);
                if (!isNullable) return newExpr;
                
                var nullableDtoCtor = typeof(DateTimeOffset?).GetConstructor(new[] { typeof(DateTimeOffset) });
                newExpr = Expression.New(nullableDtoCtor, newExpr);
                return newExpr;
            }

            if (targetType == typeof(DateOnly))
            {
                var date = DateOnly.Parse(right, CultureInfo.InvariantCulture);
                var dateCtor = typeof(DateOnly).GetConstructor(new[] { typeof(int), typeof(int), typeof(int) });
                var newExpr = Expression.New(dateCtor, Expression.Constant(date.Year), Expression.Constant(date.Month), Expression.Constant(date.Day));

                var isNullable = rawType == typeof(DateOnly?);
                if (!isNullable) return newExpr;
                
                var nullableDateCtor = typeof(DateOnly?).GetConstructor(new[] { typeof(DateOnly) });
                newExpr = Expression.New(nullableDateCtor, newExpr);
                return newExpr;
            }
            
            if (targetType == typeof(TimeOnly))
            {
                var time = TimeOnly.Parse(right, CultureInfo.InvariantCulture);

                int millisecond = 0, microsecond = 0;
                if (right.Contains('.'))
                {
                    var fractionalSeconds = right.Split('.')[1];
                    if (fractionalSeconds.Length >= 3)
                    {
                        millisecond = int.Parse(fractionalSeconds.Substring(0, 3));
                    }
                    if (fractionalSeconds.Length >= 6)
                    {
                        microsecond = int.Parse(fractionalSeconds.Substring(3, 3));
                    }
                }

                var timeCtor = typeof(TimeOnly).GetConstructor(new[] { typeof(int), typeof(int), typeof(int), typeof(int), typeof(int) });
                var newExpr = Expression.New(timeCtor, Expression.Constant(time.Hour), Expression.Constant(time.Minute), Expression.Constant(time.Second), Expression.Constant(millisecond), Expression.Constant(microsecond));

                var isNullable = rawType == typeof(TimeOnly?);
                if (!isNullable) return newExpr;
                
                var nullableTimeCtor = typeof(TimeOnly?).GetConstructor(new[] { typeof(TimeOnly) });
                newExpr = Expression.New(nullableTimeCtor, newExpr);
                return newExpr;
            }

            if (targetType == typeof(Guid))
            {
                var guidParseMethod = typeof(Guid).GetMethod("Parse", new[] { typeof(string) });
                return Expression.Call(guidParseMethod, Expression.Constant(right, typeof(string)));
            }

            var convertedValue = conversionFunction(right);
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

    private static Parser<Expression> ComparisonExprParser<T>(ParameterExpression parameter, IQueryKitConfiguration? config, DbContext? dbContext)
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
                return temp.op.GetExpression<T>(temp.leftExpr, rightExpr, dbContext);
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
                    if(config?.AllowUnknownProperties == true)
                    {
                        return Expression.Constant(true, typeof(bool));
                    }
                    throw new UnknownFilterPropertyException(actualPropertyName);
                }
            });

            var propertyConfig = config?.PropertyMappings?.GetPropertyInfo(fullPropPath);
            if (propertyConfig != null && !propertyConfig.CanFilter)
            {
                return Expression.Constant(true, typeof(bool));
            }

            return propertyExpression;
        });
    }
    
    private static Parser<Expression> AtomicExprParser<T>(ParameterExpression parameter,
        IQueryKitConfiguration? config = null,
        DbContext? dbContext = null)
        => ComparisonExprParser<T>(parameter, config, dbContext)
            .Or(Parse.Ref(() => ExprParser<T>(parameter, config)).Contained(Parse.Char('('), Parse.Char(')')));

    private static Parser<Expression> ExprParser<T>(ParameterExpression parameter, IQueryKitConfiguration? config = null, DbContext? dbContext = null)
        => OrExprParser<T>(parameter, config, dbContext);
    
    private static Parser<Expression> AndExprParser<T>(ParameterExpression parameter, IQueryKitConfiguration? config = null, DbContext? dbContext = null)
        => Parse.ChainOperator(
            LogicalOperatorParser.Where(x => x.Name == LogicalOperator.AndOperator.Operator()),
            AtomicExprParser<T>(parameter, config, dbContext),
            (op, left, right) => op.GetExpression<T>(left, right)
        );

    private static Parser<Expression> OrExprParser<T>(ParameterExpression parameter, IQueryKitConfiguration? config = null, DbContext? dbContext = null)
        => Parse.ChainOperator(
            LogicalOperatorParser.Where(x => x.Name == LogicalOperator.OrOperator.Operator()),
            AndExprParser<T>(parameter, config, dbContext),
            (op, left, right) => op.GetExpression<T>(left, right)
        );
}
