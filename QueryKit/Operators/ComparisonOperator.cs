namespace QueryKit.Operators;

using System.Linq.Expressions;
using Ardalis.SmartEnum;

public abstract class ComparisonOperator : SmartEnum<ComparisonOperator>
{
    public static ComparisonOperator CaseSensitiveEqualsOperator = new EqualsType();
    public static ComparisonOperator CaseSensitiveNotEqualsOperator = new NotEqualsType();
    public static ComparisonOperator CaseSensitiveGreaterThanOperator = new GreaterThanType();
    public static ComparisonOperator CaseSensitiveLessThanOperator = new LessThanType();
    public static ComparisonOperator CaseSensitiveGreaterThanOrEqualOperator = new GreaterThanOrEqualType();
    public static ComparisonOperator CaseSensitiveLessThanOrEqualOperator = new LessThanOrEqualType();
    public static ComparisonOperator CaseSensitiveContainsOperator = new ContainsType();
    public static ComparisonOperator CaseSensitiveStartsWithOperator = new StartsWithType();
    public static ComparisonOperator CaseSensitiveEndsWithOperator = new EndsWithType();
    public static ComparisonOperator CaseSensitiveNotContainsOperator = new NotContainsType();
    public static ComparisonOperator CaseSensitiveNotStartsWithOperator = new NotStartsWithType();
    public static ComparisonOperator CaseSensitiveNotEndsWithOperator = new NotEndsWithType();
    public static ComparisonOperator CaseSensitiveInOperator = new InType();
    
    public static ComparisonOperator EqualsOperator(bool caseInsensitive = false) => new EqualsType(caseInsensitive);
    public static ComparisonOperator NotEqualsOperator(bool caseInsensitive = false) => new NotEqualsType(caseInsensitive);
    public static ComparisonOperator GreaterThanOperator(bool caseInsensitive = false) => new GreaterThanType(caseInsensitive);
    public static ComparisonOperator LessThanOperator(bool caseInsensitive = false) => new LessThanType(caseInsensitive);
    public static ComparisonOperator GreaterThanOrEqualOperator(bool caseInsensitive = false) => new GreaterThanOrEqualType(caseInsensitive);
    public static ComparisonOperator LessThanOrEqualOperator(bool caseInsensitive = false) => new LessThanOrEqualType(caseInsensitive);
    public static ComparisonOperator ContainsOperator(bool caseInsensitive = false) => new ContainsType(caseInsensitive);
    public static ComparisonOperator StartsWithOperator(bool caseInsensitive = false) => new StartsWithType(caseInsensitive);
    public static ComparisonOperator EndsWithOperator(bool caseInsensitive = false) => new EndsWithType(caseInsensitive);
    public static ComparisonOperator NotContainsOperator(bool caseInsensitive = false) => new NotContainsType(caseInsensitive);
    public static ComparisonOperator NotStartsWithOperator(bool caseInsensitive = false) => new NotStartsWithType(caseInsensitive);
    public static ComparisonOperator NotEndsWithOperator(bool caseInsensitive = false) => new NotEndsWithType(caseInsensitive);
    public static ComparisonOperator InOperator(bool caseInsensitive = false) => new InType(caseInsensitive);

    
    public static ComparisonOperator GetByOperatorString(string op, bool caseInsensitive = false)
    {
        var comparisonOperator = List.FirstOrDefault(x => x.Operator() == op);
        if (comparisonOperator == null)
        {
            throw new Exception($"Operator {op} is not supported");
        }
        comparisonOperator.CaseInsensitive = caseInsensitive;
        return comparisonOperator;
    }

    public abstract string Operator();
    public bool CaseInsensitive { get; protected set; }
    public abstract Expression GetExpression<T>(Expression left, Expression right);
    protected ComparisonOperator(string name, int value, bool caseInsensitive = false) : base(name, value)
    {
        CaseInsensitive = caseInsensitive;
    }

    private class EqualsType : ComparisonOperator
    {
        public EqualsType(bool caseInsensitive = false) : base("==", 0, caseInsensitive)
        {
        }

        public override string Operator() => Name;
        public override Expression GetExpression<T>(Expression left, Expression right)
        {
            if (CaseInsensitive && left.Type == typeof(string) && right.Type == typeof(string))
            {
                return Expression.Equal(
                    Expression.Call(left, typeof(string).GetMethod("ToLower", Type.EmptyTypes)),
                    Expression.Call(right, typeof(string).GetMethod("ToLower", Type.EmptyTypes))
                );
            }

            return Expression.Equal(left, right);
        }
    }

    private class NotEqualsType : ComparisonOperator
    {
        public NotEqualsType(bool caseInsensitive = false) : base("!=", 1, caseInsensitive)
        {
        }

        public override string Operator() => Name;
        public override Expression GetExpression<T>(Expression left, Expression right)
        {
            
            if (CaseInsensitive && left.Type == typeof(string) && right.Type == typeof(string))
            {
                return Expression.NotEqual(
                    Expression.Call(left, typeof(string).GetMethod("ToLower", Type.EmptyTypes)),
                    Expression.Call(right, typeof(string).GetMethod("ToLower", Type.EmptyTypes))
                );
            }
            else
            {
                return Expression.NotEqual(left, right);
            }
        }
    }


    private class GreaterThanType : ComparisonOperator
    {
        public GreaterThanType(bool caseInsensitive = false) : base(">", 2, caseInsensitive)
        {
        }

        public override string Operator() => Name;
        public override Expression GetExpression<T>(Expression left, Expression right)
        {
            return Expression.GreaterThan(left, right);
        }
    }

    private class LessThanType : ComparisonOperator
    {
        public LessThanType(bool caseInsensitive = false) : base("<", 3, caseInsensitive)
        {
        }

        public override string Operator() => Name;
        public override Expression GetExpression<T>(Expression left, Expression right)
        {
            return Expression.LessThan(left, right);
        }
    }

    private class GreaterThanOrEqualType : ComparisonOperator
    {
        public override string Operator() => Name;
        public GreaterThanOrEqualType(bool caseInsensitive = false) : base(">=", 4, caseInsensitive)
        {
        }
        public override Expression GetExpression<T>(Expression left, Expression right)
        {
            return Expression.GreaterThanOrEqual(left, right);
        }
    }

    private class LessThanOrEqualType : ComparisonOperator
    {
        public LessThanOrEqualType(bool caseInsensitive = false) : base("<=", 5, caseInsensitive)
        {
        }
        public override string Operator() => Name;
        public override Expression GetExpression<T>(Expression left, Expression right)
        {
            return Expression.LessThanOrEqual(left, right);
        }
    }

    private class ContainsType : ComparisonOperator
    {
        public ContainsType(bool caseInsensitive = false) : base("@=", 6, caseInsensitive)
        {
        }

        public override string Operator() => Name;
        public override Expression GetExpression<T>(Expression left, Expression right)
        {
            if (CaseInsensitive)
            {
                return Expression.Call(
                    Expression.Call(left, "ToLower", null),
                    typeof(string).GetMethod("Contains", new[] { typeof(string) }),
                    Expression.Call(right, "ToLower", null)
                );
            }

            return Expression.Call(left, typeof(string).GetMethod("Contains", new[] { typeof(string) }), right);
        }
    }

    private class StartsWithType : ComparisonOperator
    {
        public StartsWithType(bool caseInsensitive = false) : base("_=", 7, caseInsensitive)
        {
        }

        public override string Operator() => Name;
        public override Expression GetExpression<T>(Expression left, Expression right)
        {
            if (CaseInsensitive)
            {
                return Expression.Call(
                    Expression.Call(left, "ToLower", null),
                    typeof(string).GetMethod("StartsWith", new[] { typeof(string) }),
                    Expression.Call(right, "ToLower", null)
                );
            }
            
            return Expression.Call(left, typeof(string).GetMethod("StartsWith", new[] { typeof(string) }), right);
        }
    }

    private class EndsWithType : ComparisonOperator
    {
        public EndsWithType(bool caseInsensitive = false) : base("_-=", 8, caseInsensitive)
        {
        }

        public override string Operator() => Name;
        public override Expression GetExpression<T>(Expression left, Expression right)
        {
            if (CaseInsensitive)
            {
                return Expression.Call(
                    Expression.Call(left, "ToLower", null),
                    typeof(string).GetMethod("EndsWith", new[] { typeof(string) }),
                    Expression.Call(right, "ToLower", null)
                );
            }
            
            return Expression.Call(left, typeof(string).GetMethod("EndsWith", new[] { typeof(string) }), right);
        }
    }

    private class NotContainsType : ComparisonOperator
    {
        public NotContainsType(bool caseInsensitive = false) : base("!@=", 9, caseInsensitive)
        {
        }

        public override string Operator() => Name;
        public override Expression GetExpression<T>(Expression left, Expression right)
        {
            if(CaseInsensitive)
            {
                return Expression.Not(Expression.Call(
                    Expression.Call(left, "ToLower", null),
                    typeof(string).GetMethod("Contains", new[] { typeof(string) }),
                    Expression.Call(right, "ToLower", null)
                ));
            }
            
            return Expression.Not(Expression.Call(left, typeof(string).GetMethod("Contains", new[] { typeof(string) }), right));
            
        }
    }

    private class NotStartsWithType : ComparisonOperator
    {
        public NotStartsWithType(bool caseInsensitive = false) : base("!_=", 10, caseInsensitive)
        {
        }

        public override string Operator() => Name;
        public override Expression GetExpression<T>(Expression left, Expression right)
        {
            if (CaseInsensitive)
            {
                return Expression.Not(Expression.Call(
                    Expression.Call(left, "ToLower", null),
                    typeof(string).GetMethod("StartsWith", new[] { typeof(string) }),
                    Expression.Call(right, "ToLower", null)
                ));
            }
            
            return Expression.Not(Expression.Call(left, typeof(string).GetMethod("StartsWith", new[] { typeof(string) }), right));
        }
    }

    private class NotEndsWithType : ComparisonOperator
    {
        public NotEndsWithType(bool caseInsensitive = false) : base("!_-=", 11, caseInsensitive)
        {
        }

        public override string Operator() => Name;
        public override Expression GetExpression<T>(Expression left, Expression right)
        {
            if (CaseInsensitive)
            {
                return Expression.Not(Expression.Call(
                    Expression.Call(left, "ToLower", null),
                    typeof(string).GetMethod("EndsWith", new[] { typeof(string) }),
                    Expression.Call(right, "ToLower", null)
                ));
            }
            
            return Expression.Not(Expression.Call(left, typeof(string).GetMethod("EndsWith", new[] { typeof(string) }), right));
        }
    }
    
    private class InType : ComparisonOperator
    {
        public InType(bool caseInsensitive = false) : base("^^", 12, caseInsensitive)
        {
        }

        public override string Operator() => Name;
        public override Expression GetExpression<T>(Expression left, Expression right)
        {
            var leftType = left.Type;
        
            if (right is NewArrayExpression newArrayExpression)
            {
                var listType = typeof(List<>).MakeGenericType(leftType);
                var list = Activator.CreateInstance(listType);

                foreach (var value in newArrayExpression.Expressions)
                {
                    listType.GetMethod("Add").Invoke(list, new[] { ((ConstantExpression)value).Value });
                }

                right = Expression.Constant(list, listType);
            }

            // Get the Contains method with the correct generic type
            var containsMethod = typeof(ICollection<>)
                .MakeGenericType(leftType)
                .GetMethod("Contains");

            if (CaseInsensitive && leftType == typeof(string))
            {
                var listType = typeof(List<string>);
                var toLowerList = Activator.CreateInstance(listType);
            
                var originalList = ((ConstantExpression)right).Value as IEnumerable<string>;
                foreach (var value in originalList)
                {
                    listType.GetMethod("Add").Invoke(toLowerList, new[] { value.ToLower() });
                }
                right = Expression.Constant(toLowerList, listType);
                left = Expression.Call(left, typeof(string).GetMethod("ToLower", Type.EmptyTypes));
            }

            return Expression.Call(right, containsMethod, left);
        }
    }
}
