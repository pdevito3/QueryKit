namespace QueryKit.IntegrationTests.Tests;

using System.Globalization;
using Configuration;
using Exceptions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SharedTestingHelper.Fakes;
using WebApiTestProject.Entities;

// Like v1.14.2, a query name matches with the case rules of the culture of each parse,
// also when a parse in another culture used the same query name before.
public class AliasCultureTests : TestBase
{
    [Fact]
    public async Task query_name_matches_in_en_us_after_a_tr_tr_parse()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var title = $"culture {Guid.NewGuid()}";
        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithTitle(title)
            .Build();
        var fakePersonTwo = new FakeTestingPersonBuilder()
            .Build();
        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo);

        var input = $"""ISIMDELTA == "{title}" """;
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<TestingPerson>(x => x.Title!).HasQueryName("isimdelta");
        });

        // Act
        var originalCulture = CultureInfo.CurrentCulture;
        IQueryable<TestingPerson> appliedQueryable;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
            var turkish = () => testingServiceScope.DbContext().People.ApplyQueryKitFilter(input, config);
            turkish.Should().Throw<UnknownFilterPropertyException>();

            CultureInfo.CurrentCulture = new CultureInfo("en-US");
            appliedQueryable = testingServiceScope.DbContext().People.ApplyQueryKitFilter(input, config);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(fakePersonOne.Id);
    }
}
