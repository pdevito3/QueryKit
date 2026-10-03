namespace QueryKit.UnitTests;

using System.Globalization;
using FluentAssertions;
using WebApiTestProject.Entities;

// A date in a list reads with the invariant culture, like a date in a single comparison.
// The calendar of ar-SA, th-TH, and fa-IR read "2024-01-15" as a different date or as no date.
public class ListDateCultureTests
{
    [Theory]
    [InlineData("en-US")]
    [InlineData("ar-SA")]
    [InlineData("th-TH")]
    [InlineData("fa-IR")]
    public void date_only_in_a_list_matches_in_every_culture(string cultureName)
    {
        var person = new TestingPerson { Date = new DateOnly(2024, 1, 15) };

        var matches = WithCulture(cultureName, () => Matches(person, "Date ^^ [2024-01-15]"));

        matches.Should().BeTrue();
    }

    [Theory]
    [InlineData("en-US")]
    [InlineData("ar-SA")]
    [InlineData("th-TH")]
    [InlineData("fa-IR")]
    public void date_time_offset_in_a_list_matches_in_every_culture(string cultureName)
    {
        var person = new TestingPerson
        {
            SpecificDate = DateTimeOffset.Parse("2024-01-15 10:00:00+02:00", CultureInfo.InvariantCulture)
        };

        var matches = WithCulture(cultureName, () => Matches(person, "SpecificDate ^^ [2024-01-15T10:00:00+02:00, 2024-01-16]"));
        var dateOnlyMatches = WithCulture(cultureName, () => Matches(
            new TestingPerson { SpecificDate = new DateTimeOffset(2024, 1, 16, 0, 0, 0, TimeSpan.Zero) },
            "SpecificDate ^^ [2024-01-16]"));

        matches.Should().BeTrue();
        dateOnlyMatches.Should().BeTrue();
    }

    [Theory]
    [InlineData("en-US")]
    [InlineData("ar-SA")]
    [InlineData("th-TH")]
    [InlineData("fa-IR")]
    public void time_only_in_a_list_matches_in_every_culture(string cultureName)
    {
        var person = new TestingPerson { Time = new TimeOnly(8, 30, 0) };

        var matches = WithCulture(cultureName, () => Matches(person, "Time ^^ [08:30:00]"));

        matches.Should().BeTrue();
    }

    private static bool Matches(TestingPerson person, string filter)
        => new[] { person }.ApplyQueryKitFilter(filter).Any();

    private static TResult WithCulture<TResult>(string cultureName, Func<TResult> action)
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo(cultureName);
            CultureInfo.CurrentUICulture = new CultureInfo(cultureName);
            return action();
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }
}
