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

    public static IOrderedQueryable<T> ApplyQueryKitSort<T>(this IQueryable<T> queryable, string sortExpression, IQueryKitProcessorConfiguration config = null)
    {
        var sortLambdas = SortParser.ParseSort<T>(sortExpression, config);

        if (sortLambdas.Count == 0)
        {
            return queryable.OrderBy(x => x);
        }

        var firstSortInfo = sortLambdas[0];
        var orderedQueryable = firstSortInfo.IsAscending ? queryable.OrderBy(firstSortInfo.Expression) : queryable.OrderByDescending(firstSortInfo.Expression);

        for (var i = 1; i < sortLambdas.Count; i++)
        {
            var sortInfo = sortLambdas[i];
            orderedQueryable = sortInfo.IsAscending ? orderedQueryable.ThenBy(sortInfo.Expression) : orderedQueryable.ThenByDescending(sortInfo.Expression);
        }

        return orderedQueryable;
    }
}