namespace QueryKit.IntegrationTests.Tests;

using System.Globalization;
using Configuration;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SharedTestingHelper.Fakes;
using WebApiTestProject.Entities;

// A query name ignores case with the rules of the invariant culture, so the result does not depend on the culture of the parse.
public class AliasCultureTests : TestBase
{
    [Fact]
    public async Task query_name_with_i_matches_its_upper_case_in_tr_tr()
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
