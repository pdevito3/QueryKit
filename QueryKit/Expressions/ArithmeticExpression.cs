namespace QueryKit.Expressions;

using System.Linq.Expressions;
using Operators;

/// <summary>
/// Base class for arithmetic expressions in filtering
/// </summary>
public abstract class ArithmeticExpression
{
    public abstract Expression ToLinqExpression(ParameterExpression parameter, Type entityType);
    public abstract Type GetExpressionType(Type entityType);
}

/// <summary>
/// Represents a binary arithmetic operation (e.g., Price + Tax, Quantity * UnitPrice)
/// </summary>
public class BinaryArithmeticExpression : ArithmeticExpression
{
    public ArithmeticExpression Left { get; }
    public ArithmeticOperator Operator { get; }
    public ArithmeticExpression Right { get; }

    public BinaryArithmeticExpression(ArithmeticExpression left, ArithmeticOperator op, ArithmeticExpression right)
    {
        Left = left;
        Operator = op;
        Right = right;
    }

    public override Expression ToLinqExpression(ParameterExpression parameter, Type entityType)
    {
        var leftExpr = Left.ToLinqExpression(parameter, entityType);
        var rightExpr = Right.ToLinqExpression(parameter, entityType);
        
        return Operator.GetExpression(leftExpr, rightExpr);
    }

    public override Type GetExpressionType(Type entityType)
    {
        var leftType = Left.GetExpressionType(entityType);
        var rightType = Right.GetExpressionType(entityType);
        
        // Use the same logic as the type helper to determine result type
        var leftNonNullable = Nullable.GetUnderlyingType(leftType) ?? leftType;
        var rightNonNullable = Nullable.GetUnderlyingType(rightType) ?? rightType;
        var leftIsNullable = leftType != leftNonNullable;
        var rightIsNullable = rightType != rightNonNullable;

        if (leftNonNullable == rightNonNullable)
        {
            return leftIsNullable || rightIsNullable ? typeof(Nullable<>).MakeGenericType(leftNonNullable) : leftNonNullable;
        }

        if (IsNumericType(leftNonNullable) && IsNumericType(rightNonNullable))
        {
            var widerType = GetWiderNumericType(leftNonNullable, rightNonNullable);
            return leftIsNullable || rightIsNullable ? typeof(Nullable<>).MakeGenericType(widerType) : widerType;
        }

        throw new InvalidOperationException($"Cannot perform arithmetic operation between {leftType.Name} and {rightType.Name}");
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

/// <summary>
/// Represents a property access in an arithmetic expression (e.g., Price, Author.Name)
/// </summary>
public class PropertyArithmeticExpression : ArithmeticExpression
{
    public string PropertyPath { get; }

    public PropertyArithmeticExpression(string propertyPath)
    {
        PropertyPath = propertyPath;
    }

    public override Expression ToLinqExpression(ParameterExpression parameter, Type entityType)
    {
        var propertyNames = PropertyPath.Split('.');
        var expr = (Expression)parameter;
        
        foreach (var propName in propertyNames)
        {
            var propertyInfo = GetPropertyInfo(expr.Type, propName);
            if (propertyInfo == null)
            {
                throw new ArgumentException($"Property '{propName}' not found on type '{expr.Type.Name}'");
            }
            expr = Expression.PropertyOrField(expr, propertyInfo.Name);
        }
        
        return expr;
    }

    public override Type GetExpressionType(Type entityType)
    {
        var propertyNames = PropertyPath.Split('.');
        var currentType = entityType;
        
        foreach (var propName in propertyNames)
        {
            var propertyInfo = GetPropertyInfo(currentType, propName);
            if (propertyInfo == null)
            {
                throw new ArgumentException($"Property '{propName}' not found on type '{currentType.Name}'");
            }
            currentType = propertyInfo.PropertyType;
        }
        
        return currentType;
    }
    
    private static System.Reflection.PropertyInfo? GetPropertyInfo(Type type, string propertyName)
        => type.GetProperty(propertyName, System.Reflection.BindingFlags.IgnoreCase | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
}

/// <summary>
/// Represents a literal value in an arithmetic expression (e.g., 100, 3.14)
/// </summary>
public class LiteralArithmeticExpression : ArithmeticExpression
{
    public object Value { get; }
    public Type ValueType { get; }

    public LiteralArithmeticExpression(object value, Type valueType)
    {
        Value = value;
        ValueType = valueType;
    }

    public override Expression ToLinqExpression(ParameterExpression parameter, Type entityType)
    {
        return Expression.Constant(Value, ValueType);
    }

    public override Type GetExpressionType(Type entityType)
    {
        return ValueType;
    }
}

/// <summary>
/// Represents a parenthesized arithmetic expression for grouping
/// </summary>
public class GroupedArithmeticExpression : ArithmeticExpression
{
    public ArithmeticExpression Inner { get; }

    public GroupedArithmeticExpression(ArithmeticExpression inner)
    {
        Inner = inner;
    }

    public override Expression ToLinqExpression(ParameterExpression parameter, Type entityType)
    {
        return Inner.ToLinqExpression(parameter, entityType);
    }

    public override Type GetExpressionType(Type entityType)
    {
        return Inner.GetExpressionType(entityType);
    }
}