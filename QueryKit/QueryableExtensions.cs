namespace QueryKit;

using System.Reflection;
using Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Query.Internal;

public static class QueryableExtensions
{
    public static IQueryable<T> ApplyQueryKitFilter<T>(this IQueryable<T> source, string filter, IQueryKitConfiguration? config = null) 
        where T : class
    {
        if (string.IsNullOrWhiteSpace(filter))
        {
            return source;
        }

        var dbContext = source.GetDbContext();
        var expression = FilterParser.ParseFilter<T>(filter, config, dbContext);
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

internal static class DbSetExt
{
    internal static DbContext GetDbContext<T>(this DbSet<T> dbSet) where T : class
    {
        var infrastructure = dbSet as IInfrastructure<IServiceProvider>;
        var serviceProvider = infrastructure.Instance;
        var currentDbContext = serviceProvider.GetService(typeof(ICurrentDbContext)) as ICurrentDbContext;
        return currentDbContext?.Context;
    }
    internal static DbContext GetDbContext<T>(this IQueryable<T> query) where T : class
    {
        // Check if the query is a DbSet query
        if (query is DbSet<T> dbSet)
        {
            return GetDbContext(dbSet);
        }

        // If not, try to get IQueryProvider
        if (query.Provider is EntityQueryProvider entityQueryProvider)
        {
            var serviceProvider = ((IInfrastructure<IServiceProvider>)entityQueryProvider).Instance;
            var currentDbContext = serviceProvider.GetService(typeof(ICurrentDbContext)) as ICurrentDbContext;
            return currentDbContext?.Context;
        }

        throw new ArgumentException("The query is not associated with a DbContext.", nameof(query));
    }


}