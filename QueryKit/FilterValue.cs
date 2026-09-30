namespace QueryKit;

using System.Linq.Expressions;

// Holds one filter value for an expression tree. EF Core sends a field read on a captured object
// to the database as a parameter (@p), the same as a C# closure variable. A constant goes into the
// SQL as a literal, so each new value would compile a new query and a new database plan.
internal sealed class FilterValue<T>
{
    public readonly T Value;

    public FilterValue(T value)
    {
        Value = value;
    }
}

internal static class FilterValue
{
    // The parser sets this for one parse when ParameterizeFilterValues is on. Parsing is synchronous,
    // so the value belongs to the thread that parses.
    [ThreadStatic] private static bool _parameterize;

    public static bool Parameterize
    {
        get => _parameterize;
        set => _parameterize = value;
    }

    // Returns a field read on a FilterValue<T> holder when parameters are on. Otherwise returns the
    // same literal expression as v1.14.2.
    public static Expression Create(object? value, Type type)
    {
        if (!_parameterize)
        {
            return Literal(value, type);
        }

        var holderType = typeof(FilterValue<>).MakeGenericType(type);
        var holder = Activator.CreateInstance(holderType, value);
        return Expression.Field(Expression.Constant(holder, holderType), nameof(FilterValue<object>.Value));
    }

    // Returns an in-list for Contains. When parameters are on, the list is one parameter. Otherwise it
    // is a constant that EF Core compares item by item, and the SQL is a literal IN list.
    public static Expression CreateList(object list, Type elementType)
    {
        var listType = typeof(List<>).MakeGenericType(elementType);
        if (_parameterize)
        {
            return Create(list, listType);
        }

        var valuesType = typeof(InListValues<>).MakeGenericType(elementType);
        return Expression.Constant(Activator.CreateInstance(valuesType, list), valuesType);
    }

    // Dates and times are constructor calls, and a nullable enum wraps its constant in a Nullable<T>
    // constructor. Every other value is a constant.
    private static Expression Literal(object? value, Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type);
        var valueType = underlying ?? type;

        Expression? created = value switch
        {
            DateTime dt when valueType == typeof(DateTime)
                => New(valueType, new[] { typeof(long), typeof(DateTimeKind) }, dt.Ticks, dt.Kind),
            DateTimeOffset dto when valueType == typeof(DateTimeOffset)
                => New(valueType, new[] { typeof(long), typeof(TimeSpan) }, dto.Ticks, dto.Offset),
            DateOnly date when valueType == typeof(DateOnly)
                => New(valueType, new[] { typeof(int), typeof(int), typeof(int) }, date.Year, date.Month, date.Day),
            TimeOnly time when valueType == typeof(TimeOnly) => NewTimeOnly(time),
            not null when underlying is { IsEnum: true } => Expression.Constant(value, underlying),
            _ => null
        };

        if (created == null)
        {
            return Expression.Constant(value, type);
        }

        return underlying == null
            ? created
            : Expression.New(type.GetConstructor(new[] { underlying })!, created);
    }

    // The TimeOnly constructor with microseconds needs .NET 7. Without it, the value is a constant.
    private static Expression? NewTimeOnly(TimeOnly time)
    {
        var ctor = typeof(TimeOnly).GetConstructor(new[] { typeof(int), typeof(int), typeof(int), typeof(int), typeof(int) });
        if (ctor == null)
        {
            return null;
        }

        var fractionalTicks = time.Ticks % TimeSpan.TicksPerSecond;
        var millisecond = (int)(fractionalTicks / TimeSpan.TicksPerMillisecond);
        var microsecond = (int)(fractionalTicks % TimeSpan.TicksPerMillisecond / 10);
        return Expression.New(ctor, new object[] { time.Hour, time.Minute, time.Second, millisecond, microsecond }
            .Select(arg => Expression.Constant(arg)));
    }

    private static NewExpression New(Type type, Type[] parameterTypes, params object[] args)
        => Expression.New(type.GetConstructor(parameterTypes)!, args.Select(arg => Expression.Constant(arg)));
}
