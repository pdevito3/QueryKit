namespace QueryKit.UnitTests;

using System.Globalization;
using Exceptions;
using FluentAssertions;
using WebApiTestProject.Entities.Recipes;

// In a culture whose decimal separator is not '.', v1.14.2 read only the part of a '.' number before the '.'.
// A '.' number that does not convert to the property type gives the same exception type as v1.14.2.
public class DotNumberCultureTests
{
    [Theory]
    [InlineData("de-DE", "Rating > 4.4")]
    [InlineData("fr-FR", "Rating > 4.4")]
    [InlineData("de-DE", "Rating == -4.0")]
    [InlineData("de-DE", "Rating > 4.4 || Title == \"x\"")]
    [InlineData("de-DE", "Rating == .5")]
    [InlineData("de-DE", "Rating ^^ [4.0]")]
    [InlineData("de-DE", "Rating ^^ [\"x\", 4.0]")]
    [InlineData("de-DE", "Ingredients.MinimumQuality == 0.0")]
    [InlineData("de-DE", "Ingredients.QualityLevel > 4.5")]
    [InlineData("de-DE", "Tags #== 2.0")]
    [InlineData("de-DE", "(Rating, Title) == 4.0")]
    public void dot_number_on_an_integer_property_throws_parsing_exception(string cultureName, string input)
    {
        var act = () => WithCulture(cultureName, () => FilterParser.ParseFilter<Recipe>(input));

        act.Should().ThrowExactly<ParsingException>();
    }

    [Theory]
    [InlineData("en-US", "Rating > 4.4")]
    [InlineData("de-DE", "Rating > \"4.4\"")]
    [InlineData("de-DE", "Rating ^^ [\"4.0\"]")]
    [InlineData("de-DE", "Rating > @4.4")]
    [InlineData("de-DE", "HaveMadeItMyself == 4.4")]
    public void number_that_v1_14_2_also_converted_throws_format_exception(string cultureName, string input)
    {
        var act = () => WithCulture(cultureName, () => FilterParser.ParseFilter<Recipe>(input));

        act.Should().ThrowExactly<FormatException>();
    }

    [Fact]
    public void integer_value_still_filters_in_a_comma_culture()
    {
        var filterExpression = WithCulture("de-DE", () => FilterParser.ParseFilter<Recipe>("Rating > 4"));

        filterExpression.ToString().Should().Be("x => (x.Rating > 4)");
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
