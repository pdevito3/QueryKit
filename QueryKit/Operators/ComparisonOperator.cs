namespace QueryKit.Operators;

using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using Ardalis.SmartEnum;
using Configuration;
using Exceptions;

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
    public static ComparisonOperator CaseSensitiveNotInOperator = new NotInType();
    public static ComparisonOperator CaseSensitiveSoundsLikeOperator = new SoundsLikeType();
    public static ComparisonOperator CaseSensitiveDoesNotSoundLikeOperator = new DoesNotSoundLikeType();
    public static ComparisonOperator CaseSensitiveHasCountEqualToOperator = new HasCountEqualToType();
    public static ComparisonOperator CaseSensitiveHasCountNotEqualToOperator = new HasCountNotEqualToType();
    public static ComparisonOperator CaseSensitiveHasCountGreaterThanOperator = new HasCountGreaterThanType();
    public static ComparisonOperator CaseSensitiveHasCountLessThanOperator = new HasCountLessThanType();
    public static ComparisonOperator CaseSensitiveHasCountGreaterThanOrEqualOperator = new HasCountGreaterThanOrEqualType();
    public static ComparisonOperator CaseSensitiveHasCountLessThanOrEqualOperator = new HasCountLessThanOrEqualType();
    public static ComparisonOperator CaseSensitiveHasOperator = new HasType();
    public static ComparisonOperator CaseSensitiveDoesNotHaveOperator = new DoesNotHaveType();
    
    public static ComparisonOperator EqualsOperator(bool caseInsensitive = false, bool usesAll = false) => new EqualsType(caseInsensitive);
    public static ComparisonOperator NotEqualsOperator(bool caseInsensitive = false, bool usesAll = false) => new NotEqualsType(caseInsensitive);
    public static ComparisonOperator GreaterThanOperator(bool caseInsensitive = false, bool usesAll = false) => new GreaterThanType(caseInsensitive);
    public static ComparisonOperator LessThanOperator(bool caseInsensitive = false, bool usesAll = false) => new LessThanType(caseInsensitive);
    public static ComparisonOperator GreaterThanOrEqualOperator(bool caseInsensitive = false, bool usesAll = false) => new GreaterThanOrEqualType(caseInsensitive);
    public static ComparisonOperator LessThanOrEqualOperator(bool caseInsensitive = false, bool usesAll = false) => new LessThanOrEqualType(caseInsensitive);
    public static ComparisonOperator ContainsOperator(bool caseInsensitive = false, bool usesAll = false) => new ContainsType(caseInsensitive);
    public static ComparisonOperator StartsWithOperator(bool caseInsensitive = false, bool usesAll = false) => new StartsWithType(caseInsensitive);
    public static ComparisonOperator EndsWithOperator(bool caseInsensitive = false, bool usesAll = false) => new EndsWithType(caseInsensitive);
    public static ComparisonOperator NotContainsOperator(bool caseInsensitive = false, bool usesAll = false) => new NotContainsType(caseInsensitive);
    public static ComparisonOperator NotStartsWithOperator(bool caseInsensitive = false, bool usesAll = false) => new NotStartsWithType(caseInsensitive);
    public static ComparisonOperator NotEndsWithOperator(bool caseInsensitive = false, bool usesAll = false) => new NotEndsWithType(caseInsensitive);
    public static ComparisonOperator InOperator(bool caseInsensitive = false, bool usesAll = false) => new InType(caseInsensitive);
    public static ComparisonOperator NotInOperator(bool caseInsensitive = false, bool usesAll = false) => new NotInType(caseInsensitive);
    public static ComparisonOperator SoundsLikeOperator(bool caseInsensitive = false, bool usesAll = false) => new SoundsLikeType(caseInsensitive);
    public static ComparisonOperator DoesNotSoundLikeOperator(bool caseInsensitive = false, bool usesAll = false) => new DoesNotSoundLikeType(caseInsensitive);
    public static ComparisonOperator HasCountEqualToOperator(bool caseInsensitive = false, bool usesAll = false) => new HasCountEqualToType(caseInsensitive);
    public static ComparisonOperator HasCountNotEqualToOperator(bool caseInsensitive = false, bool usesAll = false) => new HasCountNotEqualToType(caseInsensitive);
    public static ComparisonOperator HasCountGreaterThanOperator(bool caseInsensitive = false, bool usesAll = false) => new HasCountGreaterThanType(caseInsensitive);
    public static ComparisonOperator HasCountLessThanOperator(bool caseInsensitive = false, bool usesAll = false) => new HasCountLessThanType(caseInsensitive);
    public static ComparisonOperator HasCountGreaterThanOrEqualOperator(bool caseInsensitive = false, bool usesAll = false) => new HasCountGreaterThanOrEqualType(caseInsensitive);
    public static ComparisonOperator HasCountLessThanOrEqualOperator(bool caseInsensitive = false, bool usesAll = false) => new HasCountLessThanOrEqualType(caseInsensitive);
    public static ComparisonOperator HasOperator(bool caseInsensitive = false, bool usesAll = false) => new HasType(caseInsensitive);
    public static ComparisonOperator DoesNotHaveOperator(bool caseInsensitive = false, bool usesAll = false) => new DoesNotHaveType(caseInsensitive);
    
    public static ComparisonOperator GetByOperatorString(string op, bool caseInsensitive = false, bool usesAll = false)
    {
        var comparisonOperator = List.FirstOrDefault(x => x.Operator() == op);
        if (comparisonOperator == null)
        {
            throw new QueryKitParsingException($"Operator {op} is not supported");
        }

        ComparisonOperator? newOperator = null;

        if (comparisonOperator is EqualsType)
        {
            newOperator = new EqualsType(caseInsensitive, usesAll);
        }
        if (comparisonOperator is NotEqualsType)
        {
            newOperator = new NotEqualsType(caseInsensitive, usesAll);
        }
        if (comparisonOperator is GreaterThanType)
        {
            newOperator = new GreaterThanType(caseInsensitive, usesAll);
        }
        if (comparisonOperator is LessThanType)
        {
            newOperator = new LessThanType(caseInsensitive, usesAll);
        }
        if (comparisonOperator is GreaterThanOrEqualType)
        {
            newOperator = new GreaterThanOrEqualType(caseInsensitive, usesAll);
        }
        if (comparisonOperator is LessThanOrEqualType)
        {
            newOperator = new LessThanOrEqualType(caseInsensitive, usesAll);
        }
        if (comparisonOperator is ContainsType)
        {
            newOperator = new ContainsType(caseInsensitive, usesAll);
        }
        if (comparisonOperator is StartsWithType)
        {
            newOperator = new StartsWithType(caseInsensitive, usesAll);
        }
        if (comparisonOperator is EndsWithType)
        {
            newOperator = new EndsWithType(caseInsensitive, usesAll);
        }
        if (comparisonOperator is NotContainsType)
        {
            newOperator = new NotContainsType(caseInsensitive, usesAll);
        }
        if (comparisonOperator is NotStartsWithType)
        {
            newOperator = new NotStartsWithType(caseInsensitive, usesAll);
        }
        if (comparisonOperator is NotEndsWithType)
        {
            newOperator = new NotEndsWithType(caseInsensitive, usesAll);
        }
        if (comparisonOperator is InType)
        {
            newOperator = new InType(caseInsensitive, usesAll);
        }
        if (comparisonOperator is NotInType)
        {
            newOperator = new NotInType(caseInsensitive, usesAll);
        }
        if (comparisonOperator is SoundsLikeType)
        {
            newOperator = new SoundsLikeType(caseInsensitive, usesAll);
        }
        if (comparisonOperator is DoesNotSoundLikeType)
        {
            newOperator = new DoesNotSoundLikeType(caseInsensitive, usesAll);
        }
        if (comparisonOperator is HasCountEqualToType)
        {
            newOperator = new HasCountEqualToType(caseInsensitive, usesAll);
        }
        if (comparisonOperator is HasCountNotEqualToType)
        {
            newOperator = new HasCountNotEqualToType(caseInsensitive, usesAll);
        }
        if (comparisonOperator is HasCountGreaterThanType)
        {
            newOperator = new HasCountGreaterThanType(caseInsensitive, usesAll);
        }
        if (comparisonOperator is HasCountLessThanType)
        {
            newOperator = new HasCountLessThanType(caseInsensitive, usesAll);
        }
        if (comparisonOperator is HasCountGreaterThanOrEqualType)
        {
            newOperator = new HasCountGreaterThanOrEqualType(caseInsensitive, usesAll);
        }
        if (comparisonOperator is HasCountLessThanOrEqualType)
        {
            newOperator = new HasCountLessThanOrEqualType(caseInsensitive, usesAll);
        }
        if (comparisonOperator is HasType)
        {
            newOperator = new HasType(caseInsensitive, usesAll);
        }
        if (comparisonOperator is DoesNotHaveType)
        {
            newOperator = new DoesNotHaveType(caseInsensitive, usesAll);
        }
        
        return newOperator == null 
            ? throw new QueryKitParsingException($"Operator {op} is not supported")
            : newOperator!;
    }

    public const char CaseSensitiveAppendix = '*';
    public const char AllPrefix = '%';
    public abstract string Operator();
    public abstract bool IsCountOperator();
    public bool CaseInsensitive { get; protected set; }
    public bool UsesAll { get; protected set; }
    public abstract Expression GetExpression<T>(Expression left, Expression right, Type? dbContextType);

    /// <summary>
    /// Returns true if this operator requires string comparison (e.g., Contains, StartsWith, EndsWith).
    /// This is used to determine whether GUIDs should be converted to strings for comparison.
    /// </summary>
    public bool IsStringComparisonOperator()
    {
        return this is ContainsType or NotContainsType or StartsWithType or NotStartsWithType
            or EndsWithType or NotEndsWithType or SoundsLikeType or DoesNotSoundLikeType;
    }

    protected ComparisonOperator(string name, int value, bool caseInsensitive = false, bool usesAll = false) : base(name, value)
    {
        CaseInsensitive = caseInsensitive;
        UsesAll = usesAll;
    }

    private class EqualsType : ComparisonOperator
    {
        public EqualsType(bool caseInsensitive = false, bool usesAll = false) : base("==", 0, caseInsensitive, usesAll)
        {
        }

        public override string Operator() => CaseInsensitive ? $"{Name}{CaseSensitiveAppendix}" : Name;
        public override bool IsCountOperator() => false; 
        public override Expression GetExpression<T>(Expression left, Expression right, Type? dbContextType)
        {
            if (left.Type.IsGenericType && left.Type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                return GetCollectionExpression(left, right, Expression.Equal, UsesAll);
            }
            
            if (CaseInsensitive && left.Type == typeof(string) && right.Type == typeof(string))
            {
                // null != any non-null value, so we need: left != null && left.ToLower() == right.ToLower()
                var nullCheck = Expression.NotEqual(left, Expression.Constant(null, typeof(string)));
                var toLowerComparison = Expression.Equal(
                    Expression.Call(left, typeof(string).GetMethod("ToLower", Type.EmptyTypes)!),
                    Expression.Call(right, typeof(string).GetMethod("ToLower", Type.EmptyTypes)!)
                );
                return Expression.AndAlso(nullCheck, toLowerComparison);
            }

            // for some complex derived expressions
            if (left.NodeType == ExpressionType.Convert)
            {
                left = Expression.Convert(left, typeof(bool));
            }

            // Ensure type compatibility for comparisons
            var (leftCompatible, rightCompatible) = EnsureCompatibleExpressionTypes(left, right);
            return Expression.Equal(leftCompatible, rightCompatible);
        }
    }

    private class NotEqualsType : ComparisonOperator
    {
        public NotEqualsType(bool caseInsensitive = false, bool usesAll = false) : base("!=", 1, caseInsensitive, usesAll)
        {
        }

        public override string Operator() => CaseInsensitive ? $"{Name}{CaseSensitiveAppendix}" : Name;
        public override bool IsCountOperator() => false; 
        public override Expression GetExpression<T>(Expression left, Expression right, Type? dbContextType)
        {
            if (left.Type.IsGenericType && left.Type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                return GetCollectionExpression(left, right, Expression.NotEqual, UsesAll);
            }
    
            if (CaseInsensitive && left.Type == typeof(string) && right.Type == typeof(string))
            {
                // null != any non-null value, so we need: left == null || left.ToLower() != right.ToLower()
                var nullCheck = Expression.Equal(left, Expression.Constant(null, typeof(string)));
                var toLowerComparison = Expression.NotEqual(
                    Expression.Call(left, typeof(string).GetMethod("ToLower", Type.EmptyTypes)!),
                    Expression.Call(right, typeof(string).GetMethod("ToLower", Type.EmptyTypes)!)
                );
                return Expression.OrElse(nullCheck, toLowerComparison);
            }

            // for some complex derived expressions
            if (left.NodeType == ExpressionType.Convert)
            {
                left = Expression.Convert(left, typeof(bool));
            }

            // Ensure type compatibility for comparisons
            var (leftCompatible, rightCompatible) = EnsureCompatibleExpressionTypes(left, right);
            return Expression.NotEqual(leftCompatible, rightCompatible);
        }
    }

    private class GreaterThanType : ComparisonOperator
    {
        public GreaterThanType(bool caseInsensitive = false, bool usesAll = false) : base(">", 2, caseInsensitive, usesAll)
        {
        }

        public override string Operator() => Name;
        public override bool IsCountOperator() => false; 
        public override Expression GetExpression<T>(Expression left, Expression right, Type? dbContextType)
        {
            if (left.Type.IsGenericType && left.Type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                return GetCollectionExpression(left, right, Expression.GreaterThan, UsesAll);
            }
            
            // Ensure type compatibility for comparisons
            var (leftCompatible, rightCompatible) = EnsureCompatibleExpressionTypes(left, right);
            return Expression.GreaterThan(leftCompatible, rightCompatible);
        }
    }

    private class LessThanType : ComparisonOperator
    {
        public LessThanType(bool caseInsensitive = false, bool usesAll = false) : base("<", 3, caseInsensitive, usesAll)
        {
        }

        public override string Operator() => Name;
        public override bool IsCountOperator() => false; 
        public override Expression GetExpression<T>(Expression left, Expression right, Type? dbContextType)
        {
            if (left.Type.IsGenericType && left.Type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                return GetCollectionExpression(left, right, Expression.LessThan, UsesAll);
            }
            
            // Ensure type compatibility for comparisons
            var (leftCompatible, rightCompatible) = EnsureCompatibleExpressionTypes(left, right);
            return Expression.LessThan(leftCompatible, rightCompatible);
        }
    }

    private class GreaterThanOrEqualType : ComparisonOperator
    {
        public override string Operator() => Name;
        public override bool IsCountOperator() => false; 
        public GreaterThanOrEqualType(bool caseInsensitive = false, bool usesAll = false) : base(">=", 4, caseInsensitive, usesAll)
        {
        }
        public override Expression GetExpression<T>(Expression left, Expression right, Type? dbContextType)
        {
            if (left.Type.IsGenericType && left.Type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                return GetCollectionExpression(left, right, Expression.GreaterThanOrEqual, UsesAll);
            }
            
            // Ensure type compatibility for comparisons
            var (leftCompatible, rightCompatible) = EnsureCompatibleExpressionTypes(left, right);
            return Expression.GreaterThanOrEqual(leftCompatible, rightCompatible);
        }
    }

    private class LessThanOrEqualType : ComparisonOperator
    {
        public LessThanOrEqualType(bool caseInsensitive = false, bool usesAll = false) : base("<=", 5, caseInsensitive, usesAll)
        {
        }
        public override string Operator() => Name;
        public override bool IsCountOperator() => false; 
        public override Expression GetExpression<T>(Expression left, Expression right, Type? dbContextType)
        {
            if (left.Type.IsGenericType && left.Type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                return GetCollectionExpression(left, right, Expression.LessThanOrEqual, UsesAll);
            }
            
            // Ensure type compatibility for comparisons
            var (leftCompatible, rightCompatible) = EnsureCompatibleExpressionTypes(left, right);
            return Expression.LessThanOrEqual(leftCompatible, rightCompatible);
        }
    }

    private class ContainsType : ComparisonOperator
    {
        public ContainsType(bool caseInsensitive = false, bool usesAll = false) : base("@=", 6, caseInsensitive, usesAll)
        {
        }

        public override string Operator() => CaseInsensitive ? $"{Name}{CaseSensitiveAppendix}" : Name;
        public override bool IsCountOperator() => false; 
        public override Expression GetExpression<T>(Expression left, Expression right, Type? dbContextType)
        {
            if (left.Type.IsGenericType && left.Type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                return GetCollectionExpression(left, right, "Contains", false, UsesAll);
            }
    
            if (CaseInsensitive && left.Type == typeof(string) && right.Type == typeof(string))
            {
                // null doesn't contain anything, so we need: left != null && left.ToLower().Contains(right.ToLower())
                var nullCheck = Expression.NotEqual(left, Expression.Constant(null, typeof(string)));
                var containsCall = Expression.Call(
                    Expression.Call(left, "ToLower", null),
                    typeof(string).GetMethod("Contains", new[] { typeof(string) })!,
                    Expression.Call(right, "ToLower", null)
                );
                return Expression.AndAlso(nullCheck, containsCall);
            }

            return Expression.Call(left, typeof(string).GetMethod("Contains", new[] { typeof(string) })!, right);
        }
    }

    private class StartsWithType : ComparisonOperator
    {
        public StartsWithType(bool caseInsensitive = false, bool usesAll = false) : base("_=", 7, caseInsensitive, usesAll)
        {
        }

        public override string Operator() => CaseInsensitive ? $"{Name}{CaseSensitiveAppendix}" : Name;
        public override bool IsCountOperator() => false; 
        public override Expression GetExpression<T>(Expression left, Expression right, Type? dbContextType)
        {
            if (left.Type.IsGenericType && left.Type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                return GetCollectionExpression(left, right, "StartsWith", false, UsesAll);
            }
        
            if (CaseInsensitive && left.Type == typeof(string) && right.Type == typeof(string))
            {
                // null doesn't start with anything, so we need: left != null && left.ToLower().StartsWith(right.ToLower())
                var nullCheck = Expression.NotEqual(left, Expression.Constant(null, typeof(string)));
                var startsWithCall = Expression.Call(
                    Expression.Call(left, "ToLower", null),
                    typeof(string).GetMethod("StartsWith", new[] { typeof(string) })!,
                    Expression.Call(right, "ToLower", null)
                );
                return Expression.AndAlso(nullCheck, startsWithCall);
            }

            return Expression.Call(left, typeof(string).GetMethod("StartsWith", new[] { typeof(string) })!, right);
        }
    }

    private class EndsWithType : ComparisonOperator
    {
        public EndsWithType(bool caseInsensitive = false, bool usesAll = false) : base("_-=", 8, caseInsensitive, usesAll)
        {
        }

        public override string Operator() => CaseInsensitive ? $"{Name}{CaseSensitiveAppendix}" : Name;
        public override bool IsCountOperator() => false; 
        public override Expression GetExpression<T>(Expression left, Expression right, Type? dbContextType)
        {
            if (left.Type.IsGenericType && left.Type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                return GetCollectionExpression(left, right, "EndsWith", false, UsesAll);
            }
            
            if (CaseInsensitive)
            {
                // null doesn't end with anything, so we need: left != null && left.ToLower().EndsWith(right.ToLower())
                var nullCheck = Expression.NotEqual(left, Expression.Constant(null, typeof(string)));
                var endsWithCall = Expression.Call(
                    Expression.Call(left, "ToLower", null),
                    typeof(string).GetMethod("EndsWith", new[] { typeof(string) })!,
                    Expression.Call(right, "ToLower", null)
                );
                return Expression.AndAlso(nullCheck, endsWithCall);
            }

            return Expression.Call(left, typeof(string).GetMethod("EndsWith", new[] { typeof(string) })!, right);
        }
    }

    private class NotContainsType : ComparisonOperator
    {
        public NotContainsType(bool caseInsensitive = false, bool usesAll = false) : base("!@=", 9, caseInsensitive, usesAll)
        {
        }

        public override string Operator() => CaseInsensitive ? $"{Name}{CaseSensitiveAppendix}" : Name;
        public override bool IsCountOperator() => false; 
        public override Expression GetExpression<T>(Expression left, Expression right, Type? dbContextType)
        {
            if (left.Type.IsGenericType && left.Type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                return GetCollectionExpression(left, right, "Contains", true, UsesAll);
            }
            
            if(CaseInsensitive)
            {
                // null doesn't contain anything, so it should be included: left == null || !left.ToLower().Contains(right.ToLower())
                var nullCheck = Expression.Equal(left, Expression.Constant(null, typeof(string)));
                var notContainsCall = Expression.Not(Expression.Call(
                    Expression.Call(left, "ToLower", null),
                    typeof(string).GetMethod("Contains", new[] { typeof(string) })!,
                    Expression.Call(right, "ToLower", null)
                ));
                return Expression.OrElse(nullCheck, notContainsCall);
            }

            return Expression.Not(Expression.Call(left, typeof(string).GetMethod("Contains", new[] { typeof(string) })!, right));
        }
    }

    private class NotStartsWithType : ComparisonOperator
    {
        public NotStartsWithType(bool caseInsensitive = false, bool usesAll = false) : base("!_=", 10, caseInsensitive, usesAll)
        {
        }

        public override string Operator() => CaseInsensitive ? $"{Name}{CaseSensitiveAppendix}" : Name;
        public override bool IsCountOperator() => false; 
        public override Expression GetExpression<T>(Expression left, Expression right, Type? dbContextType)
        {
            if (left.Type.IsGenericType && left.Type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                return GetCollectionExpression(left, right, "StartsWith", true, UsesAll);
            }
            
            if (CaseInsensitive)
            {
                // null doesn't start with anything, so it should be included: left == null || !left.ToLower().StartsWith(right.ToLower())
                var nullCheck = Expression.Equal(left, Expression.Constant(null, typeof(string)));
                var notStartsWithCall = Expression.Not(Expression.Call(
                    Expression.Call(left, "ToLower", null),
                    typeof(string).GetMethod("StartsWith", new[] { typeof(string) })!,
                    Expression.Call(right, "ToLower", null)
                ));
                return Expression.OrElse(nullCheck, notStartsWithCall);
            }

            return Expression.Not(Expression.Call(left, typeof(string).GetMethod("StartsWith", new[] { typeof(string) })!, right));
        }
    }

    private class NotEndsWithType : ComparisonOperator
    {
        public NotEndsWithType(bool caseInsensitive = false, bool usesAll = false) : base("!_-=", 11, caseInsensitive, usesAll)
        {
        }

        public override string Operator() => CaseInsensitive ? $"{Name}{CaseSensitiveAppendix}" : Name;
        public override bool IsCountOperator() => false; 
        public override Expression GetExpression<T>(Expression left, Expression right, Type? dbContextType)
        {
            if (left.Type.IsGenericType && left.Type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                return GetCollectionExpression(left, right, "EndsWith", true, UsesAll);
            }
            
            if (CaseInsensitive)
            {
                // null doesn't end with anything, so it should be included: left == null || !left.ToLower().EndsWith(right.ToLower())
                var nullCheck = Expression.Equal(left, Expression.Constant(null, typeof(string)));
                var notEndsWithCall = Expression.Not(Expression.Call(
                    Expression.Call(left, "ToLower", null),
                    typeof(string).GetMethod("EndsWith", new[] { typeof(string) })!,
                    Expression.Call(right, "ToLower", null)
                ));
                return Expression.OrElse(nullCheck, notEndsWithCall);
            }

            return Expression.Not(Expression.Call(left, typeof(string).GetMethod("EndsWith", new[] { typeof(string) })!, right));
        }
    }

    private class InType : ComparisonOperator
    {
        public InType(bool caseInsensitive = false, bool usesAll = false) : base("^^", 12, caseInsensitive, usesAll)
        {
        }

        public override string Operator() => CaseInsensitive ? $"{Name}{CaseSensitiveAppendix}" : Name;
        public override bool IsCountOperator() => false; 
        public override Expression GetExpression<T>(Expression left, Expression right, Type? dbContextType)
        {
            var leftType = left.Type;

            if (right is NewArrayExpression newArrayExpression)
            {
                var listType = typeof(List<>).MakeGenericType(leftType);
                var list = Activator.CreateInstance(listType);

                foreach (var value in newArrayExpression.Expressions)
                {
                    listType.GetMethod("Add")!.Invoke(list, new[] { ((ConstantExpression)value).Value });
                }

                right = Expression.Constant(list, listType);
            }

            // Get the Contains method with the correct generic type
            var containsMethod = typeof(ICollection<>)
                .MakeGenericType(leftType)
                .GetMethod("Contains")!;

            if (CaseInsensitive && leftType == typeof(string))
            {
                // null is not in any list, so: left != null && list.Contains(left.ToLower())
                var nullCheck = Expression.NotEqual(left, Expression.Constant(null, typeof(string)));

                var listType = typeof(List<string>);
                var toLowerList = Activator.CreateInstance(listType);

                var originalList = ((ConstantExpression)right).Value as IEnumerable<string>;
                foreach (var value in originalList!)
                {
                    listType.GetMethod("Add")!.Invoke(toLowerList, new[] { value.ToLower() });
                }
                right = Expression.Constant(toLowerList, listType);
                var toLowerLeft = Expression.Call(left, typeof(string).GetMethod("ToLower", Type.EmptyTypes)!);

                var containsCall = Expression.Call(right, containsMethod, toLowerLeft);
                return Expression.AndAlso(nullCheck, containsCall);
            }

            return Expression.Call(right, containsMethod, left);
        }
    }

    private class SoundsLikeType : ComparisonOperator
    {
        public SoundsLikeType(bool caseInsensitive = false, bool usesAll = false) : base("~~", 13, caseInsensitive, usesAll)
        {
        }

        public override string Operator() => Name;
        public override bool IsCountOperator() => false; 

        public override Expression GetExpression<T>(Expression left, Expression right, Type? dbContextType)
        {
            if (dbContextType == null)
            {
                throw new QueryKitDbContextTypeException("DbContext type must be provided to use the SoundsLike operator.");
            }
            
            var method = dbContextType.GetMethod("SoundsLike", new Type[] { typeof(string) });

            if (method == null)
            {
                throw new SoundsLikeNotImplementedException(dbContextType.FullName!);
            }
            
            Expression leftMethodCall = Expression.Call(null, method, left);
            Expression rightMethodCall = Expression.Call(null, method, right);
            return Expression.Equal(leftMethodCall, rightMethodCall);
        }
    }

    private class DoesNotSoundLikeType : ComparisonOperator
    {
        public DoesNotSoundLikeType(bool caseInsensitive = false, bool usesAll = false) : base("!~", 14, caseInsensitive, usesAll)
        {
        }

        public override string Operator() => Name;
        public override bool IsCountOperator() => false; 

        public override Expression GetExpression<T>(Expression left, Expression right, Type? dbContextType)
        {
            if (dbContextType == null)
            {
                throw new QueryKitDbContextTypeException("DbContext type must be provided to use the DoesNotSoundsLike operator.");
            }
            
            var method = dbContextType.GetMethod("SoundsLike", new Type[] { typeof(string) });

            if (method == null)
            {
                throw new SoundsLikeNotImplementedException(dbContextType.FullName!);
            }
            
            Expression leftMethodCall = Expression.Call(null, method, left);
            Expression rightMethodCall = Expression.Call(null, method, right);
            return Expression.NotEqual(leftMethodCall, rightMethodCall);
        }
    }

    private class HasCountEqualToType : ComparisonOperator
    {
        public HasCountEqualToType(bool caseInsensitive = false, bool usesAll = false) : base("#==", 15, caseInsensitive, usesAll)
        {
        }

        public override string Operator() => CaseInsensitive ? $"{Name}{CaseSensitiveAppendix}" : Name;
        public override bool IsCountOperator() => true; 
        public override Expression GetExpression<T>(Expression left, Expression right, Type? dbContextType)
        {
            return GetCountExpression(left, right, nameof(Expression.Equal));
        }
    }

    private class HasCountNotEqualToType : ComparisonOperator
    {
        public HasCountNotEqualToType(bool caseInsensitive = false, bool usesAll = false) : base("#!=", 16, caseInsensitive, usesAll)
        {
        }

        public override string Operator() => CaseInsensitive ? $"{Name}{CaseSensitiveAppendix}" : Name;
        public override bool IsCountOperator() => true; 
        public override Expression GetExpression<T>(Expression left, Expression right, Type? dbContextType)
        {
            return GetCountExpression(left, right, nameof(Expression.NotEqual));
        }
    }

    private class HasCountGreaterThanType : ComparisonOperator
    {
        public HasCountGreaterThanType(bool caseInsensitive = false, bool usesAll = false) : base("#>", 17, caseInsensitive, usesAll)
        {
        }

        public override string Operator() => CaseInsensitive ? $"{Name}{CaseSensitiveAppendix}" : Name;
        public override bool IsCountOperator() => true; 
        public override Expression GetExpression<T>(Expression left, Expression right, Type? dbContextType)
        {
            return GetCountExpression(left, right, nameof(Expression.GreaterThan));
        }
    }

    private class HasCountLessThanType : ComparisonOperator
    {
        public HasCountLessThanType(bool caseInsensitive = false, bool usesAll = false) : base("#<", 18, caseInsensitive, usesAll)
        {
        }

        public override string Operator() => CaseInsensitive ? $"{Name}{CaseSensitiveAppendix}" : Name;
        public override bool IsCountOperator() => true; 
        public override Expression GetExpression<T>(Expression left, Expression right, Type? dbContextType)
        {
            return GetCountExpression(left, right, nameof(Expression.LessThan));
        }
    }

    private class HasCountGreaterThanOrEqualType : ComparisonOperator
    {
        public HasCountGreaterThanOrEqualType(bool caseInsensitive = false, bool usesAll = false) : base("#>=", 19, caseInsensitive, usesAll)
        {
        }

        public override string Operator() => CaseInsensitive ? $"{Name}{CaseSensitiveAppendix}" : Name;
        public override bool IsCountOperator() => true; 
        public override Expression GetExpression<T>(Expression left, Expression right, Type? dbContextType)
        {
            return GetCountExpression(left, right, nameof(Expression.GreaterThanOrEqual));
        }
    }

    private class HasCountLessThanOrEqualType : ComparisonOperator
    {
        public HasCountLessThanOrEqualType(bool caseInsensitive = false, bool usesAll = false) : base("#<=", 20, caseInsensitive, usesAll)
        {
        }

        public override string Operator() => CaseInsensitive ? $"{Name}{CaseSensitiveAppendix}" : Name;
        public override bool IsCountOperator() => true; 
        public override Expression GetExpression<T>(Expression left, Expression right, Type? dbContextType)
        {
            return GetCountExpression(left, right, nameof(Expression.LessThanOrEqual));
        }
    }

    private class HasType : ComparisonOperator
    {
        public HasType(bool caseInsensitive = false, bool usesAll = false) : base("^$", 21, caseInsensitive, usesAll)
        {
        }

        public override string Operator() => CaseInsensitive ? $"{Name}{CaseSensitiveAppendix}" : Name;
        public override bool IsCountOperator() => false; 
        public override Expression GetExpression<T>(Expression left, Expression right, Type? dbContextType)
        {
            if (left.Type.IsGenericType && 
                (left.Type.GetGenericTypeDefinition() == typeof(List<>) || 
                 left.Type.GetGenericTypeDefinition() == typeof(ICollection<>) ||
                 left.Type.GetGenericTypeDefinition() == typeof(IList<>) ||
                 typeof(IEnumerable<>).IsAssignableFrom(left.Type.GetGenericTypeDefinition())))
            {
                return GetCollectionExpression(left, right, Expression.Equal, UsesAll);
            }

            throw new QueryKitParsingException("DoesNotHaveType is only supported for collections");
        }
    }

    private class DoesNotHaveType : ComparisonOperator
    {
        public DoesNotHaveType(bool caseInsensitive = false, bool usesAll = false) : base("!^$", 22, caseInsensitive, usesAll)
        {
        }

        public override string Operator() => CaseInsensitive ? $"{Name}{CaseSensitiveAppendix}" : Name;
        public override bool IsCountOperator() => false; 
        public override Expression GetExpression<T>(Expression left, Expression right, Type? dbContextType)
        {
            if (left.Type.IsGenericType && 
                (left.Type.GetGenericTypeDefinition() == typeof(List<>) || 
                 left.Type.GetGenericTypeDefinition() == typeof(ICollection<>) ||
                 left.Type.GetGenericTypeDefinition() == typeof(IList<>) ||
                 typeof(IEnumerable<>).IsAssignableFrom(left.Type.GetGenericTypeDefinition())))
            {
                return GetCollectionExpression(left, right, Expression.NotEqual, UsesAll);
            }
            
            throw new QueryKitParsingException("DoesNotHaveType is only supported for collections");
        }
    }
    
    private class NotInType : ComparisonOperator
    {
        public NotInType(bool caseInsensitive = false, bool usesAll = false) : base("!^^", 23, caseInsensitive, usesAll)
        {
        }

        public override string Operator() => CaseInsensitive ? $"{Name}{CaseSensitiveAppendix}" : Name;
        public override bool IsCountOperator() => false; 
        public override Expression GetExpression<T>(Expression left, Expression right, Type? dbContextType)
        {
            var leftType = left.Type;

            if (right is NewArrayExpression newArrayExpression)
            {
                var listType = typeof(List<>).MakeGenericType(leftType);
                var list = Activator.CreateInstance(listType);

                foreach (var value in newArrayExpression.Expressions)
                {
                    listType.GetMethod("Add")!.Invoke(list, new[] { ((ConstantExpression)value).Value });
                }

                right = Expression.Constant(list, listType);
            }

            // Get the Contains method with the correct generic type
            var containsMethod = typeof(ICollection<>)
                .MakeGenericType(leftType)
                .GetMethod("Contains")!;

            if (CaseInsensitive && leftType == typeof(string))
            {
                // null is not in any list, so it should be included: left == null || !list.Contains(left.ToLower())
                var nullCheck = Expression.Equal(left, Expression.Constant(null, typeof(string)));

                var listType = typeof(List<string>);
                var toLowerList = Activator.CreateInstance(listType);

                var originalList = ((ConstantExpression)right).Value as IEnumerable<string>;
                foreach (var value in originalList!)
                {
                    listType.GetMethod("Add")!.Invoke(toLowerList, new[] { value.ToLower() });
                }
                right = Expression.Constant(toLowerList, listType);
                var toLowerLeft = Expression.Call(left, typeof(string).GetMethod("ToLower", Type.EmptyTypes)!);

                var containsExpression = Expression.Call(right, containsMethod, toLowerLeft);
                return Expression.OrElse(nullCheck, Expression.Not(containsExpression));
            }

            var containsExpr = Expression.Call(right, containsMethod, left);
            return Expression.Not(containsExpr);
        }
    }

    internal class ComparisonAliasMatch
    {
        public ComparisonAliasMatch(string alias, string op)
        {
            Alias = alias;
            Operator = op;
        }

        public string Alias { get; }
        public string Operator { get; }
    }

    internal static List<ComparisonAliasMatch> GetAliasMatches(IQueryKitConfiguration aliases)
    {
        var matches = new List<ComparisonAliasMatch>();
        var caseInsensitiveAppendix = aliases.CaseInsensitiveAppendix;
        if(aliases.EqualsOperator != EqualsOperator().Operator())
        {
            matches.Add(new ComparisonAliasMatch(aliases.EqualsOperator, EqualsOperator().Operator()));
            matches.Add(new ComparisonAliasMatch($"{aliases.EqualsOperator}{caseInsensitiveAppendix}", $"{EqualsOperator(true).Operator()}"));
        }
        if(aliases.NotEqualsOperator != NotEqualsOperator().Operator())
        {
            matches.Add(new ComparisonAliasMatch(aliases.NotEqualsOperator, NotEqualsOperator().Operator()));
            matches.Add(new ComparisonAliasMatch($"{aliases.NotEqualsOperator}{caseInsensitiveAppendix}", $"{NotEqualsOperator(true).Operator()}"));
        }
        if(aliases.GreaterThanOperator != GreaterThanOperator().Operator())
        {
            matches.Add(new ComparisonAliasMatch(aliases.GreaterThanOperator, GreaterThanOperator().Operator()));
            matches.Add(new ComparisonAliasMatch($"{aliases.GreaterThanOperator}{caseInsensitiveAppendix}", $"{GreaterThanOperator(true).Operator()}"));
        }
        if(aliases.LessThanOperator != LessThanOperator().Operator())
        {
            matches.Add(new ComparisonAliasMatch(aliases.LessThanOperator, LessThanOperator().Operator()));
            matches.Add(new ComparisonAliasMatch($"{aliases.LessThanOperator}{caseInsensitiveAppendix}", $"{LessThanOperator(true).Operator()}"));
        }
        if(aliases.GreaterThanOrEqualOperator != GreaterThanOrEqualOperator().Operator())
        {
            matches.Add(new ComparisonAliasMatch(aliases.GreaterThanOrEqualOperator, GreaterThanOrEqualOperator().Operator()));
            matches.Add(new ComparisonAliasMatch($"{aliases.GreaterThanOrEqualOperator}{caseInsensitiveAppendix}", $"{GreaterThanOrEqualOperator(true).Operator()}"));
        }
        if(aliases.LessThanOrEqualOperator != LessThanOrEqualOperator().Operator())
        {
            matches.Add(new ComparisonAliasMatch(aliases.LessThanOrEqualOperator, LessThanOrEqualOperator().Operator()));
            matches.Add(new ComparisonAliasMatch($"{aliases.LessThanOrEqualOperator}{caseInsensitiveAppendix}", $"{LessThanOrEqualOperator(true).Operator()}"));
        }
        if(aliases.ContainsOperator != ContainsOperator().Operator())
        {
            matches.Add(new ComparisonAliasMatch(aliases.ContainsOperator, ContainsOperator().Operator()));
            matches.Add(new ComparisonAliasMatch($"{aliases.ContainsOperator}{caseInsensitiveAppendix}", $"{ContainsOperator(true).Operator()}"));
        }
        if(aliases.StartsWithOperator != StartsWithOperator().Operator())
        {
            matches.Add(new ComparisonAliasMatch(aliases.StartsWithOperator, StartsWithOperator().Operator()));
            matches.Add(new ComparisonAliasMatch($"{aliases.StartsWithOperator}{caseInsensitiveAppendix}", $"{StartsWithOperator(true).Operator()}"));
        }
        if(aliases.EndsWithOperator != EndsWithOperator().Operator())
        {
            matches.Add(new ComparisonAliasMatch(aliases.EndsWithOperator, EndsWithOperator().Operator()));
            matches.Add(new ComparisonAliasMatch($"{aliases.EndsWithOperator}{caseInsensitiveAppendix}", $"{EndsWithOperator(true).Operator()}"));
        }
        if(aliases.NotContainsOperator != NotContainsOperator().Operator())
        {
            matches.Add(new ComparisonAliasMatch(aliases.NotContainsOperator, NotContainsOperator().Operator()));
            matches.Add(new ComparisonAliasMatch($"{aliases.NotContainsOperator}{caseInsensitiveAppendix}", $"{NotContainsOperator(true).Operator()}"));
        }
        if(aliases.NotStartsWithOperator != NotStartsWithOperator().Operator())
        {
            matches.Add(new ComparisonAliasMatch(aliases.NotStartsWithOperator, NotStartsWithOperator().Operator()));
            matches.Add(new ComparisonAliasMatch($"{aliases.NotStartsWithOperator}{caseInsensitiveAppendix}", $"{NotStartsWithOperator(true).Operator()}"));
        }
        if(aliases.NotEndsWithOperator != NotEndsWithOperator().Operator())
        {
            matches.Add(new ComparisonAliasMatch(aliases.NotEndsWithOperator, NotEndsWithOperator().Operator()));
            matches.Add(new ComparisonAliasMatch($"{aliases.NotEndsWithOperator}{caseInsensitiveAppendix}", $"{NotEndsWithOperator(true).Operator()}"));
        }
        if(aliases.InOperator != InOperator().Operator())
        {
            matches.Add(new ComparisonAliasMatch(aliases.InOperator, InOperator().Operator()));
            matches.Add(new ComparisonAliasMatch($"{aliases.InOperator}{caseInsensitiveAppendix}", $"{InOperator(true).Operator()}"));
        }
        if(aliases.NotInOperator != NotInOperator().Operator())
        {
            matches.Add(new ComparisonAliasMatch(aliases.NotInOperator, NotInOperator().Operator()));
            matches.Add(new ComparisonAliasMatch($"{aliases.NotInOperator}{caseInsensitiveAppendix}", $"{NotInOperator(true).Operator()}"));
        }
        if(aliases.HasCountEqualToOperator != HasCountEqualToOperator().Operator())
        {
            matches.Add(new ComparisonAliasMatch(aliases.HasCountEqualToOperator, HasCountEqualToOperator().Operator()));
            matches.Add(new ComparisonAliasMatch($"{aliases.HasCountEqualToOperator}{caseInsensitiveAppendix}", $"{HasCountEqualToOperator(true).Operator()}"));
        }
        if(aliases.HasCountNotEqualToOperator != HasCountNotEqualToOperator().Operator())
        {
            matches.Add(new ComparisonAliasMatch(aliases.HasCountNotEqualToOperator, HasCountNotEqualToOperator().Operator()));
            matches.Add(new ComparisonAliasMatch($"{aliases.HasCountNotEqualToOperator}{caseInsensitiveAppendix}", $"{HasCountNotEqualToOperator(true).Operator()}"));
        }
        if(aliases.HasCountGreaterThanOperator != HasCountGreaterThanOperator().Operator())
        {
            matches.Add(new ComparisonAliasMatch(aliases.HasCountGreaterThanOperator, HasCountGreaterThanOperator().Operator()));
            matches.Add(new ComparisonAliasMatch($"{aliases.HasCountGreaterThanOperator}{caseInsensitiveAppendix}", $"{HasCountGreaterThanOperator(true).Operator()}"));
        }
        if(aliases.HasCountLessThanOperator != HasCountLessThanOperator().Operator())
        {
            matches.Add(new ComparisonAliasMatch(aliases.HasCountLessThanOperator, HasCountLessThanOperator().Operator()));
            matches.Add(new ComparisonAliasMatch($"{aliases.HasCountLessThanOperator}{caseInsensitiveAppendix}", $"{HasCountLessThanOperator(true).Operator()}"));
        }
        if(aliases.HasCountGreaterThanOrEqualOperator != HasCountGreaterThanOrEqualOperator().Operator())
        {
            matches.Add(new ComparisonAliasMatch(aliases.HasCountGreaterThanOrEqualOperator, HasCountGreaterThanOrEqualOperator().Operator()));
            matches.Add(new ComparisonAliasMatch($"{aliases.HasCountGreaterThanOrEqualOperator}{caseInsensitiveAppendix}", $"{HasCountGreaterThanOrEqualOperator(true).Operator()}"));
        }
        if(aliases.HasCountLessThanOrEqualOperator != HasCountLessThanOrEqualOperator().Operator())
        {
            matches.Add(new ComparisonAliasMatch(aliases.HasCountLessThanOrEqualOperator, HasCountLessThanOrEqualOperator().Operator()));
            matches.Add(new ComparisonAliasMatch($"{aliases.HasCountLessThanOrEqualOperator}{caseInsensitiveAppendix}", $"{HasCountLessThanOrEqualOperator(true).Operator()}"));
        }
        if(aliases.HasOperator != HasOperator().Operator())
        {
            matches.Add(new ComparisonAliasMatch(aliases.HasOperator, HasOperator().Operator()));
            matches.Add(new ComparisonAliasMatch($"{aliases.HasOperator}{caseInsensitiveAppendix}", $"{HasOperator(true).Operator()}"));
        }
        if(aliases.DoesNotHaveOperator != DoesNotHaveOperator().Operator())
        {
            matches.Add(new ComparisonAliasMatch(aliases.DoesNotHaveOperator, DoesNotHaveOperator().Operator()));
            matches.Add(new ComparisonAliasMatch($"{aliases.DoesNotHaveOperator}{caseInsensitiveAppendix}", $"{DoesNotHaveOperator(true).Operator()}"));
        }

        return matches;
    }
    
    private Expression GetCollectionExpression(Expression left, Expression right, Func<Expression, Expression, Expression> comparisonFunction, bool usesAll)
    {
        var xParameter = Expression.Parameter(left.Type.GetGenericArguments()[0], "z");
        Expression body;

        if (CaseInsensitive && xParameter.Type == typeof(string) && right.Type == typeof(string))
        {
            var toLowerLeft = Expression.Call(xParameter, typeof(string).GetMethod("ToLower", Type.EmptyTypes)!);
            var toLowerRight = Expression.Call(right, typeof(string).GetMethod("ToLower", Type.EmptyTypes)!);

            body = comparisonFunction(toLowerLeft, toLowerRight);
        }
        else
        {
            body = comparisonFunction(xParameter, right);
        }

        var anyLambda = Expression.Lambda(body, xParameter);
        var anyMethod = typeof(Enumerable)
            .GetMethods()
            .Single(m => m.Name == (usesAll ? "All" : "Any") && m.GetParameters().Length == 2)
            .MakeGenericMethod(left.Type.GetGenericArguments()[0]);

        return Expression.Call(anyMethod, left, anyLambda);
    }

    private Expression GetCollectionExpression(Expression left, Expression right, string methodName, bool negate, bool usesAll)
    {
        var xParameter = Expression.Parameter(left.Type.GetGenericArguments()[0], "z");
        Expression body;

        if (CaseInsensitive && xParameter.Type == typeof(string) && right.Type == typeof(string))
        {
            var toLowerLeft = Expression.Call(xParameter, typeof(string).GetMethod("ToLower", Type.EmptyTypes)!);
            var toLowerRight = Expression.Call(right, typeof(string).GetMethod("ToLower", Type.EmptyTypes)!);

            body = Expression.Call(toLowerLeft, typeof(string).GetMethod(methodName, new[] { typeof(string) })!, toLowerRight);
        }
        else
        {
            body = Expression.Call(xParameter, typeof(string).GetMethod(methodName, new[] { typeof(string) })!, right);
        }

        var anyLambda = Expression.Lambda(body, xParameter);
        var anyMethod = typeof(Enumerable)
            .GetMethods()
            .Single(m => m.Name == (usesAll ? "All" : "Any") && m.GetParameters().Length == 2)
            .MakeGenericMethod(left.Type.GetGenericArguments()[0]);

        return negate
            ? Expression.Not(Expression.Call(anyMethod, left, anyLambda))
            : Expression.Call(anyMethod, left, anyLambda);
    }

    private Expression GetCountExpression(Expression left, Expression right, string methodName)
    {
        var leftAsEnumerableType = left.Type.GetInterface(nameof(IEnumerable));
        if (leftAsEnumerableType == null)
        {
            throw new QueryKitParsingException("Left expression should be of type IEnumerable<T>");
        }

        var leftGenericType = left.Type.GetGenericArguments()[0];
        var rightType = right.Type;

        if (rightType != typeof(int))
        {
            throw new QueryKitParsingException("The right expression should be of type int");
        }
        var countMethod = typeof(Enumerable).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(m => m.Name == "Count" && m.GetParameters().Length == 1
                                                   && m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        if (countMethod == null)
        {
            throw new QueryKitParsingException("Count method not found");
        }

        var specificCountMethod = countMethod.MakeGenericMethod(leftGenericType);

        var countExpression = Expression.Call(null, specificCountMethod, left);
        var comparisonMethod = typeof(Expression).GetMethod(methodName, new[] { typeof(Expression), typeof(Expression) });
        if (comparisonMethod == null)
        {
            throw new QueryKitParsingException($"Comparison method '{methodName}' not found");
        }

        return (Expression)comparisonMethod.Invoke(null, new object[] { countExpression, right })!;
    }
    
    private static (Expression left, Expression right) EnsureCompatibleExpressionTypes(Expression left, Expression right)
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
    
    private static Type GetWiderNumericType(Type left, Type right)
    {
        var typeOrder = new[] { typeof(byte), typeof(sbyte), typeof(short), typeof(ushort), 
                               typeof(int), typeof(uint), typeof(long), typeof(ulong), 
                               typeof(float), typeof(double), typeof(decimal) };
        
        var leftIndex = Array.IndexOf(typeOrder, left);
        var rightIndex = Array.IndexOf(typeOrder, right);
        
        if (leftIndex == -1 || rightIndex == -1)
            return left; // fallback to left type if not found
            
        return typeOrder[Math.Max(leftIndex, rightIndex)];
    }
}
