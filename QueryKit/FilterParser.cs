namespace QueryKit;

using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
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
        
        var parameter = Expression.Parameter(typeof(T), "x");
        Expression expr;
        var parameterizeBefore = FilterValue.Parameterize;
        FilterValue.Parameterize = config is IQueryKitFilterBehavior { ParameterizeFilterValues: true };
        var maxNestingDepthBefore = _maxNestingDepth;
        var nestingDepthBefore = _nestingDepth;
        var queryNameOverUnknownBefore = _queryNameOverUnknown;
        var queryNameFallbackOffBefore = _queryNameFallbackOff;
        var dateTimeKindBefore = _dateTimeKindForValuesWithoutOffset;
        _maxNestingDepth = (config as IQueryKitParseLimits)?.MaxNestingDepth ?? QueryKitSettings.DefaultMaxNestingDepth;
        _nestingDepth = 0;
        _queryNameOverUnknown = false;
        _queryNameFallbackOff = false;
        _dateTimeKindForValuesWithoutOffset = (config as IQueryKitFilterBehavior)?.DateTimeKindForValuesWithoutOffset;
        try
        {
            try
            {
                expr = ExprParser<T>(parameter, config).End().Parse(input);
            }
            catch (Exception) when (_queryNameOverUnknown)
            {
                // v1.14.2 threw UnknownFilterPropertyException where a query name now reads an unknown identifier.
                // A filter that fails now also failed in v1.14.2, so parse it again without the query names to get the v1.14.2 exception.
                _queryNameFallbackOff = true;
                _nestingDepth = 0;
                expr = ExprParser<T>(parameter, config).End().Parse(input);
            }

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
        catch (FormatException e)
        {
            throw new ParsingException(e);
        }
        catch (OverflowException e)
        {
            throw new ParsingException(e);
        }
        finally
        {
            FilterValue.Parameterize = parameterizeBefore;
            _maxNestingDepth = maxNestingDepthBefore;
            _nestingDepth = nestingDepthBefore;
            _queryNameOverUnknown = queryNameOverUnknownBefore;
            _queryNameFallbackOff = queryNameFallbackOffBefore;
            _dateTimeKindForValuesWithoutOffset = dateTimeKindBefore;
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

    // Set when a derived property or custom operation query name reads text where v1.14.2 read an unknown identifier.
    // When the fallback is off, the parser throws for the unknown identifier like v1.14.2.
    [ThreadStatic] private static bool _queryNameOverUnknown;
    [ThreadStatic] private static bool _queryNameFallbackOff;

    // The kind of a DateTime value without an offset in the parse on this thread. Null means the default, UTC.
    [ThreadStatic] private static DateTimeKind? _dateTimeKindForValuesWithoutOffset;

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

    // A property is a configured query name or a path of identifiers. Query names are matched in the grammar,
    // so a query name can hold any text (e.g. `first-name`, `_first`, or `first name`) and text inside quoted values is never changed.
    // Longer query names are tried first so a query name that starts with another query name (e.g. `first` and `first name`) still matches.
    // Every query name ignores case with the rules of the invariant culture, so the result does not depend on the culture of the parse.
    private static Parser<string> PropertyPathParser(IQueryKitConfiguration? config)
    {
        Parser<string> parser = i => Result.Failure<string>(i, "no query name", Array.Empty<string>());
        var mappings = config?.PropertyMappings;
        if (mappings != null)
        {
            var queryNames = mappings.PropertyQueryNames
                .Concat(mappings.DerivedOrCustomOperationQueryNames)
                .OrderByDescending(queryName => queryName.Length);
            foreach (var queryName in queryNames)
            {
                parser = parser.Or(QueryName(queryName));
            }
        }

        return parser.Or(IdentifierPathParser);
    }

    // A query name is a whole name: the next character can not continue a property path.
    private static Parser<string> QueryName(string queryName) => input =>
    {
        var remainder = input;
        foreach (var c in queryName)
        {
            if (remainder.AtEnd || char.ToLowerInvariant(remainder.Current) != char.ToLowerInvariant(c))
            {
                return Result.Failure<string>(input, $"Query name '{queryName}' expected", new[] { queryName });
            }

            remainder = remainder.Advance();
        }

        if (!remainder.AtEnd && IsPropertyPathChar(remainder.Current))
        {
            return Result.Failure<string>(input, $"Query name '{queryName}' must not be followed by '{remainder.Current}'", new[] { queryName });
        }

        return Result.Success(queryName, remainder);
    };

    private static bool IsPropertyPathChar(char c) => char.IsLetterOrDigit(c) || c == '_' || c == '.';

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
        var operatorParser = ComparisonOperatorAliasParser(config).Or(CanonicalComparisonOperatorParser);
        return Parse.Char(ComparisonOperator.AllPrefix).Optional().Select(opt => opt.IsDefined)
            .Then(hasHash => operatorParser.Select(x => ComparisonOperator.GetByOperatorString(x.Operator, x.CaseInsensitive, hasHash)));
    }

    // Aliases are matched in the grammar (not by rewriting the input) so text inside quoted values is never changed.
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
    private static readonly Parser<string> DateTimeFormatParser =
        from dateFormat in Parse.Regex(@"\d{4}-\d{2}-\d{2}").Text()
        from timeFormat in DateTimeTimeParser
        from micros in DateTimeMicrosParser
        from timeZone in DateTimeZoneParser
        select dateFormat + timeFormat + micros + timeZone;

    // A number always uses the '.' decimal point, so a filter has the same meaning in every culture.
    private static readonly Parser<string> NumberParser =
        from sign in Parse.Char('-').Optional().Select(x => x.IsDefined ? "-" : "")
        from number in Parse.DecimalInvariant
        select sign + number;

    // v1.14.2 read a number only with the decimal separator of the current culture.
    private static readonly Parser<string> CultureNumberParser =
        from sign in Parse.Char('-').Optional().Select(x => x.IsDefined ? "-" : "")
        from number in Parse.Decimal
        select sign + number;

    // The part of a number that v1.14.2 read: null when the current culture reads the whole number,
    // else the part before the '.' (for example "4" of "4.5" in de-DE), or "" when the culture reads no number.
    private static string? CultureNumberPrefix(string number)
    {
        var result = CultureNumberParser.TryParse(number);
        if (!result.WasSuccessful)
        {
            return "";
        }

        return result.Value.Length == number.Length ? null : result.Value;
    }

    // In a culture whose decimal separator is not '.', v1.14.2 read only the prefix of a '.' number (see CultureNumberPrefix),
    // built the clause with that prefix, and then failed in the grammar at the '.'.
    // If the clause builds with the whole number, the filter is valid. If it does not build, give the v1.14.2 result:
    // the exception of the clause for the prefix, else ParsingException. In a '.' culture, nothing changes.
    private static Expression BuildClauseLikeV1142(string? cultureNumberPrefix, string right, Func<string, Expression> buildClause)
    {
        if (cultureNumberPrefix == null)
        {
            return buildClause(right);
        }

        try
        {
            return buildClause(right);
        }
        catch (Exception exception)
        {
            if (cultureNumberPrefix.Length > 0)
            {
                buildClause(cultureNumberPrefix);
            }

            // A value that does not convert already has a ParsingException that names the value.
            if (exception is ParsingException)
            {
                throw;
            }

            throw new ParsingException(exception);
        }
    }

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
    // CultureNumberPrefix is set when the value holds a number that only the '.' decimal point reads (see BuildClauseLikeV1142).
    // IsPropertyPath is set for unquoted identifiers joined with '.', which must resolve to a property path.
    private readonly record struct RightSideValue(string Value, bool IsQuotedLiteral, string? CultureNumberPrefix = null, bool IsPropertyPath = false);

    private static readonly Parser<IEnumerable<(string Value, bool IsDotOnlyNumber)>> SquareBracketValuesParser =
        Parse.String("null").Text()
            .Or(GuidFormatParser)
            .Or(DateTimeFormatParser)
            .Or(TimeFormatParser)
            .Select(v => (v, false))
            .Or(NumberParser.Select(v => (v, CultureNumberPrefix(v) != null)))
            .Or(RawStringLiteralParser.Or(DoubleQuoteParser).Or(Identifier).Select(v => (v, false)))
            .DelimitedBy(Parse.Char(',').Token());

    // v1.14.2 failed in the grammar at a '.' list number, before it built the clause, so no part of the list is read.
    private static readonly Parser<RightSideValue> SquareBracketParser =
        from openingBracket in Parse.Char('[')
        from content in SquareBracketValuesParser
        from closingBracket in Parse.Char(']')
        select new RightSideValue("[" + string.Join(",", content.Select(x => EscapeListItem(x.Value))) + "]", false,
            content.Any(x => x.IsDotOnlyNumber) ? "" : null);

    // List items are joined with ',' so quoted items that contain ',' or '\' are escaped and split back with SplitListItems, which trims each item
    private static string EscapeListItem(string item)
        => item.Replace(@"\", @"\\").Replace(",", @"\,");

    private static string UnescapeListText(string list)
    {
        var text = new StringBuilder(list.Length);
        for (var i = 0; i < list.Length; i++)
        {
            if (list[i] == '\\' && i + 1 < list.Length)
                i++;
            text.Append(list[i]);
        }

        return text.ToString();
    }

    private static List<string> SplitListItems(string list)
    {
        var items = new List<string>();
        var current = new StringBuilder();
        var content = list.Substring(1, list.Length - 2);
        for (var i = 0; i < content.Length; i++)
        {
            if (content[i] == '\\' && i + 1 < content.Length)
            {
                current.Append(content[++i]);
            }
            else if (content[i] == ',')
            {
                items.Add(current.ToString().Trim());
                current.Clear();
            }
            else
            {
                current.Append(content[i]);
            }
        }
        items.Add(current.ToString().Trim());

        return items;
    }

    private static readonly Parser<RightSideValue> RightSideValueChoiceParser =
        Parse.String("null").Text().Select(v => new RightSideValue(v, false))
            .Or(GuidFormatParser.Select(v => new RightSideValue(v, false)))
            .XOr(DateTimeFormatParser.Select(v => new RightSideValue(v, false)))
            .XOr(TimeFormatParser.Select(v => new RightSideValue(v, false)))
            .XOr(NumberParser.Select(v => new RightSideValue(v, false, CultureNumberPrefix(v))))
            .XOr((RawStringLiteralParser.Or(DoubleQuoteParser)).Select(v => new RightSideValue(v, true)))
            .XOr(SquareBracketParser)
            .XOr(Identifier.DelimitedBy(Parse.Char('.')).Select(v => v.ToList()).Select(v => new RightSideValue(string.Join(".", v), false, IsPropertyPath: v.Count > 1))); // Keep this last to try property paths only if nothing else matches

    private static readonly Parser<RightSideValue> RightSideValueParser =
        from atSign in Parse.Char('@').Optional()
        from leadingSpaces in Parse.WhiteSpace.Many()
        from value in RightSideValueChoiceParser
        from trailingSpaces in Parse.WhiteSpace.Many()
        select atSign.IsDefined
            ? value with { Value = "@" + value.Value, CultureNumberPrefix = value.CultureNumberPrefix is { Length: > 0 } prefix ? "@" + prefix : value.CultureNumberPrefix }
            : value;

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

    private static DateTimeKind DateTimeKindForValuesWithoutOffset => _dateTimeKindForValuesWithoutOffset ?? DateTimeKind.Utc;

    // A date and time value without an offset gets the kind from DateTimeKindForValuesWithoutOffset. Utc, the default,
    // does not depend on the server time zone. Unspecified keeps the wall-clock time for a timestamp without time zone
    // column, and a value with an offset becomes its UTC time. Local reads the value in the server time zone.
    private static DateTime ParseDateTime(string value)
        => ToKindForValuesWithoutOffset(DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStylesForValuesWithoutOffset()));

    private static bool TryParseDateTime(string value, out DateTime result)
    {
        var parsed = DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStylesForValuesWithoutOffset(), out result);
        result = ToKindForValuesWithoutOffset(result);
        return parsed;
    }

    private static DateTimeStyles DateTimeStylesForValuesWithoutOffset() => DateTimeKindForValuesWithoutOffset switch
    {
        DateTimeKind.Local => DateTimeStyles.AssumeLocal,
        DateTimeKind.Unspecified => DateTimeStyles.AdjustToUniversal,
        _ => DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal
    };

    private static DateTime ToKindForValuesWithoutOffset(DateTime value)
        => DateTimeKindForValuesWithoutOffset == DateTimeKind.Unspecified ? DateTime.SpecifyKind(value, DateTimeKind.Unspecified) : value;

    // Npgsql only accepts a DateTimeOffset parameter with offset 0. The UTC value is the same instant.
    private static DateTimeOffset ParseDateTimeOffset(string value)
        => DateTimeOffset.Parse(value, CultureInfo.InvariantCulture,
            DateTimeKindForValuesWithoutOffset == DateTimeKind.Local ? DateTimeStyles.AssumeLocal : DateTimeStyles.AssumeUniversal)
            .ToUniversalTime();

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
        { typeof(DateTime), value => ParseDateTime(value) },
        { typeof(DateTimeOffset), value => ParseDateTimeOffset(value) },
        { typeof(DateOnly), value => DateOnly.Parse(value, CultureInfo.InvariantCulture) },
        { typeof(TimeOnly), value => TimeOnly.Parse(value, CultureInfo.InvariantCulture) },
        { typeof(TimeSpan), value => TimeSpan.Parse(value) },
        { typeof(uint), value => uint.Parse(value, CultureInfo.InvariantCulture) },
        { typeof(ulong), value => ulong.Parse(value, CultureInfo.InvariantCulture) },
        { typeof(ushort), value => ushort.Parse(value, CultureInfo.InvariantCulture) },
        { typeof(sbyte), value => sbyte.Parse(value, CultureInfo.InvariantCulture) },
    };

    // A value that does not convert to the property type throws ParsingException with the value, the type, and the
    // property name as the caller wrote it (the query name, not the member path).
    private static Expression CreateRightExpr(Expression leftExpr, string right, bool rightIsQuotedLiteral, ComparisonOperator op,
        string propertyName, IQueryKitConfiguration? config = null, string? propertyPath = null)
    {
        try
        {
            return CreateRightExprForProperty(leftExpr, right, rightIsQuotedLiteral, op, config, propertyPath);
        }
        catch (InvalidFilterValueException e)
        {
            throw new ParsingException(e.Value, propertyName, e.TargetType, e.InnerException!);
        }
    }

    private static Expression CreateRightExprForProperty(Expression leftExpr, string right, bool rightIsQuotedLiteral, ComparisonOperator op,
        IQueryKitConfiguration? config, string? propertyPath)
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

        // Check if this property uses HasConversion. QueryKit reads a type that it knows (a number, an enum, a Guid) by its own type,
        // like v1.14.2 did for a property with a query name, so the conversion applies only to a type that it can not read.
        if (config?.PropertyMappings != null && !string.IsNullOrEmpty(propertyPath) && !CanCreateRightExprFromType(targetType))
        {
            var propertyConfig = config.PropertyMappings.GetPropertyInfo(propertyPath);
            if (propertyConfig?.UsesConversion == true && propertyConfig.ConversionTargetType != null)
            {
                // For HasConversion properties, try to create a constant of the original type
                // by constructing it from the string value using a constructor that takes the target type
                if (propertyConfig.ConversionTargetType == typeof(string))
                {
                    // A null literal compares against null instead of being passed to the constructor
                    var underlyingType = Nullable.GetUnderlyingType(leftExpr.Type);
                    if (right == "null" && (!leftExpr.Type.IsValueType || underlyingType != null))
                    {
                        return Expression.Constant(null, leftExpr.Type);
                    }

                    // Nullable structs are constructed from their underlying type, then converted back
                    var stringCtor = (underlyingType ?? leftExpr.Type).GetConstructor(new[] { typeof(string) });
                    if (stringCtor != null)
                    {
                        Expression constructed = Expression.New(stringCtor, FilterValue.Create(right, typeof(string)));
                        return underlyingType == null ? constructed : Expression.Convert(constructed, leftExpr.Type);
                    }
                }

                // For other conversion types, fall back to using the conversion target type
                targetType = propertyConfig.ConversionTargetType;
            }
        }

        return CreateRightExprFromType(targetType, right, rightIsQuotedLiteral, op);
    }

    // True when CreateRightExprFromType can read a value of the type. For other types it throws.
    private static bool CanCreateRightExprFromType(Type type)
    {
        var targetType = TransformTargetTypeIfNullable(type);
        return IsEnumerable(type) || TypeConversionFunctions.ContainsKey(targetType) || targetType.IsEnum || targetType == typeof(object);
    }

    private static Expression CreateRightExprFromType(Type leftExprType, string right, bool rightIsQuotedLiteral, ComparisonOperator op)
    {
        var isEnumerable = IsEnumerable(leftExprType);
        var targetType = leftExprType;
        if (isEnumerable)
        {
            if (op.IsCountOperator() && int.TryParse(right, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intVal))
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
            // A quoted "null" is the text null for a string. Other types have no text value, so it stays a null.
            if (right == "null" && !(rightIsQuotedLiteral && targetType == typeof(string)))
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
                var values = SplitListItems(right);
                var elementType = targetType.IsArray ? targetType.GetElementType()! : targetType;

                var expressions = values.Select(x =>
                {
                    if (elementType == typeof(string) && x.StartsWith("\"") && x.EndsWith("\""))
                    {
                        x = x.Trim('"');
                    }

                    var convertedValue = ConvertValue(x, elementType, () => TypeConversionFunctions[elementType](x));
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
                var dt = ConvertValue(right, targetType, () => ParseDateTime(right));

                return FilterValue.Create(dt, rawType);
            }

            if (targetType == typeof(DateTimeOffset))
            {
                var dto = ConvertValue(right, targetType, () => ParseDateTimeOffset(right));

                return FilterValue.Create(dto, rawType);
            }

            if (targetType == typeof(DateOnly))
            {
                var date = ConvertValue(right, targetType, () => DateOnly.Parse(right, CultureInfo.InvariantCulture));
                return FilterValue.Create(date, rawType);
            }

            if (targetType == typeof(TimeOnly))
            {
                var time = ConvertValue(right, targetType, () => TimeOnly.Parse(right, CultureInfo.InvariantCulture));

                var fractionalTicks = time.Ticks % TimeSpan.TicksPerSecond;
                var millisecond = (int)(fractionalTicks / TimeSpan.TicksPerMillisecond);
                var microsecond = (int)(fractionalTicks % TimeSpan.TicksPerMillisecond / 10);

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
                var guidValue = ConvertValue(right, targetType, () => Guid.Parse(right));
                return FilterValue.Create(guidValue, typeof(Guid));
            }

            var convertedValue = ConvertValue(right, targetType, () => conversionFunction(right));
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
                var values = SplitListItems(right);
                var elementType = targetType.IsArray ? targetType.GetElementType() : targetType;
            
                var expressions = values.Select<string, Expression>(x =>
                {
                    if (elementType == typeof(string) && x.StartsWith("\"") && x.EndsWith("\""))
                    {
                        x = x.Trim('"');
                    }
            
                    var enumValue = ConvertValue(x, enumType, () => Enum.Parse(enumType, x));
                    var constant = Expression.Constant(enumValue, enumType);
            
                    return constant;
                }).ToArray();
            
                var newArrayExpression = Expression.NewArrayInit(enumType, expressions);
                return newArrayExpression;
            }
            
            var enumValue = ConvertValue(right, enumType, () => Enum.Parse(enumType, right));
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

    // Converts one filter value. The parse methods throw FormatException or OverflowException, and Enum.Parse throws
    // ArgumentException. CreateRightExpr adds the property name and throws ParsingException.
    private static TValue ConvertValue<TValue>(string value, Type targetType, Func<TValue> convert)
    {
        try
        {
            return convert();
        }
        catch (Exception e) when (e is FormatException or OverflowException or ArgumentException)
        {
            throw new InvalidFilterValueException(value, targetType, e);
        }
    }

    private sealed class InvalidFilterValueException : Exception
    {
        public InvalidFilterValueException(string value, Type targetType, Exception inner)
            : base(null, inner)
        {
            Value = value;
            TargetType = targetType;
        }

        public string Value { get; }
        public Type TargetType { get; }
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
                var leftArithmetic = ResolveArithmeticProperties(temp.leftArithmetic, typeof(T), config);
                var rightArithmetic = ResolveArithmeticProperties(temp.rightSide, typeof(T), config);
                if (leftArithmetic == null || rightArithmetic == null)
                {
                    return IgnoredClause(config);
                }

                var leftExpr = leftArithmetic.ToLinqExpression(parameter, typeof(T));
                var rightExpr = rightArithmetic.ToLinqExpression(parameter, typeof(T));
                
                var (leftCompatible, rightCompatible) = EnsureCompatibleTypes(leftExpr, rightExpr);
                return temp.op.GetExpression<T>(leftCompatible, rightCompatible, config?.DbContextType);
            });
    }
    
    // Resolves each property in an arithmetic expression to its member path.
    // Returns null when a property cannot be filtered or is an allowed unknown property, because then the parser removes the clause.
    private static ArithmeticExpression? ResolveArithmeticProperties(ArithmeticExpression expr, Type entityType, IQueryKitConfiguration? config)
    {
        switch (expr)
        {
            case PropertyArithmeticExpression property:
                var reference = PropertyResolver.Resolve(entityType, property.PropertyPath, config);
                // Arithmetic supports only members, so a derived property or a custom operation is unknown here
                if (reference.Kind != PropertyReferenceKind.Member)
                {
                    if (config?.AllowUnknownProperties == true)
                    {
                        return null;
                    }

                    throw new UnknownFilterPropertyException(reference.UnknownSegment ?? property.PropertyPath);
                }

                return reference.CanFilter ? new PropertyArithmeticExpression(reference.Path) : null;
            case BinaryArithmeticExpression binary:
                var left = ResolveArithmeticProperties(binary.Left, entityType, config);
                var right = ResolveArithmeticProperties(binary.Right, entityType, config);
                return left == null || right == null ? null : new BinaryArithmeticExpression(left, binary.Operator, right);
            case GroupedArithmeticExpression grouped:
                var inner = ResolveArithmeticProperties(grouped.Inner, entityType, config);
                return inner == null ? null : new GroupedArithmeticExpression(inner);
            default:
                return expr;
        }
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
            .SelectMany(temp => rightSideValueParser, (temp, rightValue) => new { temp.reference, temp.op, right = rightValue.Value, rightIsQuotedLiteral = rightValue.IsQuotedLiteral, cultureNumberPrefix = rightValue.CultureNumberPrefix, rightIsPropertyPath = rightValue.IsPropertyPath })
            .Select(clause => BuildClauseLikeV1142(clause.cultureNumberPrefix, clause.right, right =>
            {
                var temp = clause with { right = right };
                if (temp.reference.Kind == PropertyReferenceKind.CustomOperation)
                {
                    if (!temp.reference.CanFilter)
                    {
                        return IgnoredClause(config);
                    }

                    return CreateCustomOperationExpression<T>(parameter, temp.reference.Mapping!, temp.op, temp.right, temp.rightIsQuotedLiteral);
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

                        // For a Guid with HasConversion<string>(), v1.14.2 built the right side as a Guid and threw, so build it for the string.
                        var guidConfig = config?.PropertyMappings?.GetPropertyInfo(guidPropertyPath);
                        var leftExprForRightSide = guidConfig?.UsesConversion == true && guidConfig.ConversionTargetType == typeof(string)
                            ? guidStringExpr
                            : leftExpr;
                        return temp.op.GetExpression<T>(guidStringExpr, CreateRightExpr(leftExprForRightSide, temp.right, temp.rightIsQuotedLiteral, temp.op, temp.reference.Text, config, guidPropertyPath),
                            config?.DbContextType, ResolveCaseMode(guidPropertyPath, config));
                    }

                    // For non-string operators, use direct GUID comparison
                    return temp.op.GetExpression<T>(leftExpr, CreateRightExpr(leftExpr, temp.right, temp.rightIsQuotedLiteral, temp.op, temp.reference.Text, config, guidPropertyPath),
                        config?.DbContextType);
                }

                // Check if the right side is a property path for property-to-property comparison.
                // A quoted string literal is always a value, even when its text matches a property name.
                // A dotted path must resolve to a property. A single identifier that is not a property stays a value.
                if (temp.rightIsPropertyPath || !temp.rightIsQuotedLiteral && IsPropertyPath(temp.right, parameter.Type))
                {
                    // Build the right side from the resolved path, so that the checked property is the compared property.
                    var rightReference = PropertyResolver.ResolveWithoutQueryName(parameter.Type, temp.right, config);
                    if (!rightReference.CanFilter)
                    {
                        return IgnoredClause(config);
                    }

                    var rightPropertyExpr = rightReference.Kind == PropertyReferenceKind.Member
                        ? CreateRightPropertyExpr<T>(parameter, rightReference.Path, config)
                        : null;
                    if (rightPropertyExpr == null && temp.rightIsPropertyPath)
                    {
                        // A path through a collection resolves to a member, but it is not one value to compare with.
                        throw new UnknownFilterPropertyException(rightReference.UnknownSegment ?? temp.right);
                    }
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

                var rightExpr = CreateRightExpr(leftExprForComparison, temp.right, temp.rightIsQuotedLiteral, temp.op, temp.reference.Text, config, propertyPath);

                // Handle nested collection filtering
                if (leftExprForComparison is MethodCallExpression methodCall && IsNestedCollectionExpression(methodCall))
                {
                    return CreateNestedCollectionFilterExpression<T>(methodCall, rightExpr, temp.op);
                }


                return temp.op.GetExpression<T>(leftExprForComparison, rightExpr, config?.DbContextType, ResolveCaseMode(propertyPath, config));
            }));

        return propertyListComparison.Or(arithmeticComparison).Or(regularComparison);
    }

    private static Parser<PropertyReference> CreateLeftExprParser(Type entityType, IQueryKitConfiguration? config)
    {
        var leftPropertyParser = PropertyPathParser(config).Token();
        var identifierPathParser = IdentifierPathParser.Token();
        var derivedOrCustomOperationQueryNames = new HashSet<string>(
            config?.PropertyMappings?.DerivedOrCustomOperationQueryNames ?? Enumerable.Empty<string>(),
            StringComparer.InvariantCultureIgnoreCase);
        return input =>
        {
            var left = leftPropertyParser(input);
            if (!left.WasSuccessful)
            {
                return Result.Failure<PropertyReference>(left.Remainder, left.Message, left.Expectations);
            }

            // v1.14.2 read an identifier path where the grammar now reads a derived property or custom operation query name.
            // When that identifier path is unknown, a filter that fails throws the v1.14.2 exception for it.
            if (derivedOrCustomOperationQueryNames.Contains(left.Value))
            {
                var identifier = identifierPathParser(input);
                var identifierReference = identifier.WasSuccessful ? PropertyResolver.Resolve(entityType, identifier.Value, config) : null;
                if (identifierReference?.Kind == PropertyReferenceKind.Unknown && config?.AllowUnknownProperties != true)
                {
                    _queryNameOverUnknown = true;
                    if (_queryNameFallbackOff)
                    {
                        throw new UnknownFilterPropertyException(identifierReference.UnknownSegment!);
                    }
                }
            }

            var reference = PropertyResolver.Resolve(entityType, left.Value, config);
            if (reference.Kind == PropertyReferenceKind.Unknown && config?.AllowUnknownProperties != true)
            {
                throw new UnknownFilterPropertyException(reference.UnknownSegment!);
            }

            return Result.Success(reference, left.Remainder);
        };
    }

    private static Expression CreateLeftExpr(ParameterExpression parameter, PropertyReference reference, IQueryKitConfiguration? config)
    {
        var propertyExpression = reference.Kind == PropertyReferenceKind.DerivedProperty
            ? reference.Mapping!.DerivedExpression!
            : CreateMemberExpression(parameter, reference.Path);

        if (!reference.CanFilter)
        {
            return RemovedClauseExpression.Instance;
        }

        // Check if this property uses HasConversion
        if (reference.Mapping?.UsesConversion == true)
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
            var parentPropertyConfig = config?.PropertyMappings?.GetPropertyInfo(parentPropertyPath);
            
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

        return PropertyListParser(PropertyPathParser(config))
            .SelectMany(properties => comparisonOperatorParser,
                (properties, op) => new { properties, op })
            // A property list compares with a value, so a property path on the right side does not parse.
            .SelectMany(temp => rightSideValueParser.Where(rightValue => !rightValue.IsPropertyPath),
                (temp, rightValue) => new { temp.properties, temp.op, right = rightValue.Value, rightIsQuotedLiteral = rightValue.IsQuotedLiteral, cultureNumberPrefix = rightValue.CultureNumberPrefix })
            .Select(clause => BuildClauseLikeV1142(clause.cultureNumberPrefix, clause.right, right =>
            {
                var temp = clause with { right = right };
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
                    var reference = PropertyResolver.Resolve(parameter.Type, fullPropPath, config);
                    if (!reference.CanFilter)
                    {
                        continue;
                    }

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

                    var rightExpr = CreateRightExpr(leftExpr, temp.right, temp.rightIsQuotedLiteral, temp.op, fullPropPath, config, reference.Path);
                    var comparison = temp.op.GetExpression<T>(leftExpr, rightExpr, config?.DbContextType, ResolveCaseMode(reference.Path, config));

                    // Combine with AND for negative operators, OR for positive operators
                    result = result == null
                        ? comparison
                        : isNegativeOperator
                            ? Expression.AndAlso(result, comparison)
                            : Expression.OrElse(result, comparison);
                }

                // If all properties were filtered out, the clause is ignored. v1.14.2 used true here, not true == true.
                return result ?? (RemovesIgnoredClauses(config) ? RemovedClauseExpression.Instance : Expression.Constant(true));
            }));
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
            .Or(Grouped(Parse.Ref(() => ExprParser<T>(parameter, config))).Token());

    private static Parser<Expression> ExprParser<T>(ParameterExpression parameter, IQueryKitConfiguration? config = null)
        => OrExprParser<T>(parameter, config);
    
    private static Parser<Expression> AndExprParser<T>(ParameterExpression parameter, IQueryKitConfiguration? config = null)
        => ChainLeft(
            LogicalOperatorParserWithAliases(config).Where(x => x.Name == LogicalOperator.AndOperator.Operator()),
            AtomicExprParser<T>(parameter, config),
            CombineClauses<T>
        );

    private static Parser<Expression> OrExprParser<T>(ParameterExpression parameter, IQueryKitConfiguration? config = null)
        => ChainLeft(
            LogicalOperatorParserWithAliases(config).Where(x => x.Name == LogicalOperator.OrOperator.Operator()),
            AndExprParser<T>(parameter, config),
            CombineClauses<T>
        );

    // The same left-associative chain as Parse.ChainOperator, read in a loop. Parse.ChainOperator recurses
    // once for each operator, so a long flat chain such as a && b && ... overflowed the stack.
    // When an operator has no operand after it, the chain ends before that operator, the same as Parse.ChainOperator.
    private static Parser<TResult> ChainLeft<TResult, TOp>(Parser<TOp> op, Parser<TResult> operand, Func<TOp, TResult, TResult, TResult> apply)
        => input =>
        {
            var first = operand(input);
            if (!first.WasSuccessful)
            {
                return first;
            }

            var result = first.Value;
            var remainder = first.Remainder;
            while (true)
            {
                var opResult = op(remainder);
                if (!opResult.WasSuccessful)
                {
                    break;
                }

                var next = operand(opResult.Remainder);
                if (!next.WasSuccessful)
                {
                    break;
                }

                result = apply(opResult.Value, result, next.Value);
                remainder = next.Remainder;
            }

            return Result.Success(result, remainder);
        };

    private static bool RemovesIgnoredClauses(IQueryKitConfiguration? config)
        => ((config as IQueryKitFilterBehavior)?.IgnoredClauseBehavior ?? IgnoredClauseBehavior.Remove)
            == IgnoredClauseBehavior.Remove;

    // A clause on a prevented or unknown property. By default the parser removes it. With ReplaceWithTrue
    // it becomes true == true, the same as v1.14.2.
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
            value.Contains("-") && DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out _) ||
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

    private static Expression CreateCustomOperationExpression<T>(ParameterExpression parameter, QueryKitPropertyInfo customOperationInfo, ComparisonOperator op, string rightValue, bool rightIsQuotedLiteral)
    {
        if (customOperationInfo.CustomOperation == null)
            throw new ArgumentException("Custom operation expression is null");

        // For custom operations, we need to convert the string value to the appropriate basic type
        // instead of trying to match it to the entity type
        // A custom operation gets the list as text, so remove the escapes that EscapeListItem added
        if (rightValue.StartsWith("[") && rightValue.EndsWith("]"))
            rightValue = UnescapeListText(rightValue);

        object? convertedValue = ConvertStringToBasicType(rightValue, rightIsQuotedLiteral);
        
        // Create the parameter expressions for the custom operation
        var entityParameter = Expression.Convert(parameter, typeof(object));
        var operatorParameter = Expression.Constant(op, typeof(ComparisonOperator));
        var valueParameter = Expression.Constant(convertedValue, typeof(object));

        // Invoke the custom operation
        var customOperationLambda = customOperationInfo.CustomOperation;
        var invocationExpression = Expression.Invoke(customOperationLambda, entityParameter, operatorParameter, valueParameter);

        return invocationExpression;
    }

    private static object? ConvertStringToBasicType(string value, bool isQuotedLiteral)
    {
        // A quoted value is text, so it never becomes null, a boolean, or a number.
        // A quoted value in the date format of the grammar or a quoted guid still converts below.
        if (!isQuotedLiteral)
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
        }

        // Try DateTime
        if ((!isQuotedLiteral || DateTimeFormatParser.End().TryParse(value).WasSuccessful) &&
            TryParseDateTime(value, out var dateTimeValue))
            return dateTimeValue;

        // Try Guid
        if (Guid.TryParse(value, out var guidValue))
            return guidValue;

        // Default to string
        return value;
    }
}


