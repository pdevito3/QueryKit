namespace QueryKit.IntegrationTests.Tests;

using System.Globalization;
using Configuration;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SharedTestingHelper.Fakes;
using WebApiTestProject.Entities;

public class FilterParsingRegressionTests : TestBase
{
    [Fact]
    public async Task operator_alias_text_inside_quoted_value_is_kept()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var title = $"salt and pepper or eq {Guid.NewGuid()}";
        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithTitle(title)
            .Build();
        var fakePersonTwo = new FakeTestingPersonBuilder().Build();
        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo);

        var input = $"""{nameof(TestingPerson.Title)} eq "{title}" and {nameof(TestingPerson.Id)} neq "{Guid.NewGuid()}" """;
        var config = new QueryKitConfiguration(settings =>
        {
            settings.EqualsOperator = "eq";
            settings.NotEqualsOperator = "neq";
            settings.AndOperator = "and";
            settings.OrOperator = "or";
        });

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input, config);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(fakePersonOne.Id);
    }

    [Fact]
    public async Task decimal_value_uses_invariant_culture()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var title = $"culture {Guid.NewGuid()}";
        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithTitle(title)
            .WithRating(4.6m)
            .Build();
        var fakePersonTwo = new FakeTestingPersonBuilder()
            .WithTitle(title)
            .WithRating(4.4m)
            .Build();
        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo);

        var input = $"""{nameof(TestingPerson.Title)} == "{title}" && {nameof(TestingPerson.Rating)} > 4.5""";

        // Act
        var originalCulture = CultureInfo.CurrentCulture;
        IQueryable<TestingPerson> appliedQueryable;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            appliedQueryable = testingServiceScope.DbContext().People.ApplyQueryKitFilter(input);
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
