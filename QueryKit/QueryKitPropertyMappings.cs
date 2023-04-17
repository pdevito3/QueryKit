namespace QueryKit;

using System.Linq.Expressions;

public interface IQueryKitProcessorConfiguration
{
    QueryKitPropertyMappings PropertyMappings { get; }
}

public class QueryKitProcessorConfiguration : IQueryKitProcessorConfiguration
{
    public QueryKitPropertyMappings PropertyMappings { get; }

    public QueryKitProcessorConfiguration(Action<QueryKitPropertyMappings> configure)
    {
        PropertyMappings = new QueryKitPropertyMappings();
        configure(PropertyMappings);
    }
}

public class QueryKitPropertyMappings
{
    private readonly Dictionary<string, QueryKitPropertyInfo> _propertyMappings = new();

    public QueryKitPropertyMapping<TModel> Property<TModel>(Expression<Func<TModel, object>> propertySelector)
    {
        var memberExpression = propertySelector.Body is UnaryExpression unary
            ? (MemberExpression)unary.Operand
            : (MemberExpression)propertySelector.Body;

        var propertyName = memberExpression.Member.Name;
        var propertyInfo = new QueryKitPropertyInfo
        {
            Name = propertyName,
            CanFilter = true,
            CanSort = true,
            QueryName = propertyName
        };

        _propertyMappings[propertyName] = propertyInfo;

        return new QueryKitPropertyMapping<TModel>(propertyInfo, this);
    }
    public QueryKitPropertyInfo? GetPropertyInfo(string propertyName)
        =>  _propertyMappings.TryGetValue(propertyName, out var info) ? info : null;    
    public QueryKitPropertyInfo? GetPropertyInfoByQueryName(string queryName)
        =>  _propertyMappings.Values.FirstOrDefault(info => info.QueryName != null && info.QueryName.Equals(queryName, StringComparison.InvariantCultureIgnoreCase));    
}

public class QueryKitPropertyMapping<TModel>
{
    private readonly QueryKitPropertyInfo _propertyInfo;
    private readonly QueryKitPropertyMappings _mappings;

    public QueryKitPropertyMapping(QueryKitPropertyInfo propertyInfo, QueryKitPropertyMappings mappings)
    {
        _propertyInfo = propertyInfo;
        _mappings = mappings;
    }

    public QueryKitPropertyMapping<TModel> PreventFilter()
    {
        _propertyInfo.CanFilter = false;
        return this;
    }

    public QueryKitPropertyMapping<TModel> PreventSort()
    {
        _propertyInfo.CanSort = false;
        return this;
    }

    public QueryKitPropertyMappings HasQueryName(string queryName)
    {
        _propertyInfo.QueryName = queryName;
        return _mappings;
    }
}

public class QueryKitPropertyInfo
{
    public string? Name { get; set; }
    public bool CanFilter { get; set; }
    public bool CanSort { get; set; }
    public string? QueryName { get; set; }
}