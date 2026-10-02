namespace QueryKit.IntegrationTests.Tests;

using System.Globalization;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SharedTestingHelper.Fakes;
using WebApiTestProject.Entities;

// A date in a list reads with the invariant culture, like a date in a single comparison.
// Before, th-TH read "2024-01-15" with the Thai calendar as 1481-01-15, so the list matched no row.
public class ListDateCultureTests : TestBase
{
    [Theory]
    [InlineData("th-TH")]
    [InlineData("fa-IR")]
    [InlineData("ar-SA")]
    public async Task date_only_in_a_list_matches_in_a_culture_with_another_calendar(string cultureName)
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var title = $"list date culture {Guid.NewGuid()}";
        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithTitle(title)
            .WithDate(new DateOnly(2024, 1, 15))
            .Build();
        var fakePersonTwo = new FakeTestingPersonBuilder()
            .WithTitle(title)
            .WithDate(new DateOnly(2024, 1, 16))
            .Build();
        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo);

        // Act
        var appliedQueryable = WithCulture(cultureName, () => testingServiceScope.DbContext().People
            .ApplyQueryKitFilter($"""Title == "{title}" && Date ^^ [2024-01-15]"""));
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(fakePersonOne.Id);
    }

    [Theory]
    [InlineData("th-TH")]
    [InlineData("fa-IR")]
    [InlineData("ar-SA")]
    public async Task date_time_offset_in_a_list_matches_in_a_culture_with_another_calendar(string cultureName)
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var title = $"list date culture {Guid.NewGuid()}";
        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithTitle(title)
            .WithSpecificDate(new DateTimeOffset(2024, 1, 15, 0, 0, 0, TimeSpan.Zero))
            .Build();
        var fakePersonTwo = new FakeTestingPersonBuilder()
            .WithTitle(title)
            .WithSpecificDate(new DateTimeOffset(2024, 1, 16, 0, 0, 0, TimeSpan.Zero))
            .Build();
        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo);

        // Act
        var appliedQueryable = WithCulture(cultureName, () => testingServiceScope.DbContext().People
            .ApplyQueryKitFilter($"""Title == "{title}" && SpecificDate ^^ [2024-01-15Z]"""));
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(fakePersonOne.Id);
    }

    private static TResult WithCulture<TResult>(string cultureName, Func<TResult> action)
    {
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo(cultureName);
            return action();
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }
}
