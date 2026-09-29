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
