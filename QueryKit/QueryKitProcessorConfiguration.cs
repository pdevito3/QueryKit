namespace QueryKit;

public interface IQueryKitProcessorConfiguration
{
    QueryKitPropertyMappings PropertyMappings { get; }
    string? GetPropertyPathByQueryName(string propPath);
}

public class QueryKitProcessorConfiguration : IQueryKitProcessorConfiguration
{
    public QueryKitPropertyMappings PropertyMappings { get; }

    public QueryKitProcessorConfiguration(Action<QueryKitPropertyMappings> configure)
    {
        PropertyMappings = new QueryKitPropertyMappings();
        configure(PropertyMappings);
    }
    
    public string? GetPropertyPathByQueryName(string queryName)
    {
        return PropertyMappings.GetPropertyPathByQueryName(queryName);
    }
}
