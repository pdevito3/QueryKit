namespace QueryKit.Configuration;

public interface IQueryKitConfiguration
{
    QueryKitPropertyMappings PropertyMappings { get; }
    public string EqualsOperator { get; set; }
    public string NotEqualsOperator { get; set; }
    public string GreaterThanOperator { get; set; }
    public string LessThanOperator { get; set; }
    public string GreaterThanOrEqualOperator { get; set; }
    public string LessThanOrEqualOperator { get; set; }
    public string ContainsOperator { get; set; }
    public string StartsWithOperator { get; set; }
    public string EndsWithOperator { get; set; }
    public string NotContainsOperator { get; set; }
    public string NotStartsWithOperator { get; set; }
    public string NotEndsWithOperator { get; set; }
    public string InOperator { get; set; }
    public string NotInOperator { get; set; }
    public string SoundsLikeOperator { get; set; }
    public string DoesNotSoundLikeOperator { get; set; }
    public string CaseInsensitiveAppendix { get; set; }
    public string AndOperator { get; set; }
    public string OrOperator { get; set; }
    public bool AllowUnknownProperties { get; set; }
    public Type? DbContextType { get; set; }
    public string HasCountEqualToOperator { get; set; }
    public string HasCountNotEqualToOperator { get; set; }
    public string HasCountGreaterThanOperator { get; set; }
    public string HasCountLessThanOperator { get; set; }
    public string HasCountGreaterThanOrEqualOperator { get; set; }
    public string HasCountLessThanOrEqualOperator { get; set; }
    public string HasOperator { get; set; }
    public string DoesNotHaveOperator { get; set; }
    public int? MaxPropertyDepth { get; set; }
}

public class QueryKitConfiguration : IQueryKitConfiguration
{
    public QueryKitPropertyMappings PropertyMappings { get; }
    public string EqualsOperator { get; set; }
    public string NotEqualsOperator { get; set; }
    public string GreaterThanOperator { get; set; }
    public string LessThanOperator { get; set; }
    public string GreaterThanOrEqualOperator { get; set; }
    public string LessThanOrEqualOperator { get; set; }
    public string ContainsOperator { get; set; }
    public string StartsWithOperator { get; set; }
    public string EndsWithOperator { get; set; }
    public string NotContainsOperator { get; set; }
    public string NotStartsWithOperator { get; set; }
    public string NotEndsWithOperator { get; set; }
    public string InOperator { get; set; }
    public string NotInOperator { get; set; }
    public string SoundsLikeOperator { get; set; }
    public string DoesNotSoundLikeOperator { get; set; }
    public string HasCountEqualToOperator { get; set; }
    public string HasCountNotEqualToOperator { get; set; }
    public string HasCountGreaterThanOperator { get; set; }
    public string HasCountLessThanOperator { get; set; }
    public string HasCountGreaterThanOrEqualOperator { get; set; }
    public string HasCountLessThanOrEqualOperator { get; set; }
    public string HasOperator { get; set; }
    public string DoesNotHaveOperator { get; set; }
    public string CaseInsensitiveAppendix { get; set; }
    public string AndOperator { get; set; }
    public string OrOperator { get; set; }
    public bool AllowUnknownProperties { get; set; } = false;
    public Type? DbContextType { get; set; }
    public int? MaxPropertyDepth { get; set; }

    public QueryKitConfiguration(Action<QueryKitSettings> configureSettings)
    {
        var settings = new QueryKitSettings();
        configureSettings(settings);

        PropertyMappings = settings.PropertyMappings;
        EqualsOperator = settings.EqualsOperator;
        NotEqualsOperator = settings.NotEqualsOperator;
        GreaterThanOperator = settings.GreaterThanOperator;
        LessThanOperator = settings.LessThanOperator;
        GreaterThanOrEqualOperator = settings.GreaterThanOrEqualOperator;
        LessThanOrEqualOperator = settings.LessThanOrEqualOperator;
        ContainsOperator = settings.ContainsOperator;
        StartsWithOperator = settings.StartsWithOperator;
        EndsWithOperator = settings.EndsWithOperator;
        NotContainsOperator = settings.NotContainsOperator;
        NotStartsWithOperator = settings.NotStartsWithOperator;
        NotEndsWithOperator = settings.NotEndsWithOperator;
        InOperator = settings.InOperator;
        SoundsLikeOperator = settings.SoundsLikeOperator;
        NotInOperator = settings.NotInOperator;
        DoesNotSoundLikeOperator = settings.DoesNotSoundLikeOperator;
        CaseInsensitiveAppendix = settings.CaseInsensitiveAppendix;
        AndOperator = settings.AndOperator;
        OrOperator = settings.OrOperator;
        AllowUnknownProperties = settings.AllowUnknownProperties;
        DbContextType = settings.DbContextType;
        
        HasCountEqualToOperator = settings.HasCountEqualToOperator;
        HasCountNotEqualToOperator = settings.HasCountNotEqualToOperator;
        HasCountGreaterThanOperator = settings.HasCountGreaterThanOperator;
        HasCountLessThanOperator = settings.HasCountLessThanOperator;
        HasCountGreaterThanOrEqualOperator = settings.HasCountGreaterThanOrEqualOperator;
        HasCountLessThanOrEqualOperator = settings.HasCountLessThanOrEqualOperator;
        HasOperator = settings.HasOperator;
        DoesNotHaveOperator = settings.DoesNotHaveOperator;
        MaxPropertyDepth = settings.MaxPropertyDepth;
    }
}