namespace QueryKit;

public interface IQueryKitConfiguration
{
    QueryKitPropertyMappings PropertyMappings { get; }
    string? GetPropertyPathByQueryName(string? propPath);
    bool IsPropertySortable(string? propertyName);
}

public class QueryKitConfiguration : IQueryKitConfiguration
{
    public QueryKitPropertyMappings PropertyMappings { get; }

    public QueryKitConfiguration(Action<QueryKitPropertyMappings> configure)
    {
        PropertyMappings = new QueryKitPropertyMappings();
        configure(PropertyMappings);
    }
    
    public string? GetPropertyPathByQueryName(string? queryName)
    {
        return PropertyMappings.GetPropertyPathByQueryName(queryName);
    }

    public bool IsPropertySortable(string? propertyName)
    {
        return PropertyMappings.GetPropertyInfo(propertyName)?.CanSort ?? true;
    }
}
