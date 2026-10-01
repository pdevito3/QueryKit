namespace QueryKit.IntegrationTests.Tests;

using Configuration;
using Exceptions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SharedTestingHelper.Fakes;
using WebApiTestProject.Entities;

// Like v1.14.2, a failed filter with a query name that has a space throws UnknownFilterPropertyException
// for the first word. The same query name in a filter that does not fail still filters the rows.
public class QueryNameOverUnknownTests : TestBase
{
    [Fact]
    public async Task failed_filter_with_a_spaced_query_name_throws_unknown_property()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var title = $"query name {Guid.NewGuid()}";
        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithTitle(title)
            .WithAge(30)
            .Build();
        var fakePersonTwo = new FakeTestingPersonBuilder()
            .WithTitle(title)
            .WithAge(10)
            .Build();
        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo);

        var config = new QueryKitConfiguration(config =>
        {
            config.CustomOperation<TestingPerson>((x, op, value) => x.Age > 17).HasQueryName("is adult");
        });

        // Act
        var failedFilter = () => testingServiceScope.DbContext().People
            .ApplyQueryKitFilter("""is adult == true && Title ==""", config);
        var appliedQueryable = testingServiceScope.DbContext().People
            .ApplyQueryKitFilter($"""is adult == true && Title == "{title}" """, config);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        failedFilter.Should().ThrowExactly<UnknownFilterPropertyException>().WithMessage("*'is'*");
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(fakePersonOne.Id);
    }
}
