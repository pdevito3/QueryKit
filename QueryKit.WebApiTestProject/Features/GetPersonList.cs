namespace QueryKit.WebApiTestProject.Features;

using Database;
using Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

public static class GetPersonList
{
    public sealed class Query : IRequest<List<Person>>
    {
        // public readonly PersonParametersDto QueryParameters;
        //
        // public Query(PersonParametersDto queryParameters)
        // {
        //     QueryParameters = queryParameters;
        // }
    }

    public sealed class Handler : IRequestHandler<Query, List<Person>>
    {
        private readonly TestingDbContext _testingDbContext;

        public Handler(TestingDbContext testingDbContext)
        {
            _testingDbContext = testingDbContext;
        }

        public async Task<List<Person>> Handle(Query request, CancellationToken cancellationToken)
        {
            var queryablePeople = _testingDbContext.People;
            
            // TODO add filtering

            return await queryablePeople.ToListAsync(cancellationToken);
        }
    }
}