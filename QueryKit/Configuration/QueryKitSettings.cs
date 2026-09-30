namespace QueryKit.Configuration;

using System.Linq.Expressions;
using QueryKit.Operators;

public class QueryKitSettings
{
    /// <summary>
    /// The default nesting depth limit is off. Set <see cref="MaxNestingDepth"/> to turn the limit on.
    /// </summary>
    public const int DefaultMaxNestingDepth = int.MaxValue;

    /// <summary>
    /// The default input length limit is off. Set <see cref="MaxInputLength"/> to turn the limit on.
    /// </summary>
    public const int DefaultMaxInputLength = int.MaxValue;

    public QueryKitPropertyMappings PropertyMappings { get; set; } = new QueryKitPropertyMappings();
    public string EqualsOperator { get; set; } = ComparisonOperator.EqualsOperator().Operator();
    public string NotEqualsOperator { get; set; } = ComparisonOperator.NotEqualsOperator().Operator();
    public string GreaterThanOperator { get; set; } = ComparisonOperator.GreaterThanOperator().Operator();
    public string LessThanOperator { get; set; } = ComparisonOperator.LessThanOperator().Operator();
    public string GreaterThanOrEqualOperator { get; set; } = ComparisonOperator.GreaterThanOrEqualOperator().Operator();
    public string LessThanOrEqualOperator { get; set; } = ComparisonOperator.LessThanOrEqualOperator().Operator();
    public string ContainsOperator { get; set; } = ComparisonOperator.ContainsOperator().Operator();
    public string StartsWithOperator { get; set; } = ComparisonOperator.StartsWithOperator().Operator();
    public string EndsWithOperator { get; set; } = ComparisonOperator.EndsWithOperator().Operator();
    public string NotContainsOperator { get; set; } = ComparisonOperator.NotContainsOperator().Operator();
    public string NotStartsWithOperator { get; set; } = ComparisonOperator.NotStartsWithOperator().Operator();
    public string NotEndsWithOperator { get; set; } = ComparisonOperator.NotEndsWithOperator().Operator();
    public string InOperator { get; set; } = ComparisonOperator.InOperator().Operator();
    public string NotInOperator { get; set; } = ComparisonOperator.NotInOperator().Operator();
    public string SoundsLikeOperator { get; set; } = ComparisonOperator.SoundsLikeOperator().Operator();
    public string DoesNotSoundLikeOperator { get; set; } = ComparisonOperator.DoesNotSoundLikeOperator().Operator();
    public string HasCountEqualToOperator { get; set; } = ComparisonOperator.HasCountEqualToOperator().Operator();
    public string HasCountNotEqualToOperator { get; set; } = ComparisonOperator.HasCountNotEqualToOperator().Operator();
    public string HasCountGreaterThanOperator { get; set; } = ComparisonOperator.HasCountGreaterThanOperator().Operator();
    public string HasCountLessThanOperator { get; set; } = ComparisonOperator.HasCountLessThanOperator().Operator();
    public string HasCountGreaterThanOrEqualOperator { get; set; } = ComparisonOperator.HasCountGreaterThanOrEqualOperator().Operator();
    public string HasCountLessThanOrEqualOperator { get; set; } = ComparisonOperator.HasCountLessThanOrEqualOperator().Operator();
    public string HasOperator { get; set; } = ComparisonOperator.HasOperator().Operator();
    public string DoesNotHaveOperator { get; set; } = ComparisonOperator.DoesNotHaveOperator().Operator();
    public string AndOperator { get; set; } = LogicalOperator.AndOperator.Operator();
    public string OrOperator { get; set; } = LogicalOperator.OrOperator.Operator();
    public string CaseInsensitiveAppendix { get; set; } = ComparisonOperator.CaseSensitiveAppendix.ToString();
    public bool AllowUnknownProperties { get; set; }
    public Type? DbContextType { get; set; }
    public int? MaxPropertyDepth { get; set; }
    public int MaxNestingDepth { get; set; } = DefaultMaxNestingDepth;
    public int MaxInputLength { get; set; } = DefaultMaxInputLength;
    public CaseInsensitiveMode CaseInsensitiveComparison { get; set; } = CaseInsensitiveMode.Lower;

    /// <summary>
    /// When true, filter values are field reads that EF Core sends as SQL parameters. When false (the default),
    /// filter values are constants that EF Core writes into the SQL as literals.
    /// </summary>
    public bool ParameterizeFilterValues { get; set; }

    /// <summary>
    /// What the filter parser does with a clause on a prevented or unknown property. The default is
    /// <see cref="Configuration.IgnoredClauseBehavior.Remove"/>. Use
    /// <see cref="Configuration.IgnoredClauseBehavior.ReplaceWithTrue"/> for the v1.14.2 behavior.
    /// </summary>
    public IgnoredClauseBehavior IgnoredClauseBehavior { get; set; } = IgnoredClauseBehavior.Remove;

    public QueryKitPropertyMapping<TModel> Property<TModel>(Expression<Func<TModel, object>>? propertySelector)
    {
        return PropertyMappings.Property(propertySelector);
    }
    
    public QueryKitPropertyMapping<TModel> DerivedProperty<TModel>(Expression<Func<TModel, object>>? propertySelector)
    {
        return PropertyMappings.DerivedProperty(propertySelector);
    }

    public QueryKitCustomOperationMapping<TModel> CustomOperation<TModel>(Expression<Func<TModel, ComparisonOperator, object, bool>> operationExpression)
    {
        return PropertyMappings.CustomOperation(operationExpression);
    }
}