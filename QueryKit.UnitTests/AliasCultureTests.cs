namespace QueryKit.UnitTests;

using System.Globalization;
using Configuration;
using Exceptions;
using FluentAssertions;
using WebApiTestProject.Entities.Recipes;

// Like v1.14.2, an alias matches with the case rules of the culture of each parse,
// also when a parse in another culture used the same alias before.
// Each test uses its own alias, so the result does not depend on the order of the tests.
public class AliasCultureTests
{
    [Fact]
    public void query_name_matches_in_en_us_after_a_tr_tr_parse()
    {
        var config = new QueryKitConfiguration(settings =>
        {
            settings.Property<Recipe>(x => x.Rating!).HasQueryName("tipalpha");
        });
        var input = "TIPALPHA > 3";

        var turkish = () => WithCulture("tr-TR", () => FilterParser.ParseFilter<Recipe>(input, config));
        turkish.Should().Throw<UnknownFilterPropertyException>();

        var filterExpression = WithCulture("en-US", () => FilterParser.ParseFilter<Recipe>(input, config));
        filterExpression.ToString().Should().Be("x => (x.Rating > 3)");
    }

    [Fact]
    public void query_name_does_not_match_in_tr_tr_after_an_en_us_parse()
    {
        var config = new QueryKitConfiguration(settings =>
        {
            settings.Property<Recipe>(x => x.Rating!).HasQueryName("tipbeta");
        });
        var input = "TIPBETA > 3";

        var filterExpression = WithCulture("en-US", () => FilterParser.ParseFilter<Recipe>(input, config));
        filterExpression.ToString().Should().Be("x => (x.Rating > 3)");

        var turkish = () => WithCulture("tr-TR", () => FilterParser.ParseFilter<Recipe>(input, config));
        turkish.Should().Throw<UnknownFilterPropertyException>();
    }

    [Theory]
    [InlineData("tıp", "TIP > 3")]
    [InlineData("tip", "TİP > 3")]
    [InlineData("tıp", "(TIP, Title) == 3")]
    public void query_name_matches_with_the_case_rules_of_tr_tr(string queryName, string input)
    {
        var config = new QueryKitConfiguration(settings =>
        {
            settings.Property<Recipe>(x => x.Rating).HasQueryName(queryName);
        });

        var filterExpression = WithCulture("tr-TR", () => FilterParser.ParseFilter<Recipe>(input, config));

        filterExpression.ToString().Should().Contain("x.Rating");
    }

    [Fact]
    public void operator_alias_does_not_match_in_tr_tr_after_an_en_us_parse()
    {
        var config = new QueryKitConfiguration(settings =>
        {
            settings.EqualsOperator = "eşitgamma";
        });
        var input = """Title EŞITGAMMA "Pancakes" """;

        var filterExpression = WithCulture("en-US", () => FilterParser.ParseFilter<Recipe>(input, config));
        filterExpression.ToString().Should().Be("""x => (x.Title == "Pancakes")""");

        var turkish = () => WithCulture("tr-TR", () => FilterParser.ParseFilter<Recipe>(input, config));
        turkish.Should().Throw<ParsingException>();
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
