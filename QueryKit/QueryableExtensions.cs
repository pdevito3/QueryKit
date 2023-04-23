namespace QueryKit;

public static class QueryableExtensions
{
    public static IQueryable<T> ApplyQueryKitFilter<T>(this IQueryable<T> source, string filter, IQueryKitConfiguration? config = null)
    {
        if (string.IsNullOrWhiteSpace(filter))
        {
            return source;
        }

        var expression = FilterParser.ParseFilter<T>(filter, config);
        return source.Where(expression);
    }

    public static IOrderedQueryable<T> ApplyQueryKitSort<T>(this IQueryable<T> queryable, string sortExpression, IQueryKitConfiguration? config = null)
    {
        var sortLambdas = SortParser.ParseSort<T>(sortExpression, config);

        if (sortLambdas.Count == 0)
        {
            return queryable.OrderBy(x => x);
        }

        var firstSortInfo = sortLambdas[0];
        if (firstSortInfo.Expression != null)
        {
            var orderedQueryable = firstSortInfo.IsAscending ? queryable.OrderBy(firstSortInfo.Expression) : queryable.OrderByDescending(firstSortInfo.Expression);

            for (var i = 1; i < sortLambdas.Count; i++)
            {
                var sortInfo = sortLambdas[i];
                if (sortInfo.Expression != null)
                    orderedQueryable = sortInfo.IsAscending
                        ? orderedQueryable.ThenBy(sortInfo.Expression)
                        : orderedQueryable.ThenByDescending(sortInfo.Expression);
            }

            return orderedQueryable;
        }
        
        return queryable.OrderBy(x => x);
    }
}