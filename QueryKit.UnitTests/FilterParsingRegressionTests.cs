namespace QueryKit.UnitTests;

using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using Configuration;
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
    [InlineData("en-US")]
    public void decimal_value_with_decimal_comma_is_not_accepted(string cultureName)
    {
        var input = """Rating > 4,5""";

        var act = () => WithCulture(cultureName, () => FilterParser.ParseFilter<TestingPerson>(input));

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
    [InlineData("Time == \"08:30:00.5\"")]
    [InlineData("Time == \"08:30:00.50\"")]
    [InlineData("Time ^^ [08:30:00.5]")]
    public void fractional_seconds_are_kept(string input)
    {
        var result = FractionalSecondPeople().AsQueryable().ApplyQueryKitFilter(input).ToList();

        result.Select(x => x.Title).Should().Equal("match");
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
    [InlineData("""Title @= "am" """, new[] { "lamb" })]
    [InlineData("""Title _= "la" """, new[] { "lamb" })]
    [InlineData("""Title _-= "mb" """, new[] { "lamb" })]
    [InlineData("""Title !@= "am" """, new[] { "null", "other" })]
    [InlineData("""Title !_= "la" """, new[] { "null", "other" })]
    [InlineData("""Title !_-= "mb" """, new[] { "null", "other" })]
    public void case_sensitive_string_operator_handles_null_property(string input, string[] expectedFirstNames)
    {
        var people = new[]
        {
            new TestingPerson { Title = null, FirstName = "null" },
            new TestingPerson { Title = "lamb", FirstName = "lamb" },
            new TestingPerson { Title = "other", FirstName = "other" },
        };

        var result = people.AsQueryable().ApplyQueryKitFilter(input).ToList();

        result.Select(x => x.FirstName).Should().Equal(expectedFirstNames);
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

    [Theory]
    [InlineData(false, new[] { "lamb" })]
    [InlineData(true, new[] { "null", "other" })]
    public void case_insensitive_in_operator_factory_reads_a_constant_list(bool notIn, string[] expectedFirstNames)
    {
        var people = new[]
        {
            new TestingPerson { Title = null, FirstName = "null" },
            new TestingPerson { Title = "Lamb", FirstName = "lamb" },
            new TestingPerson { Title = "other", FirstName = "other" },
        };
        Expression<Func<TestingPerson, string?>> title = x => x.Title;
        var comparisonOperator = notIn ? ComparisonOperator.NotInOperator(true) : ComparisonOperator.InOperator(true);

        var body = comparisonOperator.GetExpression<TestingPerson>(title.Body, Expression.Constant(new List<string> { "LAMB" }), null);
        var filterExpression = Expression.Lambda<Func<TestingPerson, bool>>(body, title.Parameters);

        people.AsQueryable().Where(filterExpression).Select(x => x.FirstName).Should().Equal(expectedFirstNames);
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
    [InlineData("""Tags !^$ "sweet" """, new[] { "stew", "bread", "water" })]
    [InlineData("""Tags ^$* "WINNER" """, new[] { "bread" })]
    [InlineData("""Tags !^$* "WINNER" """, new[] { "pancakes", "stew", "water" })]
    [InlineData("""Tags %^$ "dinner" """, new[] { "stew", "water" })]
    [InlineData("""Tags %!^$ "dinner" """, new[] { "pancakes", "bread" })]
    public void has_and_does_not_have_return_matching_rows(string input, string[] expectedTitles)
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

    [Theory]
    [InlineData("""(Title == "pancakes") """, """(Title == "pancakes")""")]
    [InlineData("(Title == \"pancakes\")\t\n", """(Title == "pancakes")""")]
    [InlineData("""((Title == "pancakes")) """, """((Title == "pancakes"))""")]
    [InlineData("""((Title == "pancakes") ) """, """((Title == "pancakes"))""")]
    [InlineData("""Title == "stew" || (Title == "pancakes") """, """Title == "stew" || (Title == "pancakes")""")]
    [InlineData(""" (Title == "pancakes")""", """(Title == "pancakes")""")]
    public void whitespace_around_a_group_gives_the_same_filter(string input, string inputWithoutWhitespace)
    {
        var recipes = new[]
        {
            new FakeRecipeBuilder().WithTitle("pancakes").Build(),
            new FakeRecipeBuilder().WithTitle("stew").Build(),
            new FakeRecipeBuilder().WithTitle("bread").Build(),
        };

        var filterExpression = FilterParser.ParseFilter<Recipe>(input);
        var expectedExpression = FilterParser.ParseFilter<Recipe>(inputWithoutWhitespace);
        var result = recipes.AsQueryable().ApplyQueryKitFilter(input).ToList();
        var expectedResult = recipes.AsQueryable().ApplyQueryKitFilter(inputWithoutWhitespace).ToList();

        filterExpression.ToDisplayString().Should().Be(expectedExpression.ToDisplayString());
        result.Should().Equal(expectedResult);
    }

    [Fact]
    public void unknown_logical_operator_throws_query_kit_parsing_exception()
    {
        var act = () => LogicalOperator.GetByOperatorString("xor");

        act.Should().Throw<QueryKitParsingException>().WithMessage("Operator xor is not supported");
    }

    [Theory]
    [InlineData("Age sideways")]
    [InlineData("Title, Age up")]
    public void invalid_sort_direction_throws_query_kit_parsing_exception(string input)
    {
        var act = () => SortParser.ParseSort<TestingPerson>(input);

        act.Should().Throw<QueryKitParsingException>().WithMessage("Invalid direction: *");
    }

    [Theory]
    [InlineData("Title @= null")]
    [InlineData("Title _= null")]
    [InlineData("Title _-= null")]
    [InlineData("Title !@= null")]
    [InlineData("Title !_= null")]
    [InlineData("Title !_-= null")]
    [InlineData("Title @=* null")]
    [InlineData("Title !_-=* null")]
    public void string_operator_with_null_value_throws_querykit_exception(string input)
    {
        var act = () => FilterParser.ParseFilter<TestingPerson>(input);

        act.Should().Throw<QueryKitParsingException>();
    }

    [Theory]
    [InlineData("SpecificDateTime == 2024-01-15T08:00:00", "x => (x.SpecificDateTime == new DateTime(638409024000000000, Utc))")]
    [InlineData("SpecificDateTime == \"2024-01-15T08:00:00\"", "x => (x.SpecificDateTime == new DateTime(638409024000000000, Utc))")]
    [InlineData("SpecificDateTime == 2024-01-15T10:00:00+02:00", "x => (x.SpecificDateTime == new DateTime(638409024000000000, Utc))")]
    [InlineData("SpecificDateTime == 2024-01-15T08:00:00Z", "x => (x.SpecificDateTime == new DateTime(638409024000000000, Utc))")]
    [InlineData("SpecificDate == 2024-01-15T08:00:00", "x => (x.SpecificDate == new Nullable`1(new DateTimeOffset(638409024000000000, 00:00:00)))")]
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
    [InlineData("""Age == "abc" """)]
    [InlineData("""Age == abc""")]
    [InlineData("""Rating > "abc" """)]
    [InlineData("""Rating > abc""")]
    [InlineData("""Age == 99999999999""")]
    [InlineData("""Id == "abc" """)]
    [InlineData("""SpecificDateTime == "abc" """)]
    [InlineData("""Favorite == "abc" """)]
    [InlineData("""Age ^^ ["abc"]""")]
    [InlineData("""BirthMonth == "Bogus" """)]
    [InlineData("""BirthMonth ^^ ["Bogus"]""")]
    [InlineData("""BirthMonth ^^ [Bogus]""")]
    public void invalid_value_throws_parsing_exception(string input)
    {
        var act = () => FilterParser.ParseFilter<TestingPerson>(input);

        act.Should().Throw<ParsingException>();
    }

    [Theory]
    [InlineData("""BirthMonth ^^ ["Bogus"]""", "Bogus", "BirthMonthEnum", "BirthMonth")]
    [InlineData("""BirthMonth ^^ [Bogus]""", "Bogus", "BirthMonthEnum", "BirthMonth")]
    [InlineData("""BirthMonth ^^ [January, Bogus]""", "Bogus", "BirthMonthEnum", "BirthMonth")]
    [InlineData("""BirthMonth == "Bogus" """, "Bogus", "BirthMonthEnum", "BirthMonth")]
    [InlineData("""BirthMonth == Bogus""", "Bogus", "BirthMonthEnum", "BirthMonth")]
    [InlineData("""Age == "abc" """, "abc", "Int32", "Age")]
    [InlineData("""Age == 99999999999""", "99999999999", "Int32", "Age")]
    [InlineData("""Age ^^ [1, abc]""", "abc", "Int32", "Age")]
    [InlineData("""Rating > abc""", "abc", "Decimal", "Rating")]
    [InlineData("""Id == "abc" """, "abc", "Guid", "Id")]
    [InlineData("""SpecificDateTime == "abc" """, "abc", "DateTime", "SpecificDateTime")]
    [InlineData("""Favorite == "abc" """, "abc", "Boolean", "Favorite")]
    public void invalid_value_message_names_the_value_the_type_and_the_property(string input, string value, string type, string property)
    {
        var act = () => FilterParser.ParseFilter<TestingPerson>(input);

        act.Should().ThrowExactly<ParsingException>()
            .WithMessage($"The value '{value}' is not a valid {type} for the filter property '{property}'.");
    }

    [Fact]
    public void invalid_value_message_names_the_query_name()
    {
        var config = new QueryKitConfiguration(settings =>
        {
            settings.Property<TestingPerson>(x => x.BirthMonth!).HasQueryName("month");
        });

        var act = () => FilterParser.ParseFilter<TestingPerson>("""month ^^ ["Bogus"]""", config);

        act.Should().ThrowExactly<ParsingException>()
            .WithMessage("The value 'Bogus' is not a valid BirthMonthEnum for the filter property 'month'.")
            .WithInnerExceptionExactly<ArgumentException>();
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
