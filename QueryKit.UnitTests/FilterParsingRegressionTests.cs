namespace QueryKit.UnitTests;

using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using Exceptions;
using FluentAssertions;
using Operators;
using SharedTestingHelper.Fakes.Recipes;
using WebApiTestProject.Entities;
using WebApiTestProject.Entities.Recipes;

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
    [InlineData("Title ^^ [\"Warm, with syrup\", \"a\\b\"]", new[] { "Warm", "with syrup", "a\\b" })]
    [InlineData("Title !^^ [\"Warm, with syrup\", \"a\\b\"]", new[] { "Warm, with syrup" })]
    [InlineData("Title ^^* [\"WARM, WITH SYRUP\"]", new[] { "Warm", "with syrup" })]
    [InlineData("Title ^^ [\"\"\"Warm, with syrup\"\"\", \"Warm\"]", new[] { "Warm", "with syrup" })]
    [InlineData("Title ^^ [\" Warm \", \"with syrup \"]", new[] { "Warm", "with syrup" })]
    public void list_value_with_comma_is_split_into_items(string input, string[] expectedTitles)
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
    [InlineData("SpecificDateTime == 2024-01-15T08:00:00Z.5")]
    [InlineData("SpecificDate == 2024-01-15T10:00:00+02:00.500")]
    [InlineData("Time == 08:30:00.5")]
    [InlineData("Time == \"08:30:00.500\"")]
    [InlineData("Time ^^ [08:30:00.5]")]
    public void fractional_seconds_are_kept(string input)
    {
        var result = FractionalSecondPeople().AsQueryable().ApplyQueryKitFilter(input).ToList();

        result.Select(x => x.Title).Should().Equal("match");
    }

    [Theory]
    [InlineData("Time == \"08:30:00.5\"")]
    [InlineData("Time == \"08:30:00.50\"")]
    public void quoted_time_with_fewer_than_three_fraction_digits_drops_the_fraction(string input)
    {
        var result = FractionalSecondPeople().AsQueryable().ApplyQueryKitFilter(input).ToList();

        result.Select(x => x.Title).Should().Equal("whole second");
    }

    private static TestingPerson[] FractionalSecondPeople() => new[]
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

    [Theory]
    [InlineData("""Title @= "am" """)]
    [InlineData("""Title _= "la" """)]
    [InlineData("""Title _-= "mb" """)]
    [InlineData("""Title !@= "am" """)]
    [InlineData("""Title !_= "la" """)]
    [InlineData("""Title !_-= "mb" """)]
    public void case_sensitive_string_operator_on_null_property_throws_in_memory(string input)
    {
        var people = new[]
        {
            new TestingPerson { Title = null, FirstName = "null" },
            new TestingPerson { Title = "lamb", FirstName = "lamb" },
            new TestingPerson { Title = "other", FirstName = "other" },
        };

        var act = () => people.AsQueryable().ApplyQueryKitFilter(input).ToList();

        act.Should().Throw<NullReferenceException>();
    }

    public static IEnumerable<object[]> ComparisonOperatorFactories() =>
        typeof(ComparisonOperator).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(x => x.ReturnType == typeof(ComparisonOperator)
                && x.GetParameters().Select(p => p.Name).SequenceEqual(new[] { "caseInsensitive", "usesAll" }))
            .Select(x => new object[] { x.Name });

    [Theory]
    [MemberData(nameof(ComparisonOperatorFactories))]
    public void comparison_operator_factory_keeps_uses_all(string factoryName)
    {
        var factory = typeof(ComparisonOperator).GetMethod(factoryName, BindingFlags.Public | BindingFlags.Static)!;

        var comparisonOperator = (ComparisonOperator)factory.Invoke(null, new object[] { true, true })!;

        comparisonOperator.UsesAll.Should().BeTrue();
        comparisonOperator.CaseInsensitive.Should().BeTrue();
    }

    [Fact]
    public void comparison_operator_factory_has_one_test_case_per_operator_type()
    {
        ComparisonOperatorFactories().Should().HaveCount(24);
    }

    [Fact]
    public void comparison_operator_factory_with_uses_all_builds_all_expression()
    {
        Expression<Func<Recipe, IEnumerable<string>>> ingredientNames = x => x.Ingredients.Select(y => y.Name);

        var body = ComparisonOperator.EqualsOperator(usesAll: true)
            .GetExpression<Recipe>(ingredientNames.Body, Expression.Constant("waffle"), null);
        var filterExpression = Expression.Lambda<Func<Recipe, bool>>(body, ingredientNames.Parameters);

        filterExpression.ToDisplayString().Should()
            .Be(FilterParser.ParseFilter<Recipe>("""Ingredients.Name %== "waffle" """).ToDisplayString());
    }

    [Theory]
    [InlineData("Age  desc")]
    [InlineData("Age   desc")]
    [InlineData("Age\tdesc")]
    public void sort_direction_after_extra_white_space_is_read(string input)
    {
        var people = new[]
        {
            new TestingPerson { Title = "young", Age = 20 },
            new TestingPerson { Title = "old", Age = 40 },
        };

        var result = people.AsQueryable().ApplyQueryKitSort(input).ToList();

        result.Select(x => x.Title).Should().Equal("old", "young");
    }

    [Theory]
    [InlineData("""Tags ^$ "sweet" """, new[] { "pancakes" })]
    [InlineData("""Tags ^$* "WINNER" """, new[] { "bread" })]
    [InlineData("""Tags %^$ "dinner" """, new[] { "stew", "water" })]
    public void has_returns_matching_rows(string input, string[] expectedTitles)
    {
        var recipes = new[]
        {
            new FakeRecipeBuilder().WithTitle("pancakes").Build().SetTags(["breakfast", "sweet"]),
            new FakeRecipeBuilder().WithTitle("stew").Build().SetTags(["dinner"]),
            new FakeRecipeBuilder().WithTitle("bread").Build().SetTags(["bread", "Winner"]),
            new FakeRecipeBuilder().WithTitle("water").Build().SetTags([]),
        };

        var result = recipes.AsQueryable().ApplyQueryKitFilter(input).ToList();

        result.Select(x => x.Title).Should().BeEquivalentTo(expectedTitles);
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
