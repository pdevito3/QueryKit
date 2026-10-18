namespace QueryKit.UnitTests;

using System.Globalization;
using Configuration;
using Exceptions;
using FluentAssertions;
using WebApiTestProject.Entities.Recipes;

// A query name ignores case with the rules of the invariant culture, so the result does not depend on the culture of the parse.
// Like v1.14.2, an operator alias matches with the case rules of the culture of each parse,
// also when a parse in another culture used the same alias before.
// Each test uses its own alias, so the result does not depend on the order of the tests.
public class AliasCultureTests
{
    [Theory]
    [InlineData("en-US")]
    [InlineData("tr-TR")]
    public void query_name_with_i_matches_its_upper_case_with_the_invariant_rules_in_every_culture(string cultureName)
    {
        var config = new QueryKitConfiguration(settings =>
        {
            settings.Property<Recipe>(x => x.Rating!).HasQueryName("tipalpha");
        });

        var filterExpression = WithCulture(cultureName, () => FilterParser.ParseFilter<Recipe>("TIPALPHA > 3", config));

        filterExpression.ToString().Should().Be("x => (x.Rating > 3)");
    }

    [Theory]
    [InlineData("en-US", "TIPBETA > 3", "x => (x.Rating > 3)")]
    [InlineData("tr-TR", "TIPBETA > 3", "x => (x.Rating > 3)")]
    [InlineData("tr-TR", "(TIPBETA, Title) == 3", "x => ((x.Rating == 3) OrElse (x.Title == \"3\"))")]
    [InlineData("tr-TR", "TIPBETA desc", null)]
    public void query_name_with_i_matches_its_upper_case_in_tr_tr_in_every_syntax(string cultureName, string input, string? expected)
    {
        var config = new QueryKitConfiguration(settings =>
        {
            settings.Property<Recipe>(x => x.Rating!).HasQueryName("tipbeta");
        });

        if (expected == null)
        {
            var sortExpressions = WithCulture(cultureName, () => SortParser.ParseSort<Recipe>(input, config));
            sortExpressions.Should().ContainSingle();
            sortExpressions[0].Expression!.ToString().Should().Contain("x.Rating");
            return;
        }

        var filterExpression = WithCulture(cultureName, () => FilterParser.ParseFilter<Recipe>(input, config));
        filterExpression.ToString().Should().Be(expected);
    }

    [Theory]
    [InlineData("tıp", "TIP > 3")]
    [InlineData("tip", "TİP > 3")]
    public void query_name_does_not_match_with_the_dotted_and_dotless_i_rules_of_tr_tr(string queryName, string input)
    {
        var config = new QueryKitConfiguration(settings =>
        {
            settings.Property<Recipe>(x => x.Rating!).HasQueryName(queryName);
        });

        var turkish = () => WithCulture("tr-TR", () => FilterParser.ParseFilter<Recipe>(input, config));

        turkish.Should().Throw<UnknownFilterPropertyException>();
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
