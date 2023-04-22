namespace QueryKit.WebApiTestProject.Features;

using Database;
using Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

public static class GetPersonList
{
    public sealed class Query : IRequest<List<TestingPerson>>
    {
        public readonly string FilterInput;
        public readonly string SortInput;
        public readonly IQueryKitProcessorConfiguration QueryKitConfig;

        public Query(string filterInput, string sortInput, IQueryKitProcessorConfiguration queryKitConfig = default)
        {
            FilterInput = filterInput;
            SortInput = sortInput;
            QueryKitConfig = queryKitConfig;
        }
    }

    public sealed class Handler : IRequestHandler<Query, List<TestingPerson>>
    {
        private readonly TestingDbContext _testingDbContext;

        public Handler(TestingDbContext testingDbContext)
        {
            _testingDbContext = testingDbContext;
        }

        public async Task<List<TestingPerson>> Handle(Query request, CancellationToken cancellationToken)
        {
            var queryablePeople = _testingDbContext.People;
            var appliedQueryable = queryablePeople
                .ApplyQueryKitFilter(request.FilterInput, request.QueryKitConfig)
                .ApplyQueryKitSort(request.SortInput, request.QueryKitConfig);
            return await appliedQueryable.ToListAsync(cancellationToken);
        }
    }
}