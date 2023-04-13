namespace QueryKit;

using System.Linq.Expressions;

public interface IQueryKitPropertyConfiguration
{
    void Configure(QueryKitPropertyMappings mappings);
}


public class QueryKitPropertyMappings
{
    private readonly Dictionary<string, QueryKitPropertyInfo> _propertyMappings;

    public QueryKitPropertyMappings()
    {
        _propertyMappings = new Dictionary<string, QueryKitPropertyInfo>();
    }

    public QueryKitPropertyMapping<TModel> Property<TModel>(Expression<Func<TModel, object>> propertySelector)
    {
        if (propertySelector.Body is not MemberExpression memberExpression)
        {
            throw new ArgumentException("Invalid expression: must be a member access expression.");
        }

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
    {
        return _propertyMappings.TryGetValue(propertyName, out var info) ? info : null;
    }
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