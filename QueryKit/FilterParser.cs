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
        EnsureWithinInputLength(input, config);

        input = config?.ReplaceLogicalAliases(input) ?? input;
        input = config?.ReplaceComparisonAliases(input) ?? input;
        input = config?.PropertyMappings?.ReplaceAliasesWithPropertyPaths(input) ?? input;
        
        var parameter = Expression.Parameter(typeof(T), "x");
        Expression expr;
        var parameterizeBefore = FilterValue.Parameterize;
        FilterValue.Parameterize = config is IQueryKitFilterBehavior { ParameterizeFilterValues: true };
        var maxNestingDepthBefore = _maxNestingDepth;
        var nestingDepthBefore = _nestingDepth;
        _maxNestingDepth = (config as IQueryKitParseLimits)?.MaxNestingDepth ?? QueryKitSettings.DefaultMaxNestingDepth;
        _nestingDepth = 0;
        try
        {
            expr = ExprParser<T>(parameter, config).End().Parse(input);

            // When the parser removed every clause, no clause limits the result
            if (expr is RemovedClauseExpression)
            {
                expr = Expression.Constant(true);
            }

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
        finally
        {
            FilterValue.Parameterize = parameterizeBefore;
            _maxNestingDepth = maxNestingDepthBefore;
            _nestingDepth = nestingDepthBefore;
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

    // Runs before the grammar sees the input, so an oversized filter is rejected with a
    // QueryKitException instead of exhausting CPU and memory during parsing.
    private static void EnsureWithinInputLength(string input, IQueryKitConfiguration? config)
    {
        var maxLength = (config as IQueryKitParseLimits)?.MaxInputLength ?? QueryKitSettings.DefaultMaxInputLength;
        if (input.Length > maxLength)
        {
            throw new QueryKitInputLengthExceededException(input.Length, maxLength);
        }
    }

    // The nesting depth limit of the parse on this thread, and the number of parenthesized groups
    // that the parser is in now. Parsing is synchronous, so the values belong to the thread that parses.
    [ThreadStatic] private static int _maxNestingDepth;
    [ThreadStatic] private static int _nestingDepth;

    // Parses '(' inner ')' and counts the group against MaxNestingDepth. The grammar does the count,
    // so a '(' or ')' inside a quoted value cannot change it. The parser recurses once for each group,
    // so the limit also limits the depth of the call stack.
    private static Parser<TResult> Grouped<TResult>(Parser<TResult> inner)
    {
        Parser<TResult> counted = input =>
        {
            try
            {
                _nestingDepth++;
                if (_nestingDepth > _maxNestingDepth)
                {
                    throw new QueryKitNestingDepthExceededException(_nestingDepth, _maxNestingDepth);
                }

                return inner(input);
            }
            finally
            {
                _nestingDepth--;
            }
        };

        return counted.Contained(Parse.Char('('), Parse.Char(')'));
    }

    private static readonly Parser<string> Identifier =
        from first in Parse.Letter.Once()
        from rest in Parse.LetterOrDigit.XOr(Parse.Char('_')).Many()
        select new string(first.Concat(rest).ToArray());

    private static readonly Parser<string> IdentifierPathParser =
        Identifier.DelimitedBy(Parse.Char('.')).Select(parts => string.Join(".", parts));

    private static Parser<IEnumerable<string>> PropertyListParser(Parser<string> propertyPathParser)
    {
        var propertiesParser = propertyPathParser.Token().DelimitedBy(Parse.Char(',').Token());
        return Grouped(propertiesParser);
    }

    // Each parser is built once. A parser in a second or later `from` clause is built in a lambda
    // that runs on each parse, so keep those parsers in fields too. A field can only use fields that
    // are declared above it, so the recursive arithmetic parser goes through Parse.Ref.
    private static readonly Parser<string> ComparisonOperatorTextParser =
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
            .Or(Parse.String(ComparisonOperator.DoesNotHaveOperator().Operator()).Text());

    private static readonly Parser<(string Operator, bool CaseInsensitive)> CanonicalComparisonOperatorParser =
        ComparisonOperatorTextParser
            .SelectMany(op => Parse.Char(ComparisonOperator.CaseSensitiveAppendix).Optional(), (op, caseInsensitive) => (op, caseInsensitive.IsDefined));

    private static Parser<ComparisonOperator> ComparisonOperatorParser(IQueryKitConfiguration? config)
    {
        var operatorParser = CanonicalComparisonOperatorParser.Or(ComparisonOperatorAliasParser(config));
        return Parse.Char(ComparisonOperator.AllPrefix).Optional().Select(opt => opt.IsDefined)
            .Then(hasHash => operatorParser.Select(x => ComparisonOperator.GetByOperatorString(x.Operator, x.CaseInsensitive, hasHash)));
    }

    // The rewrite before the parse replaces each alias that stands between whitespace, like v1.14.2.
    // The grammar reads an alias that the rewrite did not replace, for example `(Age)eq 3`.
    // Longer aliases are tried first so an alias that starts with another alias (e.g. `@@$$` and `@@$`) still matches.
    private static Parser<(string Operator, bool CaseInsensitive)> ComparisonOperatorAliasParser(IQueryKitConfiguration? config)
    {
        Parser<(string Operator, bool CaseInsensitive)> parser = i => Result.Failure<(string, bool)>(i, "no operator alias", Array.Empty<string>());
        if (config == null)
            return parser;

        var caseInsensitiveSuffix = ComparisonOperator.CaseSensitiveAppendix.ToString();
        foreach (var match in ComparisonOperator.GetAliasMatches(config).OrderByDescending(x => x.Alias.Length))
        {
            var caseInsensitive = match.Operator.EndsWith(caseInsensitiveSuffix);
            var op = caseInsensitive ? match.Operator[..^caseInsensitiveSuffix.Length] : match.Operator;
            parser = parser.Or(OperatorAlias(match.Alias).Return((op, caseInsensitive)));
        }

        return parser;
    }

    // An alias is a whole word: it must be followed by whitespace or the end of the input.
    private static Parser<string> OperatorAlias(string alias) => input =>
    {
        var result = Parse.IgnoreCase(alias).Text()(input);
        if (!result.WasSuccessful || result.Remainder.AtEnd || char.IsWhiteSpace(result.Remainder.Current))
            return result;

        return Result.Failure<string>(input, $"Operator alias '{alias}' must be followed by whitespace", new[] { alias });
    };

    private static PropertyInfo? GetPropertyInfo(Type type, string propertyName)
        => type.GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

    private static readonly Parser<string> LogicalOperatorTextParser =
        Parse.String(LogicalOperator.AndOperator.Operator()).Text().Or(Parse.String(LogicalOperator.OrOperator.Operator()).Text());

    public static Parser<LogicalOperator> LogicalOperatorParser { get; } =
        from leadingSpaces in Parse.WhiteSpace.Many()
        from op in LogicalOperatorTextParser
        from trailingSpaces in Parse.WhiteSpace.Many()
        select LogicalOperator.GetByOperatorString(op);
    
    private static readonly Parser<string> DoubleQuoteParser
        = Parse.Char('"').Then(_ => Parse.AnyChar.Except(Parse.Char('"')).Many().Text().Then(innerValue => Parse.Char('"').Return(innerValue)));

    /* ISO 8601
     * DateTimeOffset (with offset): yyyy-MM-ddTHH:mm:ss.ffffffzzz
     * DateTimeOffset (in UTC): yyyy-MM-ddTHH:mm:ss.ffffffZ
     * DateTime (no offset information): yyyy-MM-ddTHH:mm:ss.ffffff
     * DateTime (in UTC): yyyy-MM-ddTHH:mm:ss.ffffffZ
     */
    private static readonly Parser<string> TimeFormatParser = Parse.Regex(@"\d{2}:\d{2}:\d{2}(\.\d{1,7})?").Text();
    private static readonly Parser<string> DateTimeTimeParser = Parse.Regex(@"T\d{2}:\d{2}:\d{2}").Text().Optional().Select(x => x.GetOrElse(""));
    private static readonly Parser<string> DateTimeMicrosParser = Parse.Regex(@"\.\d{1,7}").Text().Optional().Select(x => x.GetOrElse(""));
    private static readonly Parser<string> DateTimeZoneParser = Parse.Regex(@"Z|[+-]\d{2}(:\d{2})?").Text().Optional().Select(x => x.GetOrElse(""));
    // v1.14.2 read the zone before the fraction, so 2022-07-01T00:00:02Z.5 is a valid value. A zone after the fraction is also valid.
    private static readonly Parser<string> DateTimeFormatParser =
        from dateFormat in Parse.Regex(@"\d{4}-\d{2}-\d{2}").Text()
        from timeFormat in DateTimeTimeParser
        from zoneBeforeMicros in DateTimeZoneParser
        from micros in DateTimeMicrosParser
        from zoneAfterMicros in zoneBeforeMicros == "" ? DateTimeZoneParser : Parse.Return("")
        select dateFormat + timeFormat + micros + zoneBeforeMicros + zoneAfterMicros;

    // A number with a '.' decimal point, or with the decimal separator of the current culture.
    // The longer match wins, so '4.5' parses in every culture and '4,5' still parses in a culture that uses ','.
    private static readonly Parser<string> UnsignedNumberParser = input =>
    {
        var invariant = Parse.DecimalInvariant(input);
        var culture = Parse.Decimal(input);
        return culture.WasSuccessful && (!invariant.WasSuccessful || culture.Remainder.Position > invariant.Remainder.Position)
            ? culture
            : invariant;
    };

    private static readonly Parser<string> NumberParser =
        from sign in Parse.Char('-').Optional().Select(x => x.IsDefined ? "-" : "")
        from number in UnsignedNumberParser
        select sign + number;

    // List items are separated by ',', so a list number always uses the '.' decimal point.
    private static readonly Parser<string> ListNumberParser =
        from sign in Parse.Char('-').Optional().Select(x => x.IsDefined ? "-" : "")
        from number in Parse.DecimalInvariant
        select sign + number;

    private static readonly Parser<string> GuidFormatParser = Parse.Regex(@"[a-fA-F0-9]{8}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{12}").Text();
    
    private static readonly Parser<string> RawStringLiteralParser =
        from openingQuotes in Parse.Regex("\"{3,}").Text()
        let count = openingQuotes.Length
        from content in Parse.AnyChar.Except(Parse.Char('"').Repeat(count)).Many().Text()
        from closingQuotes in Parse.Char('"').Repeat(count).Text()
        select content;

    // Carries whether the right-hand value was written as a quoted string literal (e.g. "id").
    // This is needed to disambiguate a literal from a bare property reference (property-to-property
    // comparison) once the surrounding quotes have been stripped, since both are otherwise identical strings.
    private readonly record struct RightSideValue(string Value, bool IsQuotedLiteral);

    private static readonly Parser<IEnumerable<string>> SquareBracketValuesParser =
        Parse.String("null").Text()
            .Or(GuidFormatParser)
            .Or(DateTimeFormatParser)
            .Or(TimeFormatParser)
            .Or(ListNumberParser)
            .Or(RawStringLiteralParser.Or(DoubleQuoteParser))
            .Or(Identifier)
            .DelimitedBy(Parse.Char(',').Token());

    private static readonly Parser<string> SquareBracketParser =
        from openingBracket in Parse.Char('[')
        from content in SquareBracketValuesParser
        from closingBracket in Parse.Char(']')
        select "[" + string.Join(",", content) + "]";

    private static readonly Parser<RightSideValue> RightSideValueChoiceParser =
        Parse.String("null").Text().Select(v => new RightSideValue(v, false))
            .Or(GuidFormatParser.Select(v => new RightSideValue(v, false)))
            .XOr(DateTimeFormatParser.Select(v => new RightSideValue(v, false)))
            .XOr(TimeFormatParser.Select(v => new RightSideValue(v, false)))
            .XOr(NumberParser.Select(v => new RightSideValue(v, false)))
            .XOr((RawStringLiteralParser.Or(DoubleQuoteParser)).Select(v => new RightSideValue(v, true)))
            .XOr(SquareBracketParser.Select(v => new RightSideValue(v, false)))
            .XOr(Identifier.Select(v => new RightSideValue(v, false))); // Keep this last to try property paths only if nothing else matches

    private static readonly Parser<RightSideValue> RightSideValueParser =
        from atSign in Parse.Char('@').Optional()
        from leadingSpaces in Parse.WhiteSpace.Many()
        from value in RightSideValueChoiceParser
        from trailingSpaces in Parse.WhiteSpace.Many()
        select atSign.IsDefined ? value with { Value = "@" + value.Value } : value;

    // Arithmetic expression parsers
    private static readonly Parser<ArithmeticOperator> ArithmeticOperatorParser =
        Parse.Char('+').Return(ArithmeticOperator.Add)
            .Or(Parse.Char('-').Return(ArithmeticOperator.Subtract))
            .Or(Parse.Char('*').Return(ArithmeticOperator.Multiply))
            .Or(Parse.Char('/').Return(ArithmeticOperator.Divide))
            .Or(Parse.Char('%').Return(ArithmeticOperator.Modulo));

    private static readonly Parser<ArithmeticExpression> PropertyArithmeticParser =
        Identifier.DelimitedBy(Parse.Char('.'))
            .Select(props => new PropertyArithmeticExpression(string.Join(".", props)));

    private static readonly Parser<ArithmeticExpression> LiteralArithmeticParser =
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

    private static readonly Parser<ArithmeticExpression> ArithmeticTermParser =
        PropertyArithmeticParser
            .Or(LiteralArithmeticParser)
            .Or(Grouped(Parse.Ref(() => ArithmeticExpressionParser)).Select(expr => new GroupedArithmeticExpression(expr)));

    private static readonly Parser<ArithmeticExpression> ArithmeticFactorParser =
        Parse.ChainOperator(
            ArithmeticOperatorParser.Where(op => op.Precedence == 2).Token(), // *, /, %
            ArithmeticTermParser.Token(),
            (op, left, right) => new BinaryArithmeticExpression(left, op, right));

    private static readonly Parser<ArithmeticExpression> ArithmeticExpressionParser =
        Parse.ChainOperator(
            ArithmeticOperatorParser.Where(op => op.Precedence == 1).Token(), // +, -
            ArithmeticFactorParser.Token(),
            (op, left, right) => new BinaryArithmeticExpression(left, op, right));

    private static Parser<LogicalOperator> LogicalOperatorParserWithAliases(IQueryKitConfiguration? config)
    {
        var aliases = config == null ? new List<LogicalOperator.LogicalAliasMatch>() : LogicalOperator.GetAliasMatches(config);
        return aliases.Aggregate(
            LogicalOperatorParser,
            (parser, match) => parser.Or(
                from leadingSpaces in Parse.WhiteSpace.Many()
                from op in OperatorAlias(match.Alias)
                from trailingSpaces in Parse.WhiteSpace.Many()
                select LogicalOperator.GetByOperatorString(match.Operator)));
    }

    // Npgsql only accepts a DateTimeOffset parameter with offset 0, so a parameter gets the same instant in UTC.
    // A literal keeps its offset, like v1.14.2.
    private static DateTimeOffset ToParameterOffset(DateTimeOffset value)
        => FilterValue.Parameterize ? value.ToUniversalTime() : value;

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
        { typeof(DateTimeOffset), value => ToParameterOffset(DateTimeOffset.Parse(value)) },
        { typeof(DateOnly), value => DateOnly.Parse(value) },
        { typeof(TimeOnly), value => TimeOnly.Parse(value) },
        { typeof(TimeSpan), value => TimeSpan.Parse(value) },
        { typeof(uint), value => uint.Parse(value, CultureInfo.InvariantCulture) },
        { typeof(ulong), value => ulong.Parse(value, CultureInfo.InvariantCulture) },
        { typeof(ushort), value => ushort.Parse(value, CultureInfo.InvariantCulture) },
        { typeof(sbyte), value => sbyte.Parse(value, CultureInfo.InvariantCulture) },
    };

    private static Expression CreateRightExpr(Expression leftExpr, string right, bool rightIsQuotedLiteral, ComparisonOperator op,
        IQueryKitConfiguration? config = null, string? propertyPath = null)
    {
        var targetType = leftExpr.Type;

        // Handle expressions with Object type - check for ConditionalExpression inside Convert/Unary
        if (targetType == typeof(object))
        {
            var innerExpr = leftExpr;

            // Unwrap Convert/Unary expressions
            while (innerExpr is UnaryExpression unaryExpr && unaryExpr.NodeType == ExpressionType.Convert)
            {
                innerExpr = unaryExpr.Operand;
            }

            if (innerExpr is ConditionalExpression conditionalExpr)
            {
                // For conditional expressions, use the type of the branches
                // Prefer nullable types if one branch is nullable
                var trueType = conditionalExpr.IfTrue.Type;
                var falseType = conditionalExpr.IfFalse.Type;

                if (trueType == falseType)
                {
                    targetType = trueType;
                }
                else if (Nullable.GetUnderlyingType(trueType) != null || Nullable.GetUnderlyingType(falseType) != null)
                {
                    // One is nullable, use the nullable version
                    var underlyingTrue = Nullable.GetUnderlyingType(trueType) ?? trueType;
                    var underlyingFalse = Nullable.GetUnderlyingType(falseType) ?? falseType;

                    if (underlyingTrue == underlyingFalse)
                    {
                        targetType = typeof(Nullable<>).MakeGenericType(underlyingTrue);
                    }
                }
                else if (trueType.IsAssignableFrom(falseType))
                {
                    targetType = trueType;
                }
                else if (falseType.IsAssignableFrom(trueType))
                {
                    targetType = falseType;
                }
            }
        }

        // Check if this property uses HasConversion
        if (config?.PropertyMappings != null && !string.IsNullOrEmpty(propertyPath))
        {
            var propertyConfig = config.PropertyMappings.GetPropertyInfoByQueryName(propertyPath);
            if (propertyConfig?.UsesConversion == true && propertyConfig.ConversionTargetType != null)
            {
                // For HasConversion properties, try to create a constant of the original type
                // by constructing it from the string value using a constructor that takes the target type
                if (propertyConfig.ConversionTargetType == typeof(string))
                {
                    var stringCtor = leftExpr.Type.GetConstructor(new[] { typeof(string) });
                    if (stringCtor != null)
                    {
                        return Expression.New(stringCtor, FilterValue.Create(right, typeof(string)));
                    }
                }
                
                // For other conversion types, fall back to using the conversion target type
                targetType = propertyConfig.ConversionTargetType;
            }
        }
        
        return CreateRightExprFromType(targetType, right, rightIsQuotedLiteral, op);
    }

    private static Expression CreateRightExprFromType(Type leftExprType, string right, bool rightIsQuotedLiteral, ComparisonOperator op)
    {
        var isEnumerable = IsEnumerable(leftExprType);
        var targetType = leftExprType;
        if (isEnumerable)
        {
            if (op.IsCountOperator() && (int.TryParse(right, out var intVal) || int.TryParse(right, NumberStyles.Integer, CultureInfo.InvariantCulture, out intVal)))
            {
                return FilterValue.Create(intVal, typeof(int));
            }
            targetType = targetType.GetGenericArguments()[0];
            return CreateRightExprFromType(targetType, right, rightIsQuotedLiteral, op);
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
                // Only convert GUID arrays to string arrays for string operators (Contains, etc.)
                // For other operators like 'in', keep as GUID array for proper type matching
                if ((targetType == typeof(Guid) || targetType == typeof(Guid?)) && op.IsStringComparisonOperator())
                {
                    targetType = typeof(string);
                }
                var values = right.Trim('[', ']').Split(',').Select(x => x.Trim()).ToList();
                var elementType = targetType.IsArray ? targetType.GetElementType()! : targetType;

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

                return FilterValue.Create(dt, rawType);
            }

            if (targetType == typeof(DateTimeOffset))
            {
                var dtStyle = right.EndsWith("Z") ? DateTimeStyles.AdjustToUniversal : DateTimeStyles.AssumeLocal;
                var dto = DateTimeOffset.Parse(right, CultureInfo.InvariantCulture, dtStyle);
                return FilterValue.Create(ToParameterOffset(dto), rawType);
            }

            if (targetType == typeof(DateOnly))
            {
                var date = DateOnly.Parse(right, CultureInfo.InvariantCulture);
                return FilterValue.Create(date, rawType);
            }

            if (targetType == typeof(TimeOnly))
            {
                var time = TimeOnly.Parse(right, CultureInfo.InvariantCulture);

                int millisecond = 0, microsecond = 0;
                if (rightIsQuotedLiteral)
                {
                    // Like v1.14.2, the milliseconds of a quoted value need at least 3 fraction digits and the microseconds need at least 6.
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
                }
                else
                {
                    // v1.14.2 did not accept an unquoted fraction, so an unquoted value keeps its full fraction.
                    var fractionalTicks = time.Ticks % TimeSpan.TicksPerSecond;
                    millisecond = (int)(fractionalTicks / TimeSpan.TicksPerMillisecond);
                    microsecond = (int)(fractionalTicks % TimeSpan.TicksPerMillisecond / 10);
                }

                // One microsecond is 10 ticks. The TimeOnly constructor with microseconds needs .NET 7.
                var value = new TimeOnly(time.Hour, time.Minute, time.Second, millisecond)
                    .Add(TimeSpan.FromTicks(microsecond * 10));
                return FilterValue.Create(value, rawType);
            }

            if (targetType == typeof(Guid))
            {
                // For string operators (Contains, StartsWith, EndsWith), we need to compare as strings
                // For equality/comparison operators, we can compare GUIDs directly (more efficient and EF-friendly)
                if (op.IsStringComparisonOperator())
                {
                    return FilterValue.Create(right, typeof(string));
                }

                // Parse the GUID for direct comparison
                var guidValue = Guid.Parse(right);
                return FilterValue.Create(guidValue, typeof(Guid));
            }

            var convertedValue = conversionFunction(right);
            return FilterValue.Create(convertedValue, leftExprType);
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
            return FilterValue.Create(enumValue, rawType);
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
                return FilterValue.Create(boolVal, typeof(bool));
            }
        }

        throw new InvalidOperationException($"Unsupported value '{right}' for type '{targetType.Name}'");
    }

    private static Type TransformTargetTypeIfNullable(Type targetType)
    {
        if (targetType.IsNullable())
        {
            targetType = Nullable.GetUnderlyingType(targetType) ?? targetType;
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
        var comparisonOperatorParser = ComparisonOperatorParser(config).Token();
        var rightSideValueParser = RightSideValueParser.Token();
        
        // Only parse arithmetic expressions that are in parentheses and contain arithmetic operators
        var parenthesizedArithmetic = Grouped(ArithmeticExpressionParser).Token();
        
        // Ensure the arithmetic expression contains actual arithmetic operators
        var validArithmeticExpr = parenthesizedArithmetic.Where(expr => ContainsArithmeticOperator(expr));
        
        return validArithmeticExpr
            .SelectMany(leftArithmetic => comparisonOperatorParser, (leftArithmetic, op) => new { leftArithmetic, op })
            .SelectMany(temp => parenthesizedArithmetic.Or(rightSideValueParser.Select(value => CreateArithmeticFromValue(value.Value))), (temp, rightSide) => new { temp.leftArithmetic, temp.op, rightSide })
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

    // The filter settings of a left-side property: by the name that its query name maps to, in the exact case.
    // Derived properties and custom operations are not in this lookup.
    private static QueryKitPropertyInfo? GetFilterPropertyInfo(string text, IQueryKitConfiguration? config)
        => config?.PropertyMappings?.GetPropertyInfo(config.PropertyMappings.GetPropertyPathByQueryName(text) ?? text);

    private static CaseInsensitiveMode ResolveCaseMode(string? propertyPath, IQueryKitConfiguration? config)
    {
        if (!string.IsNullOrEmpty(propertyPath) && config?.PropertyMappings != null)
        {
            var propertyInfo = config.PropertyMappings.GetPropertyInfo(propertyPath)
                               ?? config.PropertyMappings.GetPropertyInfoByQueryName(propertyPath);
            if (propertyInfo?.CaseInsensitiveComparison.HasValue == true)
                return propertyInfo.CaseInsensitiveComparison.Value;
        }
        return config?.CaseInsensitiveComparison ?? CaseInsensitiveMode.Lower;
    }

    private static Parser<Expression> ComparisonExprParser<T>(ParameterExpression parameter, IQueryKitConfiguration? config)
    {
        var comparisonOperatorParser = ComparisonOperatorParser(config).Token();
        var rightSideValueParser = RightSideValueParser.Token();

        // Try property list comparison first (e.g., (firstName, lastName) @=* "paul")
        var propertyListComparison = PropertyListComparisonExprParser<T>(parameter, config);

        // Try arithmetic comparison (e.g., (price + tax) > 100)
        var arithmeticComparison = ArithmeticComparisonExprParser<T>(parameter, config);

        var regularComparison = CreateLeftExprParser(parameter.Type, config)
            .SelectMany(reference => comparisonOperatorParser, (reference, op) => new { reference, op })
            .SelectMany(temp => rightSideValueParser, (temp, rightValue) => new { temp.reference, temp.op, right = rightValue.Value, rightIsQuotedLiteral = rightValue.IsQuotedLiteral })
            .Select(temp =>
            {
                if (temp.reference.Kind == PropertyReferenceKind.CustomOperation)
                {
                    return CreateCustomOperationExpression<T>(parameter, temp.reference.Mapping!, temp.op, temp.right);
                }

                if (temp.reference.Kind == PropertyReferenceKind.Unknown)
                {
                    return IgnoredClause(config);
                }

                var leftExpr = CreateLeftExpr(parameter, temp.reference, config);
                if (leftExpr is RemovedClauseExpression)
                {
                    return IgnoredClause(config);
                }

                if (leftExpr.Type == typeof(Guid) || leftExpr.Type == typeof(Guid?))
                {
                    // Try to determine the property path for HasConversion support
                    string? guidPropertyPath = null;
                    if (leftExpr is MemberExpression guidMemberExpr)
                    {
                        guidPropertyPath = GetPropertyPath(guidMemberExpr, parameter);
                    }

                    // Only convert to string for operators that require string comparison (Contains, StartsWith, etc.)
                    // For equality/comparison operators, keep as GUID for better EF Core translation
                    if (temp.op.IsStringComparisonOperator())
                    {
                        var guidStringExpr = HandleGuidConversion(leftExpr, leftExpr.Type);
                        return temp.op.GetExpression<T>(guidStringExpr, CreateRightExpr(leftExpr, temp.right, temp.rightIsQuotedLiteral, temp.op, config, guidPropertyPath),
                            config?.DbContextType, ResolveCaseMode(guidPropertyPath, config));
                    }

                    // For non-string operators, use direct GUID comparison
                    return temp.op.GetExpression<T>(leftExpr, CreateRightExpr(leftExpr, temp.right, temp.rightIsQuotedLiteral, temp.op, config, guidPropertyPath),
                        config?.DbContextType);
                }

                // Check if the right side is a property path for property-to-property comparison.
                // A quoted string literal is always a value, even when its text matches a property name.
                if (!temp.rightIsQuotedLiteral && IsPropertyPath(temp.right, parameter.Type))
                {
                    var rightPropertyExpr = CreateRightPropertyExpr<T>(parameter, temp.right, config);
                    if (rightPropertyExpr != null)
                    {
                        // Handle GUID conversion for property-to-property comparisons
                        // Only convert to string for string operators
                        var comparedLeftExpr = leftExpr;
                        if (temp.op.IsStringComparisonOperator())
                        {
                            if (comparedLeftExpr.Type == typeof(Guid) || comparedLeftExpr.Type == typeof(Guid?))
                            {
                                comparedLeftExpr = HandleGuidConversion(comparedLeftExpr, comparedLeftExpr.Type);
                            }
                            if (rightPropertyExpr.Type == typeof(Guid) || rightPropertyExpr.Type == typeof(Guid?))
                            {
                                rightPropertyExpr = HandleGuidConversion(rightPropertyExpr, rightPropertyExpr.Type);
                            }
                        }

                        // Ensure compatible types for property-to-property comparison
                        var (leftCompatible, rightCompatible) = EnsureCompatibleTypes(comparedLeftExpr, rightPropertyExpr);
                        var propToProptPath = leftExpr is MemberExpression ptpMemberExpr ? GetPropertyPath(ptpMemberExpr, parameter) : null;
                        return temp.op.GetExpression<T>(leftCompatible, rightCompatible, config?.DbContextType, ResolveCaseMode(propToProptPath, config));
                    }
                }

                // Try to determine the property path for HasConversion support
                string? propertyPath = null;
                if (leftExpr is MemberExpression memberExpr)
                {
                    propertyPath = GetPropertyPath(memberExpr, parameter);
                }

                var leftExprForComparison = leftExpr;

                // If the left expression is a conditional with Object type, convert it to the proper type
                if (leftExprForComparison.Type == typeof(object))
                {
                    var innerExpr = leftExprForComparison;

                    // Unwrap Convert/Unary expressions
                    while (innerExpr is UnaryExpression unaryExpr && unaryExpr.NodeType == ExpressionType.Convert)
                    {
                        innerExpr = unaryExpr.Operand;
                    }

                    if (innerExpr is ConditionalExpression conditionalExpr)
                    {
                        // Determine the actual type
                        var trueType = conditionalExpr.IfTrue.Type;
                        var falseType = conditionalExpr.IfFalse.Type;
                        Type actualType = typeof(object);

                        if (trueType == falseType)
                        {
                            actualType = trueType;
                        }
                        else if (Nullable.GetUnderlyingType(trueType) != null || Nullable.GetUnderlyingType(falseType) != null)
                        {
                            var underlyingTrue = Nullable.GetUnderlyingType(trueType) ?? trueType;
                            var underlyingFalse = Nullable.GetUnderlyingType(falseType) ?? falseType;

                            if (underlyingTrue == underlyingFalse)
                            {
                                actualType = typeof(Nullable<>).MakeGenericType(underlyingTrue);
                            }
                        }
                        else if (trueType.IsAssignableFrom(falseType))
                        {
                            actualType = trueType;
                        }
                        else if (falseType.IsAssignableFrom(trueType))
                        {
                            actualType = falseType;
                        }

                        // If we found a better type, recreate the conditional with the correct type
                        if (actualType != typeof(object) && actualType != null)
                        {
                            // Create a new conditional expression with the correct return type
                            // This ensures proper type handling without unnecessary conversions
                            var newIfTrue = conditionalExpr.IfTrue;
                            var newIfFalse = conditionalExpr.IfFalse;

                            // Convert branches to the target type if needed
                            if (newIfTrue.Type != actualType)
                            {
                                newIfTrue = Expression.Convert(newIfTrue, actualType);
                            }
                            if (newIfFalse.Type != actualType)
                            {
                                newIfFalse = Expression.Convert(newIfFalse, actualType);
                            }

                            leftExprForComparison = Expression.Condition(
                                conditionalExpr.Test,
                                newIfTrue,
                                newIfFalse,
                                actualType);
                        }
                    }
                }

                var rightExpr = CreateRightExpr(leftExprForComparison, temp.right, temp.rightIsQuotedLiteral, temp.op, config, propertyPath);

                // Handle nested collection filtering
                if (leftExprForComparison is MethodCallExpression methodCall && IsNestedCollectionExpression(methodCall))
                {
                    return CreateNestedCollectionFilterExpression<T>(methodCall, rightExpr, temp.op);
                }


                return temp.op.GetExpression<T>(leftExprForComparison, rightExpr, config?.DbContextType, ResolveCaseMode(propertyPath, config));
            });

        return propertyListComparison.Or(arithmeticComparison).Or(regularComparison);
    }

    private static Parser<PropertyReference> CreateLeftExprParser(Type entityType, IQueryKitConfiguration? config)
    {
        var leftPropertyParser = IdentifierPathParser.Token();
        var queryNameParser = DerivedOrCustomOperationQueryNameParser(config).Token();
        return input =>
        {
            var left = leftPropertyParser(input);
            var reference = left.WasSuccessful ? PropertyResolver.Resolve(entityType, left.Value, config) : null;
            if (reference != null && (reference.Kind != PropertyReferenceKind.Unknown || config?.AllowUnknownProperties == true))
            {
                return Result.Success(reference, left.Remainder);
            }

            // v1.14.2 did not accept the text here, so a derived property or custom operation query name can not change an accepted filter.
            var queryName = queryNameParser(input);
            if (queryName.WasSuccessful)
            {
                return Result.Success(PropertyResolver.Resolve(entityType, queryName.Value, config), queryName.Remainder);
            }

            if (reference == null)
            {
                return Result.Failure<PropertyReference>(left.Remainder, left.Message, left.Expectations);
            }

            throw new UnknownFilterPropertyException(reference.UnknownSegment!);
        };
    }

    // The rewrite before the parse does not replace the query name of a derived property or a custom operation,
    // so the grammar reads it when the identifier path is not a property. This lets the query name hold any text (for example `full-name` or `full name`).
    // Longer query names are tried first, so a query name that starts with another query name still matches.
    private static Parser<string> DerivedOrCustomOperationQueryNameParser(IQueryKitConfiguration? config)
    {
        Parser<string> parser = i => Result.Failure<string>(i, "no query name", Array.Empty<string>());
        var mappings = config?.PropertyMappings;
        if (mappings == null)
        {
            return parser;
        }

        var queryNames = mappings.DerivedPropertyMappings.Values.Concat(mappings.CustomOperationMappings.Values)
            .Select(info => info.QueryName)
            .Where(queryName => !string.IsNullOrEmpty(queryName))
            .Select(queryName => queryName!)
            .Distinct(StringComparer.InvariantCultureIgnoreCase)
            .OrderByDescending(queryName => queryName.Length);
        foreach (var queryName in queryNames)
        {
            parser = parser.Or(WholeQueryName(queryName));
        }

        return parser;
    }

    // A query name is a whole name: the next character can not continue a property path.
    private static Parser<string> WholeQueryName(string queryName) => input =>
    {
        var result = Parse.IgnoreCase(queryName).Text()(input);
        if (!result.WasSuccessful || result.Remainder.AtEnd || !IsPropertyPathChar(result.Remainder.Current))
        {
            return result;
        }

        return Result.Failure<string>(input, $"Query name '{queryName}' must not be followed by '{result.Remainder.Current}'", new[] { queryName });
    };

    private static bool IsPropertyPathChar(char c) => char.IsLetterOrDigit(c) || c == '_' || c == '.';

    private static Expression CreateLeftExpr(ParameterExpression parameter, PropertyReference reference, IQueryKitConfiguration? config)
    {
        var propertyExpression = reference.Kind == PropertyReferenceKind.DerivedProperty
            ? reference.Mapping!.DerivedExpression!
            : CreateMemberExpression(parameter, reference.Path);

        if (GetFilterPropertyInfo(reference.Text, config)?.CanFilter == false)
        {
            return RemovedClauseExpression.Instance;
        }

        // Check if this property uses HasConversion
        var propertyConfig = config?.PropertyMappings?.GetPropertyInfoByQueryName(
            config.PropertyMappings.GetPropertyPathByQueryName(reference.Text) ?? reference.Text);
        if (propertyConfig?.UsesConversion == true)
        {
            // For HasConversion properties, return the property expression as-is
            // EF Core will handle the type conversion automatically when it translates the expression to SQL
            // The key is that the right-side value will be converted to match the property's conversion target type
            return propertyExpression;
        }
        
        // Also check if this is a nested property where the parent has HasConversion configured
        if (propertyExpression is MemberExpression nestedMemberExpression &&
            nestedMemberExpression.Expression is MemberExpression parentExpression)
        {
            var parentPropertyPath = GetPropertyPath(parentExpression, parameter);
            var parentPropertyConfig = config?.PropertyMappings?.GetPropertyInfoByQueryName(parentPropertyPath);
            
            if (parentPropertyConfig?.UsesConversion == true)
            {
                // Use the parent expression instead of the nested property
                return parentExpression;
            }
        }

        return propertyExpression;
    }
    
    private static string GetPropertyPath(MemberExpression memberExpression, ParameterExpression parameter)
    {
        var parts = new List<string>();
        var current = memberExpression;

        while (current != null)
        {
            parts.Insert(0, current.Member.Name);

            if (current.Expression == parameter)
                break;

            current = current.Expression as MemberExpression;
        }

        return string.Join(".", parts);
    }

    // Builds the access expression for a resolved member path. A member of a collection element becomes a Select, or a SelectMany when the member is a collection too.
    private static Expression CreateMemberExpression(ParameterExpression parameter, string memberPath)
    {
        return memberPath.Split('.').Aggregate((Expression)parameter, (expr, memberName) =>
        {
            if (expr is MemberExpression member && IsEnumerable(member.Type))
            {
                var genericArgType = member.Type.GetGenericArguments()[0];
                var innerParameter = Expression.Parameter(genericArgType, "y");
                Expression lambdaBody = Expression.PropertyOrField(innerParameter, memberName);
                var propertyType = lambdaBody.Type;

                if (IsEnumerable(propertyType))
                {
                    propertyType = propertyType.GetGenericArguments()[0];

                    var selectManyMethod = typeof(Enumerable).GetMethods()
                        .First(m => m.Name == "SelectMany" && m.GetParameters().Length == 2)
                        .MakeGenericMethod(genericArgType, propertyType);

                    // Ensure the lambda body returns IEnumerable<T> for SelectMany
                    var expectedType = typeof(IEnumerable<>).MakeGenericType(propertyType);
                    if (lambdaBody.Type != expectedType && !expectedType.IsAssignableFrom(lambdaBody.Type))
                    {
                        // Convert to IEnumerable<T> if needed (e.g., List<T> to IEnumerable<T>)
                        lambdaBody = Expression.Convert(lambdaBody, expectedType);
                    }

                    // Create lambda with the correct return type
                    var lambdaType = typeof(Func<,>).MakeGenericType(genericArgType, expectedType);
                    return Expression.Call(selectManyMethod, member, Expression.Lambda(lambdaType, lambdaBody, innerParameter));
                }

                var selectMethod = typeof(Enumerable).GetMethods()
                    .First(m => m.Name == "Select" && m.GetParameters().Length == 2)
                    .MakeGenericMethod(genericArgType, propertyType);

                var selectLambda = Expression.Lambda(lambdaBody, innerParameter);
                var selectResult = Expression.Call(null, selectMethod, member, selectLambda);

                return HandleGuidConversion(selectResult, propertyType, "Select");
            }

            if (expr is MethodCallExpression call)
            {
                var innerGenericType = GetInnerGenericType(call.Method.ReturnType)!;
                var innerParameter = Expression.Parameter(innerGenericType, "y");
                var lambdaBody = Expression.PropertyOrField(innerParameter, memberName);

                var propertyType = lambdaBody.Type;
                var linqMethod = IsEnumerable(propertyType) ? "SelectMany" : "Select";
                var resultType = IsEnumerable(propertyType) ? propertyType.GetGenericArguments()[0] : propertyType;

                var selectMethod = typeof(Enumerable).GetMethods()
                    .First(m => m.Name == linqMethod && m.GetParameters().Length == 2)
                    .MakeGenericMethod(innerGenericType, resultType);

                var selectLambda = Expression.Lambda(lambdaBody, innerParameter);

                return Expression.Call(selectMethod, expr, selectLambda);
            }

            return Expression.PropertyOrField(expr, memberName);
        });
    }

    private static Parser<Expression> PropertyListComparisonExprParser<T>(
        ParameterExpression parameter,
        IQueryKitConfiguration? config)
    {
        var comparisonOperatorParser = ComparisonOperatorParser(config).Token();
        var rightSideValueParser = RightSideValueParser.Token();

        return PropertyListParser(IdentifierPathParser)
            .SelectMany(properties => comparisonOperatorParser,
                (properties, op) => new { properties, op })
            .SelectMany(temp => rightSideValueParser,
                (temp, rightValue) => new { temp.properties, temp.op, right = rightValue.Value, rightIsQuotedLiteral = rightValue.IsQuotedLiteral })
            .Select(temp =>
            {
                if (!temp.properties.Any())
                {
                    throw new InvalidOperationException("Property list cannot be empty");
                }

                Expression? result = null;

                // For negative operators (NotEquals, NotContains, NotStartsWith, NotEndsWith, NotIn, DoesNotHave),
                // we use AND instead of OR so that all properties must NOT match
                var isNegativeOperator = temp.op.Operator().StartsWith("!") || temp.op.Operator().Contains("!=");

                foreach (var fullPropPath in temp.properties)
                {
                    // Build expression for each property. A property list does not support custom operations.
                    // Check if property can be filtered
                    var propertyConfig = config?.PropertyMappings?.GetPropertyInfo(fullPropPath);
                    if (propertyConfig != null && !propertyConfig.CanFilter)
                    {
                        continue;
                    }

                    var reference = PropertyResolver.Resolve(parameter.Type, fullPropPath, config);

                    if (reference.Kind is PropertyReferenceKind.Unknown or PropertyReferenceKind.CustomOperation)
                    {
                        if (config?.AllowUnknownProperties == true)
                        {
                            continue;
                        }

                        throw new UnknownFilterPropertyException(reference.UnknownSegment!);
                    }

                    var leftExpr = reference.Kind == PropertyReferenceKind.DerivedProperty
                        ? reference.Mapping!.DerivedExpression!
                        : CreateMemberExpression(parameter, reference.Path);

                    // Handle GUID conversion for string operators
                    if ((leftExpr.Type == typeof(Guid) || leftExpr.Type == typeof(Guid?)) &&
                        temp.op.IsStringComparisonOperator())
                    {
                        leftExpr = HandleGuidConversion(leftExpr, leftExpr.Type);
                    }

                    var rightExpr = CreateRightExpr(leftExpr, temp.right, temp.rightIsQuotedLiteral, temp.op, config, fullPropPath);
                    var comparison = temp.op.GetExpression<T>(leftExpr, rightExpr, config?.DbContextType, ResolveCaseMode(fullPropPath, config));

                    // Combine with AND for negative operators, OR for positive operators
                    result = result == null
                        ? comparison
                        : isNegativeOperator
                            ? Expression.AndAlso(result, comparison)
                            : Expression.OrElse(result, comparison);
                }

                // If all properties were filtered out, the clause is ignored. v1.14.2 used true here, not true == true.
                return result ?? (RemovesIgnoredClauses(config) ? RemovedClauseExpression.Instance : Expression.Constant(true));
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
            .Or(Grouped(Parse.Ref(() => ExprParser<T>(parameter, config))));

    private static Parser<Expression> ExprParser<T>(ParameterExpression parameter, IQueryKitConfiguration? config = null)
        => OrExprParser<T>(parameter, config);
    
    private static Parser<Expression> AndExprParser<T>(ParameterExpression parameter, IQueryKitConfiguration? config = null)
        => Parse.ChainOperator(
            LogicalOperatorParserWithAliases(config).Where(x => x.Name == LogicalOperator.AndOperator.Operator()),
            AtomicExprParser<T>(parameter, config),
            CombineClauses<T>
        );

    private static Parser<Expression> OrExprParser<T>(ParameterExpression parameter, IQueryKitConfiguration? config = null)
        => Parse.ChainOperator(
            LogicalOperatorParserWithAliases(config).Where(x => x.Name == LogicalOperator.OrOperator.Operator()),
            AndExprParser<T>(parameter, config),
            CombineClauses<T>
        );

    private static bool RemovesIgnoredClauses(IQueryKitConfiguration? config)
        => config is IQueryKitFilterBehavior { IgnoredClauseBehavior: IgnoredClauseBehavior.Remove };

    // A clause on a prevented or unknown property. By default it becomes true == true, the same as v1.14.2.
    private static Expression IgnoredClause(IQueryKitConfiguration? config)
        => RemovesIgnoredClauses(config)
            ? RemovedClauseExpression.Instance
            : Expression.Equal(Expression.Constant(true), Expression.Constant(true));

    // A removed clause has no effect, so the operator keeps only the other side
    private static Expression CombineClauses<T>(LogicalOperator op, Expression left, Expression right)
    {
        if (left is RemovedClauseExpression)
        {
            return right;
        }

        if (right is RemovedClauseExpression)
        {
            return left;
        }

        return op.GetExpression<T>(left, right);
    }
    
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
        public CollectionInfo(Type collectionElementType, string propertyName)
        {
            CollectionElementType = collectionElementType;
            PropertyName = propertyName;
        }

        public Type CollectionElementType { get; }
        public string PropertyName { get; }
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
                result.Insert(0, new CollectionInfo(elementType, member.Member.Name));
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
            value.Contains("-") && (DateTime.TryParse(value, out _) || DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out _)) ||
            decimal.TryParse(value, out _) ||
            decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out _) ||
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
            // Validate property depth before processing
            config?.ValidatePropertyDepth(propertyPath);

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
        object? convertedValue = ConvertStringToBasicType(rightValue);
        
        // Create the parameter expressions for the custom operation
        var entityParameter = Expression.Convert(parameter, typeof(object));
        var operatorParameter = Expression.Constant(op, typeof(ComparisonOperator));
        var valueParameter = Expression.Constant(convertedValue, typeof(object));

        // Invoke the custom operation
        var customOperationLambda = customOperationInfo.CustomOperation;
        var invocationExpression = Expression.Invoke(customOperationLambda, entityParameter, operatorParameter, valueParameter);

        return invocationExpression;
    }

    private static object? ConvertStringToBasicType(string value)
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


