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
    [InlineData("fr-FR")]
    public void decimal_value_with_culture_decimal_comma_is_read_as_before(string cultureName)
    {
        var input = """Rating > 4,5""";

        var filterExpression = WithCulture(cultureName, () => FilterParser.ParseFilter<TestingPerson>(input));

        filterExpression.ToDisplayString().Should().Be("x => (x.Rating > 45)");
    }

    [Fact]
    public void decimal_value_with_decimal_comma_is_not_accepted_in_a_culture_with_decimal_point()
    {
        var input = """Rating > 4,5""";

        var act = () => WithCulture("en-US", () => FilterParser.ParseFilter<TestingPerson>(input));

        act.Should().Throw<QueryKitException>();
    }

    [Theory]
    [InlineData("de-DE")]
    [InlineData("en-US")]
    public void list_numbers_are_separated_by_commas_in_every_culture(string cultureName)
    {
        var people = new[]
        {
            new TestingPerson { Title = "four", Rating = 4m },
            new TestingPerson { Title = "four and a half", Rating = 4.5m },
            new TestingPerson { Title = "five", Rating = 5m },
        };

        var result = WithCulture(cultureName, () => people.AsQueryable().ApplyQueryKitFilter("Rating ^^ [4,5]").ToList());

        result.Select(x => x.Title).Should().Equal("four", "five");
    }

    [Theory]
    [InlineData("de-DE")]
    [InlineData("en-US")]
    public void list_number_uses_invariant_culture(string cultureName)
    {
        var people = new[]
        {
            new TestingPerson { Title = "four", Rating = 4m },
            new TestingPerson { Title = "four and a half", Rating = 4.5m },
        };

        var result = WithCulture(cultureName, () => people.AsQueryable().ApplyQueryKitFilter("Rating ^^ [4.5, 6]").ToList());

        result.Select(x => x.Title).Should().Equal("four and a half");
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
    [InlineData("Title ^^ [\" Warm \", \"with syrup \"]", new[] { "Warm", "with syrup" })]
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
