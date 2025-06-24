namespace QueryKit;

using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using Configuration;
using Exceptions;
using Operators;
using Expressions;
using Sprache;

public static class FilterParser
{
    /// <summary>
    /// Generates an expression parser to filter data of the specified type.
    /// </summary>
    /// <param name="input">A string that defines the filter parameters.</param>
    /// <param name="config">An optional IQueryKitConfiguration object to provide configuration for parsing, including logical aliases, comparison aliases and property mappings. Defaults to null.</param>
    /// <typeparam name="T">The type of data to be filtered by the returned expression parser.</typeparam>
    /// <returns>Returns a Func delegate that represents a lambda expression that applies the filter defined by the input parameter.</returns>
    public static Expression<Func<T, bool>> ParseFilter<T>(string input, IQueryKitConfiguration? config = null)
    {
        input = config?.ReplaceLogicalAliases(input) ?? input;
        input = config?.ReplaceComparisonAliases(input) ?? input;
        input = config?.PropertyMappings?.ReplaceAliasesWithPropertyPaths(input) ?? input;
        
        var parameter = Expression.Parameter(typeof(T), "x");
        Expression expr;
        try
        {
            expr = ExprParser<T>(parameter, config).End().Parse(input);
            expr = ReplaceDerivedProperties(expr, config, parameter);
        }
        catch (InvalidOperationException e)
        {
            throw new ParsingException(e);
        }
        catch (ParseException e)
        {
            throw new ParsingException(e);
        }

        return Expression.Lambda<Func<T, bool>>(expr, parameter);
    }
    
    private static Expression ReplaceDerivedProperties(Expression expr, IQueryKitConfiguration? config, ParameterExpression parameter)
    {
        if (config?.PropertyMappings == null)
        {
            return expr;
        }

        return new ParameterReplacer(parameter).Visit(expr);
    }


    private static readonly Parser<string> Identifier =
        from first in Parse.Letter.Once()
        from rest in Parse.LetterOrDigit.XOr(Parse.Char('_')).Many()
        select new string(first.Concat(rest).ToArray());
    
    private static Parser<ComparisonOperator> ComparisonOperatorParser =>
        Parse.Char(ComparisonOperator.AllPrefix).Optional().Select(opt => opt.IsDefined)
            .Then(hasHash => 
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
                    .Or(Parse.String(ComparisonOperator.NotInOperator().Operator()).Text())
                    .Or(Parse.String(ComparisonOperator.SoundsLikeOperator().Operator()).Text())
                    .Or(Parse.String(ComparisonOperator.DoesNotSoundLikeOperator().Operator()).Text())
                    .Or(Parse.String(ComparisonOperator.HasCountEqualToOperator().Operator()).Text())
                    .Or(Parse.String(ComparisonOperator.HasCountNotEqualToOperator().Operator()).Text())
                    .Or(Parse.String(ComparisonOperator.HasCountGreaterThanOrEqualOperator().Operator()).Text())
                    .Or(Parse.String(ComparisonOperator.HasCountLessThanOrEqualOperator().Operator()).Text())
                    .Or(Parse.String(ComparisonOperator.HasCountGreaterThanOperator().Operator()).Text())
                    .Or(Parse.String(ComparisonOperator.HasCountLessThanOperator().Operator()).Text())
                    .Or(Parse.String(ComparisonOperator.HasOperator().Operator()).Text())
                    .Or(Parse.String(ComparisonOperator.DoesNotHaveOperator().Operator()).Text())
                    .SelectMany(op => Parse.Char(ComparisonOperator.CaseSensitiveAppendix).Optional(), (op, caseInsensitive) => new { op, caseInsensitive, hasHash })
                    .Select(x => ComparisonOperator.GetByOperatorString(x.op, x.caseInsensitive.IsDefined, x.hasHash)));

    private static PropertyInfo? GetPropertyInfo(Type type, string propertyName)
        => type.GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

    // Arithmetic expression parsers
    private static Parser<ArithmeticOperator> ArithmeticOperatorParser =>
        Parse.Char('+').Return(ArithmeticOperator.Add)
            .Or(Parse.Char('-').Return(ArithmeticOperator.Subtract))
            .Or(Parse.Char('*').Return(ArithmeticOperator.Multiply))
            .Or(Parse.Char('/').Return(ArithmeticOperator.Divide))
            .Or(Parse.Char('%').Return(ArithmeticOperator.Modulo));

    private static Parser<ArithmeticExpression> ArithmeticTermParser =>
        PropertyArithmeticParser
            .Or(LiteralArithmeticParser)
            .Or(Parse.Ref(() => ArithmeticExpressionParser).Contained(Parse.Char('('), Parse.Char(')')).Select(expr => new GroupedArithmeticExpression(expr)));

    private static Parser<ArithmeticExpression> PropertyArithmeticParser =>
        Identifier.DelimitedBy(Parse.Char('.'))
            .Select(props => new PropertyArithmeticExpression(string.Join(".", props)));

    private static Parser<ArithmeticExpression> LiteralArithmeticParser =>
        NumberParser.Select(numStr =>
        {
            if (int.TryParse(numStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intVal))
                return new LiteralArithmeticExpression(intVal, typeof(int));
            if (decimal.TryParse(numStr, NumberStyles.Number, CultureInfo.InvariantCulture, out var decVal))
                return new LiteralArithmeticExpression(decVal, typeof(decimal));
            if (double.TryParse(numStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var doubleVal))
                return new LiteralArithmeticExpression(doubleVal, typeof(double));
            
            throw new InvalidOperationException($"Cannot parse number: {numStr}");
        });

    private static Parser<ArithmeticExpression> ArithmeticFactorParser =>
        Parse.ChainOperator(
            ArithmeticOperatorParser.Where(op => op.Precedence == 2).Token(), // *, /, %
            ArithmeticTermParser.Token(),
            (op, left, right) => new BinaryArithmeticExpression(left, op, right));

    private static Parser<ArithmeticExpression> ArithmeticExpressionParser =>
        Parse.ChainOperator(
            ArithmeticOperatorParser.Where(op => op.Precedence == 1).Token(), // +, -
            ArithmeticFactorParser.Token(),
            (op, left, right) => new BinaryArithmeticExpression(left, op, right));

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

    private static Parser<string> NumberParser =>
        from sign in Parse.Char('-').Optional().Select(x => x.IsDefined ? "-" : "")
        from number in Parse.Decimal
        select sign + number;

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
            .XOr(DateTimeFormatParser)
            .XOr(TimeFormatParser)
            .XOr(NumberParser)
            .XOr(RawStringLiteralParser.Or(DoubleQuoteParser))
            .XOr(SquareBracketParser)
            .XOr(Identifier) // Keep this last to try property paths only if nothing else matches
        from trailingSpaces in Parse.WhiteSpace.Many()
        select atSign.IsDefined ? "@" + value : value;
    
    private static Parser<string> SquareBracketParser =>
        from openingBracket in Parse.Char('[')
        from content in Parse.String("null").Text()
            .Or(GuidFormatParser)
            .Or(DateTimeFormatParser)
            .Or(TimeFormatParser)
            .Or(NumberParser)
            .Or(RawStringLiteralParser.Or(DoubleQuoteParser))
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
    };

    private static Expression CreateRightExpr(Expression leftExpr, string right, ComparisonOperator op)
    {
        var targetType = leftExpr.Type;
        return CreateRightExprFromType(targetType, right, op);
    }

    private static Expression CreateRightExprFromType(Type leftExprType, string right, ComparisonOperator op)
    {
        var isEnumerable = IsEnumerable(leftExprType);
        var targetType = leftExprType;
        if (isEnumerable)
        {
            if (op.IsCountOperator() && int.TryParse(right, out var intVal))
            {
                return Expression.Constant(intVal, typeof(int));
            }
            targetType = targetType.GetGenericArguments()[0];
            return CreateRightExprFromType(targetType, right, op);
        }
        
        var rawType = targetType;
        
        targetType = TransformTargetTypeIfNullable(targetType);

        if (TypeConversionFunctions.TryGetValue(targetType, out var conversionFunction))
        {
            if (right == "null")
            {
                if (rawType == typeof(Guid?))
                {
                    return Expression.Constant(null, typeof(string));
                }
                
                return Expression.Constant(null, leftExprType);
            }
            
            if (right.StartsWith("[") && right.EndsWith("]"))
            {
                targetType = targetType == typeof(Guid) || targetType == typeof(Guid?) ? typeof(string) : targetType;
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
                return Expression.Constant(right, typeof(string)); 
            }

            var convertedValue = conversionFunction(right);
            return Expression.Constant(convertedValue, leftExprType);
        }

        if (rawType.IsEnum || (Nullable.GetUnderlyingType(rawType)?.IsEnum ?? false))
        {
            var enumType = Nullable.GetUnderlyingType(rawType) ?? rawType;
    
            if (right == "null" && Nullable.GetUnderlyingType(rawType) != null)
            {
                return Expression.Constant(null, rawType);
            }
            
            if (right.StartsWith("[") && right.EndsWith("]"))
            {
                var values = right.Trim('[', ']').Split(',').Select(x => x.Trim()).ToList();
                var elementType = targetType.IsArray ? targetType.GetElementType() : targetType;
            
                var expressions = values.Select<string, Expression>(x =>
                {
                    if (elementType == typeof(string) && x.StartsWith("\"") && x.EndsWith("\""))
                    {
                        x = x.Trim('"');
                    }
            
                    var enumValue = Enum.Parse(enumType, x);
                    var constant = Expression.Constant(enumValue, enumType);
            
                    return constant;
                }).ToArray();
            
                var newArrayExpression = Expression.NewArrayInit(enumType, expressions);
                return newArrayExpression;
            }
            
            var parsed = Enum.TryParse(enumType, right, out var enumValue);
            if (!parsed) 
            {
                throw new InvalidOperationException($"Unsupported value '{right}' for type '{targetType.Name}'");
            }
            var constant = Expression.Constant(enumValue, enumType);

            if (rawType == enumType) return constant;
            
            var nullableCtor = rawType.GetConstructor(new[] {enumType});
            return Expression.New(nullableCtor, constant);
        }
        
        // for some complex derived expressions
        if (targetType == typeof(object))
        {
            if (right == "null")
            {
                return Expression.Constant(null, typeof(object));
            }

            if (bool.TryParse(right, out var boolVal))
            {
                return Expression.Constant(boolVal, typeof(bool));
            }
        }

        throw new InvalidOperationException($"Unsupported value '{right}' for type '{targetType.Name}'");
    }

    private static Type TransformTargetTypeIfNullable(Type targetType)
    {
        if (targetType.IsNullable())
        {
            targetType = Nullable.GetUnderlyingType(targetType);
        }

        return targetType;
    }

    private static bool IsEnumerable(Type targetType)
    {
        if (targetType == typeof(string))
        {
            return false;
        }
        return targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(IEnumerable<>) ||
               targetType.GetInterfaces()
                   .Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEnumerable<>));
    }

    // New arithmetic-aware comparison parser - only matches expressions in parentheses with arithmetic operators
    private static Parser<Expression> ArithmeticComparisonExprParser<T>(ParameterExpression parameter, IQueryKitConfiguration? config)
    {
        var comparisonOperatorParser = ComparisonOperatorParser.Token();
        var rightSideValueParser = RightSideValueParser.Token();
        
        // Only parse arithmetic expressions that are in parentheses and contain arithmetic operators
        var parenthesizedArithmetic = ArithmeticExpressionParser.Contained(Parse.Char('('), Parse.Char(')')).Token();
        
        // Ensure the arithmetic expression contains actual arithmetic operators
        var validArithmeticExpr = parenthesizedArithmetic.Where(expr => ContainsArithmeticOperator(expr));
        
        return validArithmeticExpr
            .SelectMany(leftArithmetic => comparisonOperatorParser, (leftArithmetic, op) => new { leftArithmetic, op })
            .SelectMany(temp => parenthesizedArithmetic.Or(rightSideValueParser.Select(value => CreateArithmeticFromValue(value))), (temp, rightSide) => new { temp.leftArithmetic, temp.op, rightSide })
            .Select(temp =>
            {
                var leftExpr = temp.leftArithmetic.ToLinqExpression(parameter, typeof(T));
                var rightExpr = temp.rightSide.ToLinqExpression(parameter, typeof(T));
                
                var (leftCompatible, rightCompatible) = EnsureCompatibleTypes(leftExpr, rightExpr);
                return temp.op.GetExpression<T>(leftCompatible, rightCompatible, config?.DbContextType);
            });
    }
    
    private static bool ContainsArithmeticOperator(ArithmeticExpression expr)
    {
        return expr switch
        {
            BinaryArithmeticExpression => true,
            PropertyArithmeticExpression => false,
            LiteralArithmeticExpression => false,
            _ => false
        };
    }
    
    private static ArithmeticExpression CreateArithmeticFromValue(string value)
    {
        // Handle null case
        if (value == "null")
            throw new InvalidOperationException("Cannot use 'null' in arithmetic expressions");
            
        // Try to parse as number
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intVal))
            return new LiteralArithmeticExpression(intVal, typeof(int));
        if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var decVal))
            return new LiteralArithmeticExpression(decVal, typeof(decimal));
        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var doubleVal))
            return new LiteralArithmeticExpression(doubleVal, typeof(double));
        
        // Assume it's a property path (but only for valid property names)
        if (IsValidPropertyName(value))
            return new PropertyArithmeticExpression(value);
            
        throw new InvalidOperationException($"Cannot parse '{value}' as property or literal in arithmetic expression");
    }
    
    private static bool IsValidPropertyName(string value)
    {
        return !string.IsNullOrWhiteSpace(value) && 
               char.IsLetter(value[0]) && 
               value.All(c => char.IsLetterOrDigit(c) || c == '_' || c == '.');
    }

    private static Parser<Expression> ComparisonExprParser<T>(ParameterExpression parameter, IQueryKitConfiguration? config)
    {
        var comparisonOperatorParser = ComparisonOperatorParser.Token();
        var rightSideValueParser = RightSideValueParser.Token();

        // Try arithmetic comparison first, but only if parentheses are present
        var arithmeticComparison = ArithmeticComparisonExprParser<T>(parameter, config);
        
        var regularComparison = CreateLeftExprParser(parameter, config)
            .SelectMany(leftExpr => comparisonOperatorParser, (leftExpr, op) => new { leftExpr, op })
            .SelectMany(temp => rightSideValueParser, (temp, right) => new { temp.leftExpr, temp.op, right })
            .Select(temp =>
            {
                if (temp.leftExpr.NodeType == ExpressionType.Constant && ((ConstantExpression)temp.leftExpr).Value!.Equals(true))
                {
                    return Expression.Equal(Expression.Constant(true), Expression.Constant(true));
                }

                // Check if this is a custom operation placeholder
                if (temp.leftExpr.NodeType == ExpressionType.Constant && 
                    ((ConstantExpression)temp.leftExpr).Value is string constantValue && 
                    constantValue.StartsWith("CustomOperation:"))
                {
                    var operationName = constantValue.Substring("CustomOperation:".Length);
                    var customOperationInfo = config?.PropertyMappings?.GetCustomOperationInfoByQueryName(operationName);
                    if (customOperationInfo?.CustomOperation != null)
                    {
                        return CreateCustomOperationExpression<T>(parameter, customOperationInfo, temp.op, temp.right);
                    }
                }
                
                if (temp.leftExpr.Type == typeof(Guid) || temp.leftExpr.Type == typeof(Guid?))
                {
                    var guidStringExpr = HandleGuidConversion(temp.leftExpr, temp.leftExpr.Type);
                    return temp.op.GetExpression<T>(guidStringExpr, CreateRightExpr(temp.leftExpr, temp.right, temp.op),
                        config?.DbContextType);
                }

                // Check if the right side is a property path for property-to-property comparison
                if (IsPropertyPath(temp.right, parameter.Type))
                {
                    var rightPropertyExpr = CreateRightPropertyExpr<T>(parameter, temp.right, config);
                    if (rightPropertyExpr != null)
                    {
                        // Handle GUID conversion for property-to-property comparisons
                        var leftExpr = temp.leftExpr;
                        if (leftExpr.Type == typeof(Guid) || leftExpr.Type == typeof(Guid?))
                        {
                            leftExpr = HandleGuidConversion(leftExpr, leftExpr.Type);
                        }
                        if (rightPropertyExpr.Type == typeof(Guid) || rightPropertyExpr.Type == typeof(Guid?))
                        {
                            rightPropertyExpr = HandleGuidConversion(rightPropertyExpr, rightPropertyExpr.Type);
                        }
                        
                        // Ensure compatible types for property-to-property comparison
                        var (leftCompatible, rightCompatible) = EnsureCompatibleTypes(leftExpr, rightPropertyExpr);
                        return temp.op.GetExpression<T>(leftCompatible, rightCompatible, config?.DbContextType);
                    }
                }

                var rightExpr = CreateRightExpr(temp.leftExpr, temp.right, temp.op);
                
                // Handle nested collection filtering
                if (temp.leftExpr is MethodCallExpression methodCall && IsNestedCollectionExpression(methodCall))
                {
                    return CreateNestedCollectionFilterExpression<T>(methodCall, rightExpr, temp.op);
                }
                
                return temp.op.GetExpression<T>(temp.leftExpr, rightExpr, config?.DbContextType);
            });

        return arithmeticComparison.Or(regularComparison);
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
                if (expr is MemberExpression member)
                {
                    if (IsEnumerable(member.Type))
                    {
                        var genericArgType = member.Type.GetGenericArguments()[0];
                        var propertyType = genericArgType.GetProperty(propName).PropertyType;

                        if (IsEnumerable(propertyType))
                        {
                            propertyType = propertyType.GetGenericArguments()[0];

                            var linqMethod = "SelectMany";
                            var selectMethod = typeof(Enumerable).GetMethods()
                                .First(m => m.Name ==  linqMethod && m.GetParameters().Length == 2)
                                .MakeGenericMethod(genericArgType, propertyType);

                            var innerParameter = Expression.Parameter(genericArgType, "y");
                            var propertyInfoForMethod = GetPropertyInfo(genericArgType, propName);
                            Expression lambdaBody = Expression.PropertyOrField(innerParameter, propertyInfoForMethod.Name);

                            // Ensure the lambda body returns IEnumerable<T> for SelectMany
                            var expectedType = typeof(IEnumerable<>).MakeGenericType(propertyType);
                            if (lambdaBody.Type != expectedType && !expectedType.IsAssignableFrom(lambdaBody.Type))
                            {
                                // Convert to IEnumerable<T> if needed (e.g., List<T> to IEnumerable<T>)
                                lambdaBody = Expression.Convert(lambdaBody, expectedType);
                            }

                            // Create lambda with the correct return type
                            var lambdaType = typeof(Func<,>).MakeGenericType(genericArgType, expectedType);
                            lambdaBody = Expression.Lambda(lambdaType, lambdaBody, innerParameter);

                            return Expression.Call(selectMethod, member, lambdaBody);
                        }
                        else
                        {
                            var selectMethod = typeof(Enumerable).GetMethods()
                                .First(m => m.Name == "Select" && m.GetParameters().Length == 2)
                                .MakeGenericMethod(genericArgType, genericArgType.GetProperty(propName).PropertyType);

                            var innerParameter = Expression.Parameter(genericArgType, "y");
                            var propertyInfoForMethod = GetPropertyInfo(genericArgType, propName);
                            var lambdaBody = Expression.PropertyOrField(innerParameter, propertyInfoForMethod.Name);
                            var selectLambda = Expression.Lambda(lambdaBody, innerParameter);
                            var selectResult = Expression.Call(null, selectMethod, member, selectLambda);

                            return HandleGuidConversion(selectResult, propertyType, "Select");
                        }
                    }
                }

                if (expr is MethodCallExpression call)
                {
                    var innerGenericType = GetInnerGenericType(call.Method.ReturnType);
                    var propertyInfoForMethod = GetPropertyInfo(innerGenericType, propName);

                    var propertyType = propertyInfoForMethod.PropertyType;
                    var linqMethod = IsEnumerable(propertyType) ? "SelectMany" : "Select";
                    var resultType = IsEnumerable(propertyType) ? propertyType.GetGenericArguments()[0] : propertyType;
                    
                    var selectMethod = typeof(Enumerable).GetMethods()
                        .First(m => m.Name == linqMethod && m.GetParameters().Length == 2)
                        .MakeGenericMethod(innerGenericType, resultType);

                    var innerParameter = Expression.Parameter(innerGenericType, "y");
                    var lambdaBody = Expression.PropertyOrField(innerParameter, propertyInfoForMethod.Name);
                    var selectLambda = Expression.Lambda(lambdaBody, innerParameter);

                    return Expression.Call(selectMethod, expr, selectLambda);
                }

                var propertyInfo = GetPropertyInfo(expr.Type, propName);
                var actualPropertyName = propertyInfo?.Name ?? propName;
                try
                {
                    return Expression.PropertyOrField(expr, actualPropertyName);
                }
                catch(ArgumentException)
                {
                    // Check for custom operations first
                    var customOperationInfo = config?.PropertyMappings?.GetCustomOperationInfoByQueryName(fullPropPath);
                    if (customOperationInfo?.CustomOperation != null)
                    {
                        // Custom operations will be handled in the comparison parsing, so return a placeholder
                        return Expression.Constant($"CustomOperation:{fullPropPath}", typeof(string));
                    }

                    var derivedPropertyInfo = config?.PropertyMappings?.GetDerivedPropertyInfoByQueryName(fullPropPath);
                    if (derivedPropertyInfo?.DerivedExpression != null)
                    {
                        return derivedPropertyInfo.DerivedExpression;
                    }
                    
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
    
    private static Type? GetInnerGenericType(Type type)
    {
        if (!IsEnumerable(type))
        {
            return type;
        }

        var innerGenericType = type.GetGenericArguments()[0];
        return GetInnerGenericType(innerGenericType);
    }
    
    private static Parser<Expression> AtomicExprParser<T>(ParameterExpression parameter, IQueryKitConfiguration? config = null)
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
    
    private static Expression GetGuidToStringExpression(Expression leftExpr)
    {
        var toStringMethod = typeof(Guid).GetMethod("ToString", Type.EmptyTypes);

        return leftExpr.Type == typeof(Guid?) ?
            Expression.Condition(
                Expression.Property(leftExpr, "HasValue"),
                Expression.Call(Expression.Property(leftExpr, "Value"), toStringMethod!),
                Expression.Constant(null, typeof(string))
            ) :
            Expression.Call(leftExpr, toStringMethod!);
    }

    private static Expression HandleGuidConversion(Expression expression, Type propertyType, string? selectMethodName = null)
    {
        if (propertyType != typeof(Guid) && propertyType != typeof(Guid?)) return expression;

        if (string.IsNullOrWhiteSpace(selectMethodName)) return GetGuidToStringExpression(expression);

        var selectMethod = typeof(Enumerable).GetMethods()
            .First(m => m.Name == selectMethodName && m.GetParameters().Length == 2)
            .MakeGenericMethod(propertyType, typeof(string));

        var param = Expression.Parameter(propertyType, "g");
        var toStringLambda = Expression.Lambda(GetGuidToStringExpression(param), param);

        return Expression.Call(selectMethod, expression, toStringLambda);
    }

    private static bool IsNestedCollectionExpression(MethodCallExpression methodCall)
    {
        // Check if this is a nested SelectMany expression indicating nested collection navigation
        if (methodCall.Method.Name == "SelectMany" && methodCall.Arguments.Count == 2)
        {
            // Check if the source is also a SelectMany (indicating nesting)
            if (methodCall.Arguments[0] is MethodCallExpression sourceCall && 
                sourceCall.Method.Name == "SelectMany")
            {
                return true;
            }
        }
        return false;
    }

    private static Expression CreateNestedCollectionFilterExpression<T>(MethodCallExpression methodCall, Expression rightExpr, ComparisonOperator op)
    {
        // For nested collection expressions like Ingredients.Preparations.Text
        // We need to unwind the SelectMany chain and create nested Any expressions
        // like: x.Ingredients.Any(i => i.Preparations.Any(p => p.Text == "value"))
        
        var expressions = UnwindSelectManyChain(methodCall);
        if (expressions.Count < 2)
        {
            // Fallback to regular collection expression
            return op.GetExpression<T>(methodCall, rightExpr, null);
        }

        // Build nested Any expressions from the inside out
        var currentExpression = expressions.Last();
        var currentParameter = Expression.Parameter(currentExpression.CollectionElementType, $"item{expressions.Count - 1}");
        var finalPropertyAccess = Expression.PropertyOrField(currentParameter, currentExpression.PropertyName);
        
        // Create the innermost comparison
        var comparison = op.GetExpression<T>(finalPropertyAccess, rightExpr, null);
        var innerLambda = Expression.Lambda(comparison, currentParameter);
        
        // Build the Any chain from inside out
        for (int i = expressions.Count - 2; i >= 0; i--)
        {
            var collectionInfo = expressions[i];
            var param = Expression.Parameter(collectionInfo.CollectionElementType, $"item{i}");
            var collectionAccess = Expression.PropertyOrField(param, collectionInfo.PropertyName);
            
            // Create Any method call
            var anyMethod = typeof(Enumerable).GetMethods()
                .First(m => m.Name == "Any" && m.GetParameters().Length == 2)
                .MakeGenericMethod(collectionInfo.CollectionElementType);
            
            var anyCall = Expression.Call(anyMethod, collectionAccess, innerLambda);
            innerLambda = Expression.Lambda(anyCall, param);
        }

        return innerLambda.Body;
    }

    private class CollectionInfo
    {
        public Type CollectionElementType { get; set; }
        public string PropertyName { get; set; }
    }

    private static List<CollectionInfo> UnwindSelectManyChain(MethodCallExpression methodCall)
    {
        var result = new List<CollectionInfo>();
        var current = methodCall;

        while (current != null && current.Method.Name == "SelectMany")
        {
            // Extract the property access from the lambda
            if (current.Arguments[1] is LambdaExpression lambda &&
                lambda.Body is MemberExpression member)
            {
                var elementType = current.Method.GetGenericArguments()[0];
                result.Insert(0, new CollectionInfo 
                { 
                    CollectionElementType = elementType, 
                    PropertyName = member.Member.Name 
                });
            }

            // Move to the next level
            if (current.Arguments[0] is MethodCallExpression nextCall)
            {
                current = nextCall;
            }
            else
            {
                break;
            }
        }

        return result;
    }

    private static bool IsPropertyPath(string value, Type entityType)
    {
        // Skip obvious literal values
        if (value == "null" || 
            value.StartsWith("\"") || 
            value.StartsWith("[") ||
            value.Contains("-") && DateTime.TryParse(value, out _) ||
            decimal.TryParse(value, out _) ||
            bool.TryParse(value, out _) ||
            Guid.TryParse(value, out _))
        {
            return false;
        }

        // Check if it's a valid property path
        var propertyPath = value.Split('.');
        var currentType = entityType;

        foreach (var propName in propertyPath)
        {
            var property = GetPropertyInfo(currentType, propName);
            if (property == null)
            {
                return false;
            }
            currentType = property.PropertyType;
        }

        return true;
    }

    private static Expression? CreateRightPropertyExpr<T>(ParameterExpression parameter, string propertyPath, IQueryKitConfiguration? config)
    {
        try
        {
            var propertyNames = propertyPath.Split('.');
            return propertyNames.Aggregate((Expression)parameter, (expr, propName) =>
            {
                var propertyInfo = GetPropertyInfo(expr.Type, propName);
                if (propertyInfo == null)
                {
                    throw new ArgumentException($"Property '{propName}' not found on type '{expr.Type.Name}'");
                }
                return Expression.PropertyOrField(expr, propertyInfo.Name);
            });
        }
        catch
        {
            return null;
        }
    }

    private static (Expression left, Expression right) EnsureCompatibleTypes(Expression left, Expression right)
    {
        if (left.Type == right.Type)
        {
            return (left, right);
        }

        // Handle nullable types
        var leftNonNullable = Nullable.GetUnderlyingType(left.Type) ?? left.Type;
        var rightNonNullable = Nullable.GetUnderlyingType(right.Type) ?? right.Type;
        var leftIsNullable = left.Type != leftNonNullable;
        var rightIsNullable = right.Type != rightNonNullable;

        if (leftNonNullable == rightNonNullable)
        {
            return (left, right);
        }

        // Handle numeric type conversions
        if (IsNumericType(leftNonNullable) && IsNumericType(rightNonNullable))
        {
            var widerType = GetWiderNumericType(leftNonNullable, rightNonNullable);
            
            // Determine if the final type should be nullable
            var shouldBeNullable = leftIsNullable || rightIsNullable;
            var targetType = shouldBeNullable ? typeof(Nullable<>).MakeGenericType(widerType) : widerType;
            
            if (left.Type != targetType)
            {
                left = Expression.Convert(left, targetType);
            }
            if (right.Type != targetType)
            {
                right = Expression.Convert(right, targetType);
            }
        }

        return (left, right);
    }

    private static bool IsNumericType(Type type)
    {
        return type == typeof(byte) || type == typeof(sbyte) ||
               type == typeof(short) || type == typeof(ushort) ||
               type == typeof(int) || type == typeof(uint) ||
               type == typeof(long) || type == typeof(ulong) ||
               type == typeof(float) || type == typeof(double) ||
               type == typeof(decimal);
    }

    private static Type GetWiderNumericType(Type type1, Type type2)
    {
        var typeRanks = new Dictionary<Type, int>
        {
            { typeof(byte), 1 }, { typeof(sbyte), 1 },
            { typeof(short), 2 }, { typeof(ushort), 2 },
            { typeof(int), 3 }, { typeof(uint), 3 },
            { typeof(long), 4 }, { typeof(ulong), 4 },
            { typeof(float), 5 },
            { typeof(double), 6 },
            { typeof(decimal), 7 }
        };

        var rank1 = typeRanks.GetValueOrDefault(type1, 0);
        var rank2 = typeRanks.GetValueOrDefault(type2, 0);

        return rank1 >= rank2 ? type1 : type2;
    }

    private static Expression CreateCustomOperationExpression<T>(ParameterExpression parameter, QueryKitPropertyInfo customOperationInfo, ComparisonOperator op, string rightValue)
    {
        if (customOperationInfo.CustomOperation == null)
            throw new ArgumentException("Custom operation expression is null");

        // For custom operations, we need to convert the string value to the appropriate basic type
        // instead of trying to match it to the entity type
        object convertedValue = ConvertStringToBasicType(rightValue);
        
        // Create the parameter expressions for the custom operation
        var entityParameter = Expression.Convert(parameter, typeof(object));
        var operatorParameter = Expression.Constant(op, typeof(ComparisonOperator));
        var valueParameter = Expression.Constant(convertedValue, typeof(object));

        // Invoke the custom operation
        var customOperationLambda = customOperationInfo.CustomOperation;
        var invocationExpression = Expression.Invoke(customOperationLambda, entityParameter, operatorParameter, valueParameter);

        return invocationExpression;
    }

    private static object ConvertStringToBasicType(string value)
    {
        // Handle null
        if (string.IsNullOrEmpty(value) || value.Equals("null", StringComparison.InvariantCultureIgnoreCase))
            return null;

        // Try boolean
        if (bool.TryParse(value, out var boolValue))
            return boolValue;

        // Try int
        if (int.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var intValue))
            return intValue;

        // Try decimal
        if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var decimalValue))
            return decimalValue;

        // Try double
        if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var doubleValue))
            return doubleValue;

        // Try DateTime
        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var dateTimeValue))
            return dateTimeValue;

        // Try Guid
        if (Guid.TryParse(value, out var guidValue))
            return guidValue;

        // Default to string
        return value;
    }
}


