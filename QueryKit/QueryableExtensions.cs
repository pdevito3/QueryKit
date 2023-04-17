namespace QueryKit;

public static class QueryableExtensions
{
    public static IQueryable<T> ApplyQueryKitFilter<T>(this IQueryable<T> source, string filter, IQueryKitProcessorConfiguration config = null)
    {
        if (string.IsNullOrWhiteSpace(filter))
        {
            return source;
        }

        var expression = FilterParser.ParseFilter<T>(filter, config);
        return source.Where(expression);
    }
}