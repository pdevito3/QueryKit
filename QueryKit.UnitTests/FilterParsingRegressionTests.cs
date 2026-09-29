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
