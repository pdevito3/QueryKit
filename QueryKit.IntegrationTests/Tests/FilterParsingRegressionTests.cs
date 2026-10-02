namespace QueryKit.IntegrationTests.Tests;

using System.Globalization;
using System.Linq.Expressions;
using Configuration;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Operators;
using SharedTestingHelper.Fakes;
using SharedTestingHelper.Fakes.Ingredients;
using SharedTestingHelper.Fakes.Recipes;
using WebApiTestProject.Entities;
using WebApiTestProject.Entities.Recipes;

public class FilterParsingRegressionTests : TestBase
{
    [Fact]
    public async Task operator_alias_text_inside_quoted_value_is_replaced()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var title = $"salt and pepper or eq {Guid.NewGuid()}";
        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithTitle(title)
            .Build();
        var fakePersonTwo = new FakeTestingPersonBuilder().Build();
        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo);

        var input = $"""{nameof(TestingPerson.Title)} eq "{title}" and {nameof(TestingPerson.Id)} neq "{Guid.NewGuid()}" """;
        var config = new QueryKitConfiguration(settings =>
        {
            settings.EqualsOperator = "eq";
            settings.NotEqualsOperator = "neq";
            settings.AndOperator = "and";
            settings.OrOperator = "or";
        });

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input, config);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Should().BeEmpty();
    }

    [Fact]
    public async Task decimal_value_uses_invariant_culture()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var title = $"culture {Guid.NewGuid()}";
        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithTitle(title)
            .WithRating(4.6m)
            .Build();
        var fakePersonTwo = new FakeTestingPersonBuilder()
            .WithTitle(title)
            .WithRating(4.4m)
            .Build();
        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo);

        var input = $"""{nameof(TestingPerson.Title)} == "{title}" && {nameof(TestingPerson.Rating)} > 4.5""";

        // Act
        var originalCulture = CultureInfo.CurrentCulture;
        IQueryable<TestingPerson> appliedQueryable;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            appliedQueryable = testingServiceScope.DbContext().People.ApplyQueryKitFilter(input);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(fakePersonOne.Id);
    }

    [Fact]
    public async Task list_value_with_comma_is_split_into_items()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var suffix = Guid.NewGuid().ToString();
        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithTitle($"Warm, with syrup {suffix}")
            .WithFirstName(suffix)
            .Build();
        var fakePersonTwo = new FakeTestingPersonBuilder()
            .WithTitle("Warm")
            .WithFirstName(suffix)
            .Build();
        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo);

        var inInput = $"""{nameof(TestingPerson.FirstName)} == "{suffix}" && {nameof(TestingPerson.Title)} ^^ ["Warm, with syrup {suffix}"]""";
        var notInInput = $"""{nameof(TestingPerson.FirstName)} == "{suffix}" && {nameof(TestingPerson.Title)} !^^ ["Warm, with syrup {suffix}"]""";

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var inPeople = await queryablePeople.ApplyQueryKitFilter(inInput).ToListAsync();
        var notInPeople = await queryablePeople.ApplyQueryKitFilter(notInInput).ToListAsync();

        // Assert
        inPeople.Select(x => x.Id).Should().Equal(fakePersonTwo.Id);
        notInPeople.Select(x => x.Id).Should().Equal(fakePersonOne.Id);
    }

    [Theory]
    [InlineData("SpecificDate == 2024-01-15T10:00:00+02:00", false)]
    [InlineData("SpecificDate ^^ [2024-01-15T10:00:00+02:00]", false)]
    [InlineData("SpecificDate == 2024-01-15T10:00:00+02:00", true)]
    [InlineData("SpecificDate ^^ [2024-01-15T10:00:00+02:00]", true)]
    public async Task date_time_offset_value_with_offset_matches_same_instant(string valueFilter, bool parameterizeFilterValues)
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var title = $"offset {Guid.NewGuid()}";
        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithTitle(title)
            .WithSpecificDate(new DateTimeOffset(2024, 1, 15, 8, 0, 0, TimeSpan.Zero))
            .Build();
        var fakePersonTwo = new FakeTestingPersonBuilder()
            .WithTitle(title)
            .WithSpecificDate(new DateTimeOffset(2024, 1, 15, 10, 0, 0, TimeSpan.Zero))
            .Build();
        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo);

        var input = $"""{nameof(TestingPerson.Title)} == "{title}" && {valueFilter}""";
        var config = new QueryKitConfiguration(settings => settings.ParameterizeFilterValues = parameterizeFilterValues);

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var people = await queryablePeople.ApplyQueryKitFilter(input, config).ToListAsync();

        // Assert
        people.Select(x => x.Id).Should().Equal(fakePersonOne.Id);
    }

    [Theory]
    [InlineData("SpecificDateTime == 2024-01-15T08:00:00.500Z", true)]
    [InlineData("SpecificDateTime == 2024-01-15T08:00:00Z.5", true)]
    [InlineData("SpecificDate == 2024-01-15T10:00:00.5+02:00", true)]
    [InlineData("Time == 08:30:00.5", true)]
    [InlineData("Time == \"08:30:00.500\"", true)]
    [InlineData("Time == \"08:30:00.5\"", true)]
    public async Task fractional_second_value_matches_by_its_fraction(string valueFilter, bool expectFractionPerson)
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var title = $"fraction {Guid.NewGuid()}";
        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithTitle(title)
            .WithSpecificDateTime(new DateTime(2024, 1, 15, 8, 0, 0, 500, DateTimeKind.Utc))
            .WithSpecificDate(new DateTimeOffset(2024, 1, 15, 8, 0, 0, 500, TimeSpan.Zero))
            .WithTime(new TimeOnly(8, 30, 0, 500))
            .Build();
        var fakePersonTwo = new FakeTestingPersonBuilder()
            .WithTitle(title)
            .WithSpecificDateTime(new DateTime(2024, 1, 15, 8, 0, 0, DateTimeKind.Utc))
            .WithSpecificDate(new DateTimeOffset(2024, 1, 15, 8, 0, 0, TimeSpan.Zero))
            .WithTime(new TimeOnly(8, 30, 0))
            .Build();
        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo);

        var input = $"""{nameof(TestingPerson.Title)} == "{title}" && {valueFilter}""";

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var people = await queryablePeople.ApplyQueryKitFilter(input).ToListAsync();

        // Assert
        people.Select(x => x.Id).Should().Equal(expectFractionPerson ? fakePersonOne.Id : fakePersonTwo.Id);
    }

    [Theory]
    [InlineData("Age == Rating", true)]
    [InlineData("Rating == Age", true)]
    [InlineData("Age != Rating", false)]
    [InlineData("Rating != Age", false)]
    public async Task int_property_compares_to_decimal_property(string valueFilter, bool expectEqualPerson)
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var title = $"numeric {Guid.NewGuid()}";
        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithTitle(title)
            .WithAge(4)
            .WithRating(4m)
            .Build();
        var fakePersonTwo = new FakeTestingPersonBuilder()
            .WithTitle(title)
            .WithAge(4)
            .WithRating(4.5m)
            .Build();
        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo);

        var input = $"""{nameof(TestingPerson.Title)} == "{title}" && {valueFilter}""";

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var people = await queryablePeople.ApplyQueryKitFilter(input).ToListAsync();

        // Assert
        people.Select(x => x.Id).Should().Equal(expectEqualPerson ? fakePersonOne.Id : fakePersonTwo.Id);
    }

    [Theory]
    [InlineData("""Title @= "am" """, false)]
    [InlineData("""Title _= "la" """, false)]
    [InlineData("""Title _-= "mb" """, false)]
    [InlineData("""Title !@= "am" """, true)]
    [InlineData("""Title !_= "la" """, true)]
    [InlineData("""Title !_-= "mb" """, true)]
    public async Task case_sensitive_string_operator_handles_null_property(string valueFilter, bool expectNullPerson)
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var firstName = $"null title {Guid.NewGuid()}";
        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithFirstName(firstName)
            .WithTitle(null)
            .Build();
        var fakePersonTwo = new FakeTestingPersonBuilder()
            .WithFirstName(firstName)
            .WithTitle("lamb")
            .Build();
        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo);

        var input = $"""{nameof(TestingPerson.FirstName)} == "{firstName}" && {valueFilter}""";

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var people = await queryablePeople.ApplyQueryKitFilter(input).ToListAsync();

        // Assert
        people.Select(x => x.Id).Should().Equal(expectNullPerson ? fakePersonOne.Id : fakePersonTwo.Id);
    }

    [Fact]
    public async Task comparison_operator_factory_with_uses_all_matches_every_item()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var name = $"waffle {Guid.NewGuid()}";
        var fakeRecipeOne = new FakeRecipeBuilder().Build();
        fakeRecipeOne.AddIngredient(new FakeIngredientBuilder().WithName(name).Build());
        fakeRecipeOne.AddIngredient(new FakeIngredientBuilder().WithName(name).Build());
        var fakeRecipeTwo = new FakeRecipeBuilder().Build();
        fakeRecipeTwo.AddIngredient(new FakeIngredientBuilder().WithName(name).Build());
        fakeRecipeTwo.AddIngredient(new FakeIngredientBuilder().WithName($"other {Guid.NewGuid()}").Build());
        await testingServiceScope.InsertAsync(fakeRecipeOne, fakeRecipeTwo);

        Expression<Func<Recipe, IEnumerable<string>>> ingredientNames = x => x.Ingredients.Select(y => y.Name);
        var body = ComparisonOperator.EqualsOperator(usesAll: true)
            .GetExpression<Recipe>(ingredientNames.Body, Expression.Constant(name), null);
        var filterExpression = Expression.Lambda<Func<Recipe, bool>>(body, ingredientNames.Parameters);
        var recipeIds = new[] { fakeRecipeOne.Id, fakeRecipeTwo.Id };

        // Act
        var queryableRecipes = testingServiceScope.DbContext().Recipes;
        var recipes = await queryableRecipes
            .Where(x => recipeIds.Contains(x.Id))
            .Where(filterExpression)
            .ToListAsync();

        // Assert
        recipes.Select(x => x.Id).Should().Equal(fakeRecipeOne.Id);
    }

    [Fact]
    public async Task sort_direction_after_double_space_is_read()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var title = $"sort {Guid.NewGuid()}";
        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithTitle(title)
            .WithAge(20)
            .Build();
        var fakePersonTwo = new FakeTestingPersonBuilder()
            .WithTitle(title)
            .WithAge(40)
            .Build();
        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo);

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var people = await queryablePeople
            .Where(x => x.Title == title)
            .ApplyQueryKitSort("Age  desc")
            .ToListAsync();

        // Assert
        people.Select(x => x.Id).Should().Equal(fakePersonTwo.Id, fakePersonOne.Id);
    }

    [Theory]
    [InlineData("""Tags ^$ "sweet" """, new[] { "pancakes" })]
    [InlineData("""Tags ^$* "WINNER" """, new[] { "bread" })]
    [InlineData("""Tags %^$ "dinner" """, new[] { "stew", "water" })]
    public async Task has_returns_matching_rows(string input, string[] expectedTitles)
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var prefix = $"tags {Guid.NewGuid()} ";
        var pancakes = new FakeRecipeBuilder().WithTitle($"{prefix}pancakes").Build().SetTags(["breakfast", "sweet"]);
        var stew = new FakeRecipeBuilder().WithTitle($"{prefix}stew").Build().SetTags(["dinner"]);
        var bread = new FakeRecipeBuilder().WithTitle($"{prefix}bread").Build().SetTags(["bread", "Winner"]);
        var water = new FakeRecipeBuilder().WithTitle($"{prefix}water").Build().SetTags([]);
        await testingServiceScope.InsertAsync(pancakes, stew, bread, water);

        // Act
        var queryableRecipes = testingServiceScope.DbContext().Recipes;
        var recipes = await queryableRecipes
            .Where(x => x.Title.StartsWith(prefix))
            .ApplyQueryKitFilter(input)
            .ToListAsync();

        // Assert
        recipes.Select(x => x.Title[prefix.Length..]).Should().BeEquivalentTo(expectedTitles);
    }
}
