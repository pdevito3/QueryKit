namespace QueryKit;

public interface IQueryKitProcessorConfiguration
{
    QueryKitPropertyMappings PropertyMappings { get; }
    string? GetPropertyPathByQueryName(string propPath);
    bool IsPropertySortable(string propertyName);
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

    public bool IsPropertySortable(string propertyName)
    {
        return PropertyMappings.GetPropertyInfo(propertyName)?.CanSort ?? true;
    }
}
