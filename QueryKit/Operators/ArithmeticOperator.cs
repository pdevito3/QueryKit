namespace QueryKit.Operators;

using System.Linq.Expressions;

/// <summary>
/// Represents arithmetic operators for mathematical expressions in filters
/// </summary>
public abstract class ArithmeticOperator
{
    public string Symbol { get; }
    public int Precedence { get; }
    
    protected ArithmeticOperator(string symbol, int precedence)
    {
        Symbol = symbol;
        Precedence = precedence;
    }
    
    public abstract Expression GetExpression(Expression left, Expression right);
    
    public static ArithmeticOperator Add => new AddOperator();
    public static ArithmeticOperator Subtract => new SubtractOperator();
    public static ArithmeticOperator Multiply => new MultiplyOperator();
    public static ArithmeticOperator Divide => new DivideOperator();
    public static ArithmeticOperator Modulo => new ModuloOperator();
    
    public static ArithmeticOperator? FromSymbol(string symbol) => symbol switch
    {
        "+" => Add,
        "-" => Subtract,
        "*" => Multiply,
        "/" => Divide,
        "%" => Modulo,
        _ => null
    };
}

internal class AddOperator : ArithmeticOperator
{
    public AddOperator() : base("+", 1) { }
    
    public override Expression GetExpression(Expression left, Expression right)
    {
        var (leftCompatible, rightCompatible) = ArithmeticTypeHelper.EnsureCompatibleNumericTypes(left, right);
        return Expression.Add(leftCompatible, rightCompatible);
    }
}

internal class SubtractOperator : ArithmeticOperator
{
    public SubtractOperator() : base("-", 1) { }
    
    public override Expression GetExpression(Expression left, Expression right)
    {
        var (leftCompatible, rightCompatible) = ArithmeticTypeHelper.EnsureCompatibleNumericTypes(left, right);
        return Expression.Subtract(leftCompatible, rightCompatible);
    }
}

internal class MultiplyOperator : ArithmeticOperator
{
    public MultiplyOperator() : base("*", 2) { }
    
    public override Expression GetExpression(Expression left, Expression right)
    {
        var (leftCompatible, rightCompatible) = ArithmeticTypeHelper.EnsureCompatibleNumericTypes(left, right);
        return Expression.Multiply(leftCompatible, rightCompatible);
    }
}

internal class DivideOperator : ArithmeticOperator
{
    public DivideOperator() : base("/", 2) { }
    
    public override Expression GetExpression(Expression left, Expression right)
    {
        var (leftCompatible, rightCompatible) = ArithmeticTypeHelper.EnsureCompatibleNumericTypes(left, right);
        return Expression.Divide(leftCompatible, rightCompatible);
    }
}

internal class ModuloOperator : ArithmeticOperator
{
    public ModuloOperator() : base("%", 2) { }
    
    public override Expression GetExpression(Expression left, Expression right)
    {
        var (leftCompatible, rightCompatible) = ArithmeticTypeHelper.EnsureCompatibleNumericTypes(left, right);
        return Expression.Modulo(leftCompatible, rightCompatible);
    }
}

internal static class ArithmeticTypeHelper
{
    public static (Expression left, Expression right) EnsureCompatibleNumericTypes(Expression left, Expression right)
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
            // Even if the underlying types are the same, we need to ensure both expressions
            // have the same type (both nullable or both non-nullable)
            var shouldBeNullable = leftIsNullable || rightIsNullable;
            var targetType = shouldBeNullable ? typeof(Nullable<>).MakeGenericType(leftNonNullable) : leftNonNullable;
            
            if (left.Type != targetType)
            {
                left = Expression.Convert(left, targetType);
            }
            if (right.Type != targetType)
            {
                right = Expression.Convert(right, targetType);
            }
            
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
}