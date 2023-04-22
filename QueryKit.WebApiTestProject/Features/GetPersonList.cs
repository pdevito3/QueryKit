namespace QueryKit.WebApiTestProject.Features;

using Database;
using Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

public static class GetPersonList
{
    public sealed class Query : IRequest<List<TestingPerson>>
    {
        public readonly string Input;
        public readonly IQueryKitProcessorConfiguration FilterConfig;

        public Query(string input, IQueryKitProcessorConfiguration filterConfig = default)
        {
            Input = input;
            FilterConfig = filterConfig;
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
            
            var appliedQueryable = queryablePeople.ApplyQueryKitFilter(request.Input, request.FilterConfig);
            
            return await appliedQueryable.ToListAsync(cancellationToken);
        }
    }
}