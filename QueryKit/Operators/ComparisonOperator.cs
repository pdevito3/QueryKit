namespace QueryKit.Operators;

using System.Linq.Expressions;
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
    public static ComparisonOperator CaseSensitiveSoundsLikeOperator = new SoundsLikeType();
    public static ComparisonOperator CaseSensitiveDoesNotSoundLikeOperator = new DoesNotSoundLikeType();
    
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
    public static ComparisonOperator SoundsLikeOperator(bool caseInsensitive = false, bool usesAll = false) => new SoundsLikeType(caseInsensitive);
    public static ComparisonOperator DoesNotSoundLikeOperator(bool caseInsensitive = false, bool usesAll = false) => new DoesNotSoundLikeType(caseInsensitive);

    
    public static ComparisonOperator GetByOperatorString(string op, bool caseInsensitive = false, bool usesAll = false)
    {
        var comparisonOperator = List.FirstOrDefault(x => x.Operator() == op);
        if (comparisonOperator == null)
        {
            throw new Exception($"Operator {op} is not supported");
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
        if (comparisonOperator is SoundsLikeType)
        {
            newOperator = new SoundsLikeType(caseInsensitive, usesAll);
        }
        if (comparisonOperator is DoesNotSoundLikeType)
        {
            newOperator = new DoesNotSoundLikeType(caseInsensitive, usesAll);
        }
        
        return newOperator == null 
            ? throw new Exception($"Operator {op} is not supported")
            : newOperator!;
    }

    public const char CaseSensitiveAppendix = '*';
    public const char AllPrefix = '#';
    public abstract string Operator();
    public bool CaseInsensitive { get; protected set; }
    public bool UsesAll { get; protected set; }
    public abstract Expression GetExpression<T>(Expression left, Expression right, Type? dbContextType);
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
        public override Expression GetExpression<T>(Expression left, Expression right, Type? dbContextType)
        {
            if (left.Type.IsGenericType && left.Type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                return GetCollectionExpression(left, right, Expression.Equal, UsesAll);
            }
            
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
        public NotEqualsType(bool caseInsensitive = false, bool usesAll = false) : base("!=", 1, caseInsensitive, usesAll)
        {
        }

        public override string Operator() => CaseInsensitive ? $"{Name}{CaseSensitiveAppendix}" : Name;
        public override Expression GetExpression<T>(Expression left, Expression right, Type? dbContextType)
        {
            if (left.Type.IsGenericType && left.Type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                return GetCollectionExpression(left, right, Expression.NotEqual, UsesAll);
            }
    
            if (CaseInsensitive && left.Type == typeof(string) && right.Type == typeof(string))
            {
                return Expression.NotEqual(
                    Expression.Call(left, typeof(string).GetMethod("ToLower", Type.EmptyTypes)),
                    Expression.Call(right, typeof(string).GetMethod("ToLower", Type.EmptyTypes))
                );
            }

            return Expression.NotEqual(left, right);
        }
    }

    private class GreaterThanType : ComparisonOperator
    {
        public GreaterThanType(bool caseInsensitive = false, bool usesAll = false) : base(">", 2, caseInsensitive, usesAll)
        {
        }

        public override string Operator() => Name;
        public override Expression GetExpression<T>(Expression left, Expression right, Type? dbContextType)
        {
            if (left.Type.IsGenericType && left.Type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                return GetCollectionExpression(left, right, Expression.GreaterThan, UsesAll);
            }
            return Expression.GreaterThan(left, right);
        }
    }

    private class LessThanType : ComparisonOperator
    {
        public LessThanType(bool caseInsensitive = false, bool usesAll = false) : base("<", 3, caseInsensitive, usesAll)
        {
        }

        public override string Operator() => Name;
        public override Expression GetExpression<T>(Expression left, Expression right, Type? dbContextType)
        {
            if (left.Type.IsGenericType && left.Type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                return GetCollectionExpression(left, right, Expression.LessThan, UsesAll);
            }
            return Expression.LessThan(left, right);
        }
    }

    private class GreaterThanOrEqualType : ComparisonOperator
    {
        public override string Operator() => Name;
        public GreaterThanOrEqualType(bool caseInsensitive = false, bool usesAll = false) : base(">=", 4, caseInsensitive, usesAll)
        {
        }
        public override Expression GetExpression<T>(Expression left, Expression right, Type? dbContextType)
        {
            if (left.Type.IsGenericType && left.Type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                return GetCollectionExpression(left, right, Expression.GreaterThanOrEqual, UsesAll);
            }
            return Expression.GreaterThanOrEqual(left, right);
        }
    }

    private class LessThanOrEqualType : ComparisonOperator
    {
        public LessThanOrEqualType(bool caseInsensitive = false, bool usesAll = false) : base("<=", 5, caseInsensitive, usesAll)
        {
        }
        public override string Operator() => Name;
        public override Expression GetExpression<T>(Expression left, Expression right, Type? dbContextType)
        {
            if (left.Type.IsGenericType && left.Type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                return GetCollectionExpression(left, right, Expression.LessThanOrEqual, UsesAll);
            }
            return Expression.LessThanOrEqual(left, right);
        }
    }

    private class ContainsType : ComparisonOperator
    {
        public ContainsType(bool caseInsensitive = false, bool usesAll = false) : base("@=", 6, caseInsensitive, usesAll)
        {
        }

        public override string Operator() => CaseInsensitive ? $"{Name}{CaseSensitiveAppendix}" : Name;
        public override Expression GetExpression<T>(Expression left, Expression right, Type? dbContextType)
        {
            if (left.Type.IsGenericType && left.Type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                return GetCollectionExpression(left, right, "Contains", false, UsesAll);
            }
    
            if (CaseInsensitive && left.Type == typeof(string) && right.Type == typeof(string))
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
        public StartsWithType(bool caseInsensitive = false, bool usesAll = false) : base("_=", 7, caseInsensitive, usesAll)
        {
        }

        public override string Operator() => CaseInsensitive ? $"{Name}{CaseSensitiveAppendix}" : Name;
        public override Expression GetExpression<T>(Expression left, Expression right, Type? dbContextType)
        {
            if (left.Type.IsGenericType && left.Type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                return GetCollectionExpression(left, right, "StartsWith", false, UsesAll);
            }
        
            if (CaseInsensitive && left.Type == typeof(string) && right.Type == typeof(string))
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
        public EndsWithType(bool caseInsensitive = false, bool usesAll = false) : base("_-=", 8, caseInsensitive, usesAll)
        {
        }

        public override string Operator() => CaseInsensitive ? $"{Name}{CaseSensitiveAppendix}" : Name;
        public override Expression GetExpression<T>(Expression left, Expression right, Type? dbContextType)
        {
            if (left.Type.IsGenericType && left.Type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                return GetCollectionExpression(left, right, "EndsWith", false, UsesAll);
            }
            
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
        public NotContainsType(bool caseInsensitive = false, bool usesAll = false) : base("!@=", 9, caseInsensitive, usesAll)
        {
        }

        public override string Operator() => CaseInsensitive ? $"{Name}{CaseSensitiveAppendix}" : Name;
        public override Expression GetExpression<T>(Expression left, Expression right, Type? dbContextType)
        {
            if (left.Type.IsGenericType && left.Type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                return GetCollectionExpression(left, right, "Contains", true, UsesAll);
            }
            
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
        public NotStartsWithType(bool caseInsensitive = false, bool usesAll = false) : base("!_=", 10, caseInsensitive, usesAll)
        {
        }

        public override string Operator() => CaseInsensitive ? $"{Name}{CaseSensitiveAppendix}" : Name;
        public override Expression GetExpression<T>(Expression left, Expression right, Type? dbContextType)
        {
            if (left.Type.IsGenericType && left.Type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                return GetCollectionExpression(left, right, "StartsWith", true, UsesAll);
            }
            
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
        public NotEndsWithType(bool caseInsensitive = false, bool usesAll = false) : base("!_-=", 11, caseInsensitive, usesAll)
        {
        }

        public override string Operator() => CaseInsensitive ? $"{Name}{CaseSensitiveAppendix}" : Name;
        public override Expression GetExpression<T>(Expression left, Expression right, Type? dbContextType)
        {
            if (left.Type.IsGenericType && left.Type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                return GetCollectionExpression(left, right, "EndsWith", true, UsesAll);
            }
            
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
        public InType(bool caseInsensitive = false, bool usesAll = false) : base("^^", 12, caseInsensitive, usesAll)
        {
        }

        public override string Operator() => CaseInsensitive ? $"{Name}{CaseSensitiveAppendix}" : Name;
        public override Expression GetExpression<T>(Expression left, Expression right, Type? dbContextType)
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

    private class SoundsLikeType : ComparisonOperator
    {
        public SoundsLikeType(bool caseInsensitive = false, bool usesAll = false) : base("~~", 13, caseInsensitive, usesAll)
        {
        }

        public override string Operator() => Name;

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


    internal class ComparisonAliasMatch
    {
        public string Alias { get; set; }
        public string Operator { get; set; }
    }
    
    internal static List<ComparisonAliasMatch> GetAliasMatches(IQueryKitConfiguration aliases)
    {
        var matches = new List<ComparisonAliasMatch>();
        var caseInsensitiveAppendix = aliases.CaseInsensitiveAppendix;
        if(aliases.EqualsOperator != EqualsOperator().Operator())
        {
            matches.Add(new ComparisonAliasMatch { Alias = aliases.EqualsOperator, Operator = EqualsOperator().Operator() });
            matches.Add(new ComparisonAliasMatch { Alias = $"{aliases.EqualsOperator}{caseInsensitiveAppendix}", Operator = $"{EqualsOperator(true).Operator()}"});
        }
        if(aliases.NotEqualsOperator != NotEqualsOperator().Operator())
        {
            matches.Add(new ComparisonAliasMatch { Alias = aliases.NotEqualsOperator, Operator = NotEqualsOperator().Operator() });
            matches.Add(new ComparisonAliasMatch { Alias = $"{aliases.NotEqualsOperator}{caseInsensitiveAppendix}", Operator = $"{NotEqualsOperator(true).Operator()}" });
        }
        if(aliases.GreaterThanOperator != GreaterThanOperator().Operator())
        {
            matches.Add(new ComparisonAliasMatch { Alias = aliases.GreaterThanOperator, Operator = GreaterThanOperator().Operator() });
            matches.Add(new ComparisonAliasMatch { Alias = $"{aliases.GreaterThanOperator}{caseInsensitiveAppendix}", Operator = $"{GreaterThanOperator(true).Operator()}" });
        }
        if(aliases.LessThanOperator != LessThanOperator().Operator())
        {
            matches.Add(new ComparisonAliasMatch { Alias = aliases.LessThanOperator, Operator = LessThanOperator().Operator() });
            matches.Add(new ComparisonAliasMatch { Alias = $"{aliases.LessThanOperator}{caseInsensitiveAppendix}", Operator = $"{LessThanOperator(true).Operator()}" });
        }
        if(aliases.GreaterThanOrEqualOperator != GreaterThanOrEqualOperator().Operator())
        {
            matches.Add(new ComparisonAliasMatch { Alias = aliases.GreaterThanOrEqualOperator, Operator = GreaterThanOrEqualOperator().Operator() });
            matches.Add(new ComparisonAliasMatch { Alias = $"{aliases.GreaterThanOrEqualOperator}{caseInsensitiveAppendix}", Operator = $"{GreaterThanOrEqualOperator(true).Operator()}" });
        }
        if(aliases.LessThanOrEqualOperator != LessThanOrEqualOperator().Operator())
        {
            matches.Add(new ComparisonAliasMatch { Alias = aliases.LessThanOrEqualOperator, Operator = LessThanOrEqualOperator().Operator() });
            matches.Add(new ComparisonAliasMatch { Alias = $"{aliases.LessThanOrEqualOperator}{caseInsensitiveAppendix}", Operator = $"{LessThanOrEqualOperator(true).Operator()}" });
        }
        if(aliases.ContainsOperator != ContainsOperator().Operator())
        {
            matches.Add(new ComparisonAliasMatch { Alias = aliases.ContainsOperator, Operator = ContainsOperator().Operator() });
            matches.Add(new ComparisonAliasMatch { Alias = $"{aliases.ContainsOperator}{caseInsensitiveAppendix}", Operator = $"{ContainsOperator(true).Operator()}" });
        }
        if(aliases.StartsWithOperator != StartsWithOperator().Operator())
        {
            matches.Add(new ComparisonAliasMatch { Alias = aliases.StartsWithOperator, Operator = StartsWithOperator().Operator() });
            matches.Add(new ComparisonAliasMatch { Alias = $"{aliases.StartsWithOperator}{caseInsensitiveAppendix}", Operator = $"{StartsWithOperator(true).Operator()}" });
        }
        if(aliases.EndsWithOperator != EndsWithOperator().Operator())
        {
            matches.Add(new ComparisonAliasMatch { Alias = aliases.EndsWithOperator, Operator = EndsWithOperator().Operator() });
            matches.Add(new ComparisonAliasMatch { Alias = $"{aliases.EndsWithOperator}{caseInsensitiveAppendix}", Operator = $"{EndsWithOperator(true).Operator()}" });
        }
        if(aliases.NotContainsOperator != NotContainsOperator().Operator())
        {
            matches.Add(new ComparisonAliasMatch { Alias = aliases.NotContainsOperator, Operator = NotContainsOperator().Operator() });
            matches.Add(new ComparisonAliasMatch { Alias = $"{aliases.NotContainsOperator}{caseInsensitiveAppendix}", Operator = $"{NotContainsOperator(true).Operator()}" });
        }
        if(aliases.NotStartsWithOperator != NotStartsWithOperator().Operator())
        {
            matches.Add(new ComparisonAliasMatch { Alias = aliases.NotStartsWithOperator, Operator = NotStartsWithOperator().Operator() });
            matches.Add(new ComparisonAliasMatch { Alias = $"{aliases.NotStartsWithOperator}{caseInsensitiveAppendix}", Operator = $"{NotStartsWithOperator(true).Operator()}" });
        }
        if(aliases.NotEndsWithOperator != NotEndsWithOperator().Operator())
        {
            matches.Add(new ComparisonAliasMatch { Alias = aliases.NotEndsWithOperator, Operator = NotEndsWithOperator().Operator() });
            matches.Add(new ComparisonAliasMatch { Alias = $"{aliases.NotEndsWithOperator}{caseInsensitiveAppendix}", Operator = $"{NotEndsWithOperator(true).Operator()}" });
        }
        if(aliases.InOperator != InOperator().Operator())
        {
            matches.Add(new ComparisonAliasMatch { Alias = aliases.InOperator, Operator = InOperator().Operator() });
            matches.Add(new ComparisonAliasMatch { Alias = $"{aliases.InOperator}{caseInsensitiveAppendix}", Operator = $"{InOperator(true).Operator()}" });
        }
        
        return matches;
    }
    
    private Expression GetCollectionExpression(Expression left, Expression right, Func<Expression, Expression, Expression> comparisonFunction, bool usesAll)
    {
        var xParameter = Expression.Parameter(left.Type.GetGenericArguments()[0], "z");
        Expression body;

        if (CaseInsensitive && xParameter.Type == typeof(string) && right.Type == typeof(string))
        {
            var toLowerLeft = Expression.Call(xParameter, typeof(string).GetMethod("ToLower", Type.EmptyTypes));
            var toLowerRight = Expression.Call(right, typeof(string).GetMethod("ToLower", Type.EmptyTypes));

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
            var toLowerLeft = Expression.Call(xParameter, typeof(string).GetMethod("ToLower", Type.EmptyTypes));
            var toLowerRight = Expression.Call(right, typeof(string).GetMethod("ToLower", Type.EmptyTypes));

            body = Expression.Call(toLowerLeft, typeof(string).GetMethod(methodName, new[] { typeof(string) }), toLowerRight);
        }
        else
        {
            body = Expression.Call(xParameter, typeof(string).GetMethod(methodName, new[] { typeof(string) }), right);
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
}
