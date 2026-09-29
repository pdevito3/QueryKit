namespace QueryKit.IntegrationTests.Tests;

using System.Globalization;
using System.Linq.Expressions;
using Configuration;
using Exceptions;
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
    public async Task operator_alias_text_inside_quoted_value_is_kept()
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
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(fakePersonOne.Id);
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
    public async Task list_value_with_comma_is_one_item()
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
        inPeople.Select(x => x.Id).Should().Equal(fakePersonOne.Id);
        notInPeople.Select(x => x.Id).Should().Equal(fakePersonTwo.Id);
    }

    [Theory]
    [InlineData("SpecificDate == 2024-01-15T10:00:00+02:00")]
    [InlineData("SpecificDate ^^ [2024-01-15T10:00:00+02:00]")]
    public async Task date_time_offset_value_with_offset_matches_same_instant(string valueFilter)
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

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var people = await queryablePeople.ApplyQueryKitFilter(input).ToListAsync();

        // Assert
        people.Select(x => x.Id).Should().Equal(fakePersonOne.Id);
    }

    [Theory]
    [InlineData("SpecificDateTime == 2024-01-15T08:00:00.500Z")]
    [InlineData("SpecificDate == 2024-01-15T10:00:00.5+02:00")]
    [InlineData("Time == 08:30:00.5")]
    [InlineData("Time == \"08:30:00.5\"")]
    public async Task fractional_seconds_are_kept(string valueFilter)
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
        people.Select(x => x.Id).Should().Equal(fakePersonOne.Id);
    }

    [Theory]
    [InlineData("SpecificDateTime == 2024-01-15T08:00:00")]
    [InlineData("SpecificDateTime == 2024-01-15T10:00:00+02:00")]
    [InlineData("SpecificDateTime ^^ [2024-01-15T08:00:00]")]
    [InlineData("SpecificDateTime ^^ [2024-01-15T10:00:00+02:00]")]
    [InlineData("SpecificDate == 2024-01-15T08:00:00")]
    [InlineData("SpecificDate == 2024-01-15T10:00:00+02:00")]
    [InlineData("SpecificDate ^^ [2024-01-15T08:00:00]")]
    [InlineData("SpecificDate ^^ [2024-01-15T10:00:00+02:00]")]
    public async Task date_time_without_offset_is_utc(string valueFilter)
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var title = $"utc {Guid.NewGuid()}";
        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithTitle(title)
            .WithSpecificDateTime(new DateTime(2024, 1, 15, 8, 0, 0, DateTimeKind.Utc))
            .WithSpecificDate(new DateTimeOffset(2024, 1, 15, 8, 0, 0, TimeSpan.Zero))
            .Build();
        var fakePersonTwo = new FakeTestingPersonBuilder()
            .WithTitle(title)
            .WithSpecificDateTime(new DateTime(2024, 1, 15, 9, 0, 0, DateTimeKind.Utc))
            .WithSpecificDate(new DateTimeOffset(2024, 1, 15, 9, 0, 0, TimeSpan.Zero))
            .Build();
        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo);

        var input = $"""{nameof(TestingPerson.Title)} == "{title}" && {valueFilter}""";

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var people = await queryablePeople.ApplyQueryKitFilter(input).ToListAsync();

        // Assert
        people.Select(x => x.Id).Should().Equal(fakePersonOne.Id);
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
    public async Task custom_operation_keeps_quoted_value_as_string()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var firstName = $"custom op {Guid.NewGuid()}";
        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithFirstName(firstName)
            .WithTitle("001")
            .Build();
        var fakePersonTwo = new FakeTestingPersonBuilder()
            .WithFirstName(firstName)
            .WithTitle("1")
            .Build();
        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo);

        var input = $"""titleIs == "001" && {nameof(TestingPerson.FirstName)} == "{firstName}" """;
        var config = new QueryKitConfiguration(config =>
        {
            config.CustomOperation<TestingPerson>((x, op, value) => x.Title == (string)value)
                .HasQueryName("titleIs");
        });

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var people = await queryablePeople.ApplyQueryKitFilter(input, config).ToListAsync();

        // Assert
        people.Select(x => x.Id).Should().Equal(fakePersonOne.Id);
    }

    [Fact]
    public void string_operator_with_null_value_throws_querykit_exception()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var queryablePeople = testingServiceScope.DbContext().People;

        // Act
        var act = () => queryablePeople.ApplyQueryKitFilter("Title @= null");

        // Assert
        act.Should().Throw<QueryKitParsingException>();
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

    [Theory]
    [InlineData("""Age == "abc" """)]
    [InlineData("""Age == abc""")]
    [InlineData("""Rating > "abc" """)]
    [InlineData("""Rating > abc""")]
    public async Task invalid_value_throws_parsing_exception(string input)
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var act = async () => await queryablePeople.ApplyQueryKitFilter(input).ToListAsync();

        // Assert
        await act.Should().ThrowAsync<ParsingException>();
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
}
