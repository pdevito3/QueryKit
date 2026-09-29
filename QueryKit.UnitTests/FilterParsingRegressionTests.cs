namespace QueryKit.UnitTests;

using System.Globalization;
using Exceptions;
using FluentAssertions;
using WebApiTestProject.Entities;

public class FilterParsingRegressionTests
{
    [Theory]
    [InlineData("de-DE")]
    [InlineData("fr-FR")]
    [InlineData("en-US")]
    public void decimal_value_uses_invariant_culture(string cultureName)
    {
        var input = """Rating > 4.5""";

        var filterExpression = WithCulture(cultureName, () => FilterParser.ParseFilter<TestingPerson>(input));

        filterExpression.ToDisplayString().Should().Be("x => (x.Rating > 4.5)");
    }

    [Theory]
    [InlineData("de-DE")]
    [InlineData("en-US")]
    public void decimal_value_with_culture_decimal_comma_is_not_accepted(string cultureName)
    {
        var input = """Rating > 4,5""";

        var act = () => WithCulture(cultureName, () => FilterParser.ParseFilter<TestingPerson>(input));

        act.Should().Throw<QueryKitException>();
    }

    [Fact]
    public void decimal_value_uses_invariant_culture_in_memory()
    {
        var people = new[]
        {
            new TestingPerson { Title = "low", Rating = 4.4m },
            new TestingPerson { Title = "high", Rating = 4.6m },
        };

        var result = WithCulture("de-DE", () => people.AsQueryable().ApplyQueryKitFilter("Rating > 4.5").ToList());

        result.Select(x => x.Title).Should().Equal("high");
    }

    [Theory]
    [InlineData("Title ^^ [\"Warm, with syrup\", \"a\\b\"]", new[] { "Warm, with syrup", "a\\b" })]
    [InlineData("Title !^^ [\"Warm, with syrup\", \"a\\b\"]", new[] { "Warm", "with syrup" })]
    [InlineData("Title ^^* [\"WARM, WITH SYRUP\"]", new[] { "Warm, with syrup" })]
    [InlineData("Title ^^ [\"\"\"Warm, with syrup\"\"\", \"Warm\"]", new[] { "Warm, with syrup", "Warm" })]
    public void list_value_with_comma_is_one_item(string input, string[] expectedTitles)
    {
        var people = new[]
        {
            new TestingPerson { Title = "Warm, with syrup" },
            new TestingPerson { Title = "Warm" },
            new TestingPerson { Title = "with syrup" },
            new TestingPerson { Title = "a\\b" },
        };

        var result = people.AsQueryable().ApplyQueryKitFilter(input).ToList();

        result.Select(x => x.Title).Should().BeEquivalentTo(expectedTitles);
    }

    [Fact]
    public void enum_list_value_is_split_into_items()
    {
        var people = new[]
        {
            new TestingPerson { Title = "jan", BirthMonth = BirthMonthEnum.January },
            new TestingPerson { Title = "feb", BirthMonth = BirthMonthEnum.February },
            new TestingPerson { Title = "mar", BirthMonth = BirthMonthEnum.March },
        };

        var result = people.AsQueryable().ApplyQueryKitFilter("""BirthMonth ^^ ["January", "March"]""").ToList();

        result.Select(x => x.Title).Should().BeEquivalentTo("jan", "mar");
    }

    [Theory]
    [InlineData("SpecificDateTime == 2024-01-15T08:00:00.500Z")]
    [InlineData("SpecificDateTime == 2024-01-15T08:00:00.5Z")]
    [InlineData("SpecificDateTime == \"2024-01-15T08:00:00.5Z\"")]
    [InlineData("SpecificDateTime ^^ [2024-01-15T08:00:00.5Z]")]
    [InlineData("SpecificDate == 2024-01-15T10:00:00.5+02:00")]
    [InlineData("SpecificDate == 2024-01-15T08:00:00.5000000Z")]
    [InlineData("Time == 08:30:00.5")]
    [InlineData("Time == \"08:30:00.5\"")]
    [InlineData("Time == \"08:30:00.50\"")]
    [InlineData("Time ^^ [08:30:00.5]")]
    public void fractional_seconds_are_kept(string input)
    {
        var people = new[]
        {
            new TestingPerson
            {
                Title = "match",
                SpecificDateTime = new DateTime(2024, 1, 15, 8, 0, 0, 500, DateTimeKind.Utc),
                SpecificDate = new DateTimeOffset(2024, 1, 15, 8, 0, 0, 500, TimeSpan.Zero),
                Time = new TimeOnly(8, 30, 0, 500),
            },
            new TestingPerson
            {
                Title = "whole second",
                SpecificDateTime = new DateTime(2024, 1, 15, 8, 0, 0, DateTimeKind.Utc),
                SpecificDate = new DateTimeOffset(2024, 1, 15, 8, 0, 0, TimeSpan.Zero),
                Time = new TimeOnly(8, 30, 0),
            },
        };

        var result = people.AsQueryable().ApplyQueryKitFilter(input).ToList();

        result.Select(x => x.Title).Should().Equal("match");
    }

    [Fact]
    public void time_fraction_keeps_microseconds()
    {
        var filterExpression = FilterParser.ParseFilter<TestingPerson>("Time == 08:30:00.123456");

        filterExpression.ToDisplayString().Should().Be("x => (x.Time == new Nullable`1(new TimeOnly(8, 30, 0, 123, 456)))");
    }

    [Theory]
    [InlineData("SpecificDateTime == 2024-01-15T08:00:00", "x => (x.SpecificDateTime == new DateTime(638409024000000000, Utc))")]
    [InlineData("SpecificDateTime == \"2024-01-15T08:00:00\"", "x => (x.SpecificDateTime == new DateTime(638409024000000000, Utc))")]
    [InlineData("SpecificDateTime == 2024-01-15T10:00:00+02:00", "x => (x.SpecificDateTime == new DateTime(638409024000000000, Utc))")]
    [InlineData("SpecificDateTime == 2024-01-15T08:00:00Z", "x => (x.SpecificDateTime == new DateTime(638409024000000000, Utc))")]
    [InlineData("SpecificDate == 2024-01-15T08:00:00", "x => (x.SpecificDate == new Nullable`1(new DateTimeOffset(638409024000000000, 00:00:00)))")]
    [InlineData("SpecificDate == 2024-01-15T10:00:00+02:00", "x => (x.SpecificDate == new Nullable`1(new DateTimeOffset(638409024000000000, 00:00:00)))")]
    public void date_time_without_offset_is_utc(string input, string expected)
    {
        var filterExpression = FilterParser.ParseFilter<TestingPerson>(input);

        filterExpression.ToDisplayString().Should().Be(expected);
    }

    [Theory]
    [InlineData("SpecificDateTime ^^ [2024-01-15T08:00:00]")]
    [InlineData("SpecificDateTime ^^ [2024-01-15T10:00:00+02:00]")]
    [InlineData("SpecificDate ^^ [2024-01-15T08:00:00]")]
    [InlineData("SpecificDate ^^ [2024-01-15T10:00:00+02:00]")]
    public void date_time_list_value_matches_scalar_value(string input)
    {
        var people = new[]
        {
            new TestingPerson
            {
                Title = "match",
                SpecificDateTime = new DateTime(2024, 1, 15, 8, 0, 0, DateTimeKind.Utc),
                SpecificDate = new DateTimeOffset(2024, 1, 15, 8, 0, 0, TimeSpan.Zero),
            },
            new TestingPerson
            {
                Title = "other",
                SpecificDateTime = new DateTime(2024, 1, 15, 9, 0, 0, DateTimeKind.Utc),
                SpecificDate = new DateTimeOffset(2024, 1, 15, 9, 0, 0, TimeSpan.Zero),
            },
        };

        var result = people.AsQueryable().ApplyQueryKitFilter(input).ToList();
        var scalarResult = people.AsQueryable().ApplyQueryKitFilter(input.Replace("^^ [", "== ").TrimEnd(']')).ToList();

        result.Select(x => x.Title).Should().Equal("match");
        scalarResult.Select(x => x.Title).Should().Equal("match");
    }

    [Theory]
    [InlineData("Age == Rating", "equal")]
    [InlineData("Rating == Age", "equal")]
    [InlineData("Age != Rating", "different")]
    [InlineData("Rating != Age", "different")]
    public void int_property_compares_to_decimal_property(string input, string expectedTitle)
    {
        var people = new[]
        {
            new TestingPerson { Title = "equal", Age = 4, Rating = 4m },
            new TestingPerson { Title = "different", Age = 4, Rating = 4.5m },
        };

        var result = people.AsQueryable().ApplyQueryKitFilter(input).ToList();

        result.Select(x => x.Title).Should().Equal(expectedTitle);
    }

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
