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
    public static Expression Parameter(object? value, Type type)
    {
        var holderType = typeof(FilterValue<>).MakeGenericType(type);
        var holder = Activator.CreateInstance(holderType, value);
        return Expression.Field(Expression.Constant(holder, holderType), nameof(FilterValue<object>.Value));
    }
}
