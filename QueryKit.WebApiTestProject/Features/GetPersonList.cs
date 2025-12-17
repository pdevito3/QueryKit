namespace QueryKit.WebApiTestProject.Features;

using Configuration;
using Database;
using Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

public static class GetPersonList
{
    public sealed record Query(string FilterInput, string SortInput, IQueryKitConfiguration? QueryKitConfig = null) : IRequest<List<TestingPerson>>;

    public sealed class Handler(TestingDbContext testingDbContext) : IRequestHandler<Query, List<TestingPerson>>
    {
        public async Task<List<TestingPerson>> Handle(Query request, CancellationToken cancellationToken)
        {
            var queryablePeople = testingDbContext.People;
            var appliedQueryable = queryablePeople
                .ApplyQueryKitFilter(request.FilterInput, request.QueryKitConfig)
                .ApplyQueryKitSort(request.SortInput, request.QueryKitConfig);
            return await appliedQueryable.ToListAsync(cancellationToken);
        }
    }
}