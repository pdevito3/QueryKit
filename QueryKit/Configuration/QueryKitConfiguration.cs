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
    public string CaseInsensitiveAppendix { get; set; }
    public string AndOperator { get; set; }
    public string OrOperator { get; set; }
    public bool AllowUnknownProperties { get; set; }
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
    public string CaseInsensitiveAppendix { get; set; }
    public string AndOperator { get; set; }
    public string OrOperator { get; set; }
    public bool AllowUnknownProperties { get; set; } = false;
    
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
        CaseInsensitiveAppendix = settings.CaseInsensitiveAppendix;
        AndOperator = settings.AndOperator;
        OrOperator = settings.OrOperator;
        AllowUnknownProperties = settings.AllowUnknownProperties;
    }
}