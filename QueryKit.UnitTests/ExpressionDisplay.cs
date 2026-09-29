namespace QueryKit.UnitTests;

using System.Linq.Expressions;

// QueryKit sends filter values as FilterValue<T> field reads so that EF Core makes SQL parameters.
// ToString() prints these reads as value(QueryKit.FilterValue`1[...]).Value, so the tests put the
// values back inline. Date and time values print as constructor calls, the same as before.
public static class ExpressionDisplay
{
    public static string ToDisplayString(this Expression expression)
        => new InlineFilterValues().Visit(expression)!.ToString();

    private sealed class InlineFilterValues : ExpressionVisitor
    {
        protected override Expression VisitMember(MemberExpression node)
        {
            if (node.Expression is not ConstantExpression { Value: { } holder }
                || !holder.GetType().IsGenericType
                || holder.GetType().GetGenericTypeDefinition() != typeof(FilterValue<>))
                return base.VisitMember(node);

            var value = ((System.Reflection.FieldInfo)node.Member).GetValue(holder);
            return Inline(value, node.Type);
        }

        private static Expression Inline(object? value, Type type)
        {
            var underlying = Nullable.GetUnderlyingType(type);
            var isDateOrTime = value is DateTime or DateTimeOffset or DateOnly or TimeOnly;
            if (!isDateOrTime)
                return Expression.Constant(value, type);

            Expression newExpr = value switch
            {
                DateTime dt => New<DateTime>(new[] { typeof(long), typeof(DateTimeKind) }, dt.Ticks, dt.Kind),
                DateTimeOffset dto => New<DateTimeOffset>(new[] { typeof(long), typeof(TimeSpan) }, dto.Ticks, dto.Offset),
                DateOnly date => New<DateOnly>(new[] { typeof(int), typeof(int), typeof(int) }, date.Year, date.Month, date.Day),
                TimeOnly time => New<TimeOnly>(new[] { typeof(int), typeof(int), typeof(int), typeof(int), typeof(int) },
                    time.Hour, time.Minute, time.Second, time.Millisecond, time.Microsecond),
                _ => throw new InvalidOperationException()
            };

            return underlying == null
                ? newExpr
                : Expression.New(type.GetConstructor(new[] { underlying })!, newExpr);
        }

        private static NewExpression New<T>(Type[] parameterTypes, params object[] args)
            => Expression.New(typeof(T).GetConstructor(parameterTypes)!, args.Select(Expression.Constant));
    }
}
