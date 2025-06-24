namespace QueryKit.IntegrationTests.Tests;

using Bogus;
using Configuration;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SharedTestingHelper.Fakes;
using SharedTestingHelper.Fakes.Author;
using SharedTestingHelper.Fakes.IngredientPreparations;
using SharedTestingHelper.Fakes.Ingredients;
using SharedTestingHelper.Fakes.Recipes;
using WebApiTestProject.Database;
using WebApiTestProject.Entities;
using WebApiTestProject.Entities.Authors;
using WebApiTestProject.Entities.Ingredients;
using WebApiTestProject.Entities.Ingredients.Models;
using WebApiTestProject.Entities.Recipes;
using Xunit.Abstractions;

public class DatabaseFilteringTests(ITestOutputHelper testOutputHelper) : TestBase
{
    [Fact]
    public async Task can_filter_by_string()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var faker = new Faker();
        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithTitle(faker.Lorem.Sentence())
            .Build();
        var fakePersonTwo = new FakeTestingPersonBuilder().Build();
        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo);
        
        var input = $"""{nameof(TestingPerson.Title)} == "{fakePersonOne.Title}" """;
        
        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(fakePersonOne.Id);
    }
    
    [Fact]
    public async Task can_filter_by_boolean()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var faker = new Faker();
        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithTitle(faker.Lorem.Sentence())
            .WithFavorite(true)
            .Build();
        var fakePersonTwo = new FakeTestingPersonBuilder()
            .WithTitle(faker.Lorem.Sentence())
            .WithFavorite(false)
            .Build();
        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo);
        
        var input = $"""{nameof(TestingPerson.Title)} == "{fakePersonOne.Title}" && {nameof(TestingPerson.Favorite)} == true""";
        
        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(fakePersonOne.Id);
    }
    
    [Fact]
    public async Task can_filter_by_combo_multi_value_pass()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var fakePersonOne = new FakeTestingPersonBuilder().Build();
        var fakePersonTwo = new FakeTestingPersonBuilder()
            .WithFirstName(fakePersonOne.FirstName)
            .Build();
        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo);
        
        var input = $"""fullname @=* "{fakePersonOne.FirstName} {fakePersonOne.LastName}" """;
        var config = new QueryKitConfiguration(config =>
        {
            config.DerivedProperty<TestingPerson>(tp => tp.FirstName + " " + tp.LastName).HasQueryName("fullname");
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
    public async Task can_filter_by_combo_complex()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithAge(8888)
            .Build();
        var fakePersonTwo = new FakeTestingPersonBuilder()
            .WithFirstName(fakePersonOne.FirstName)
            .Build();
        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo);
        
        var input = $"""(fullname @=* "{fakePersonOne.FirstName} {fakePersonOne.LastName}") && age >= {fakePersonOne.Age}""";
        var config = new QueryKitConfiguration(config =>
        {
            config.DerivedProperty<TestingPerson>(tp => tp.FirstName + " " + tp.LastName).HasQueryName("fullname");
        });
        
        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input, config);
        var people = await appliedQueryable.ToListAsync();
        
        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(fakePersonOne.Id);
    }
    
    [Theory]
    [InlineData(88448)]
    [InlineData(-83388)]
    public async Task can_filter_by_int(int age)
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithAge(age)
            .Build();
        var fakePersonTwo = new FakeTestingPersonBuilder()
            .WithFirstName(fakePersonOne.FirstName)
            .Build();
        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo);
        
        var input = $"""age == {fakePersonOne.Age}""";
        var config = new QueryKitConfiguration(_ =>
        {
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
    public async Task can_filter_by_and_also_bool()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var faker = new Faker();
        var uniqueTitle = faker.Lorem.Sentence();
        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithFirstName("John")
            .WithAge(18)
            .WithTitle(uniqueTitle)
            .Build();
        var fakePersonTwo = new FakeTestingPersonBuilder().Build();
        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo);
        
        var input = $"""adult_johns == true && Title == "{uniqueTitle}" """;
        var config = new QueryKitConfiguration(config =>
        {
            config.DerivedProperty<TestingPerson>(tp => tp.Age >= 18 && tp.FirstName == "John").HasQueryName("adult_johns");
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
    public async Task can_filter_by_combo()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithFirstName(Guid.NewGuid().ToString())
            .Build();
        var fakePersonTwo = new FakeTestingPersonBuilder().Build();
        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo);
        
        var input = $"""fullname @=* "{fakePersonOne.FirstName}" """;
        var config = new QueryKitConfiguration(config =>
        {
            config.DerivedProperty<TestingPerson>(tp => tp.FirstName + " " + tp.LastName).HasQueryName("fullname");
        });
        
        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input, config);
        var people = await appliedQueryable.ToListAsync();
        // var people = testingServiceScope.DbContext().People
        //     // .Where(p => (p.FirstName + " " + p.LastName).ToLower().Contains(fakePersonOne.FirstName.ToLower()))
        //     // .Where(x => ((x.FirstName + " ") + x.LastName).ToLower().Contains("ito".ToLower()))
        //     .ToList();
        
        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(fakePersonOne.Id);
    }
    
    [Fact]
    public async Task can_filter_by_string_for_collection()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var faker = new Faker();
        var fakeIngredientOne = new FakeIngredientBuilder()
            .WithName(faker.Lorem.Sentence())
            .Build();
        var fakeRecipeOne = new FakeRecipeBuilder().Build();
        fakeRecipeOne.AddIngredient(fakeIngredientOne);
        
        var fakeIngredientTwo = new FakeIngredientBuilder()
            .WithName(faker.Lorem.Sentence())
            .Build();
        var fakeRecipeTwo = new FakeRecipeBuilder().Build();
        fakeRecipeTwo.AddIngredient(fakeIngredientTwo);
        await testingServiceScope.InsertAsync(fakeRecipeOne, fakeRecipeTwo);
        
        var input = $"""Ingredients.Name == "{fakeIngredientOne.Name}" """;

        // Act
        var queryableRecipes = testingServiceScope.DbContext().Recipes;
        var appliedQueryable = queryableRecipes.ApplyQueryKitFilter(input);
        var recipes = await appliedQueryable.ToListAsync();

        // Assert
        recipes.Count.Should().Be(1);
        recipes[0].Id.Should().Be(fakeRecipeOne.Id);
    }
    
    [Fact]
    public async Task can_filter_by_numeric_string_for_collection()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var faker = new Faker();
        var fakeIngredientOne = new FakeIngredientBuilder()
            .WithName("abc123")
            .Build();
        var fakeRecipeOne = new FakeRecipeBuilder().Build();
        fakeRecipeOne.AddIngredient(fakeIngredientOne);
        
        var fakeIngredientTwo = new FakeIngredientBuilder()
            .WithName(faker.Lorem.Sentence())
            .Build();
        var fakeRecipeTwo = new FakeRecipeBuilder().Build();
        fakeRecipeTwo.AddIngredient(fakeIngredientTwo);
        await testingServiceScope.InsertAsync(fakeRecipeOne, fakeRecipeTwo);
        
        var input = $"""Ingredients.Name @=* "123" """;

        // Act
        var queryableRecipes = testingServiceScope.DbContext().Recipes;
        var appliedQueryable = queryableRecipes.ApplyQueryKitFilter(input);
        var recipes = await appliedQueryable.ToListAsync();

        // Assert
        recipes.Count.Should().Be(1);
        recipes[0].Id.Should().Be(fakeRecipeOne.Id);
    }
    
    [Fact]
    public async Task can_filter_by_string_for_collection_with_count()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var faker = new Faker();
        var fakeIngredientOne = new FakeIngredientBuilder()
            .WithName(faker.Lorem.Sentence())
            .Build();
        var fakeRecipeOne = new FakeRecipeBuilder().Build();
        fakeRecipeOne.AddIngredient(fakeIngredientOne);
        
        var fakeIngredientTwo = new FakeIngredientBuilder()
            .WithName(faker.Lorem.Sentence())
            .Build();
        var fakeRecipeTwo = new FakeRecipeBuilder().Build();
        fakeRecipeTwo.AddIngredient(fakeIngredientTwo);
        await testingServiceScope.InsertAsync(fakeRecipeOne, fakeRecipeTwo);
        
        var input = $"""Title == "{fakeRecipeOne.Title}" && Ingredients #>= 1""";

        // Act
        var queryableRecipes = testingServiceScope.DbContext().Recipes;
        var appliedQueryable = queryableRecipes.ApplyQueryKitFilter(input);
        var recipes = await appliedQueryable.ToListAsync();

        // Assert
        recipes.Count.Should().Be(1);
        recipes[0].Id.Should().Be(fakeRecipeOne.Id);
    }
    
    [Fact]
    public async Task can_filter_by_string_for_collection_contains()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var faker = new Faker();
        var fakeIngredientOne = new FakeIngredientBuilder()
            .WithName($"{faker.Lorem.Sentence()}partial")
            .Build();
        var fakeRecipeOne = new FakeRecipeBuilder().Build();
        fakeRecipeOne.AddIngredient(fakeIngredientOne);
        
        var fakeIngredientTwo = new FakeIngredientBuilder()
            .WithName(faker.Lorem.Sentence())
            .Build();
        var fakeRecipeTwo = new FakeRecipeBuilder().Build();
        fakeRecipeTwo.AddIngredient(fakeIngredientTwo);
        await testingServiceScope.InsertAsync(fakeRecipeOne, fakeRecipeTwo);
        
        var input = $"""Ingredients.Name @= "partial" """;

        // Act
        var queryableRecipes = testingServiceScope.DbContext().Recipes;
        var appliedQueryable = queryableRecipes.ApplyQueryKitFilter(input);
        var recipes = await appliedQueryable.ToListAsync();

        // Assert
        recipes.Count.Should().Be(1);
        recipes[0].Id.Should().Be(fakeRecipeOne.Id);
    }
    
    [Fact]
    public async Task can_filter_within_collection_long()
    {
        var testingServiceScope = new TestingServiceScope();
        var qualityLevel = 2L;
        var fakeRecipeOne = new FakeRecipeBuilder().Build();
        var ingredient = new FakeIngredientBuilder()
            .WithQualityLevel(qualityLevel)
            .Build();
        fakeRecipeOne.AddIngredient(ingredient);
        
        await testingServiceScope.InsertAsync(fakeRecipeOne);
        
        var input = $"ql == {qualityLevel}";
        var config = new QueryKitConfiguration(settings =>
        {
            settings.Property<Recipe>(x => x.Ingredients.Select(y => y.QualityLevel)).HasQueryName("ql");
        });
        
        var queryableRecipes = testingServiceScope.DbContext().Recipes;
        var appliedQueryable = queryableRecipes.ApplyQueryKitFilter(input, config);
        var recipes = await appliedQueryable.ToListAsync();
        
        recipes.Count.Should().Be(1);
        recipes[0].Id.Should().Be(fakeRecipeOne.Id);
    }
    
    [Fact]
    public async Task can_filter_by_guid_for_collection()
    {
        var testingServiceScope = new TestingServiceScope();
        var fakeRecipeOne = new FakeRecipeBuilder().Build();
        var ingredient = new FakeIngredientBuilder()
            .Build();

        fakeRecipeOne.AddIngredient(ingredient);

        await testingServiceScope.InsertAsync(fakeRecipeOne);

        var input = $""" Ingredients.Id == "{ingredient.Id}" """;
        var queryableRecipes = testingServiceScope.DbContext().Recipes;
        var appliedQueryable = queryableRecipes.ApplyQueryKitFilter(input);
        var recipes = await appliedQueryable.ToListAsync();

        recipes.Count.Should().Be(1);
        recipes[0].Ingredients.First().Id.Should().Be(ingredient.Id);
    }
    
    [Fact]
    public async Task can_filter_by_string_for_nested_collection()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var faker = new Faker();
        var preparationOne = new FakeIngredientPreparation().Generate();
        var preparationTwo = new FakeIngredientPreparation().Generate();
        var fakeIngredientOne = new FakeIngredientBuilder()
            .WithPreparation(preparationOne)
            .Build();
        var fakeRecipeOne = new FakeRecipeBuilder().Build();
        fakeRecipeOne.AddIngredient(fakeIngredientOne);
        
        var fakeIngredientTwo = new FakeIngredientBuilder()
            .WithName(faker.Lorem.Sentence())
            .WithPreparation(preparationTwo)
            .Build();
        var fakeRecipeTwo = new FakeRecipeBuilder().Build();
        fakeRecipeTwo.AddIngredient(fakeIngredientTwo);
        await testingServiceScope.InsertAsync(fakeRecipeOne, fakeRecipeTwo);
        
        var input = $"""Ingredients.Preparations.Text == "{preparationOne.Text}" """;
        var config = new QueryKitConfiguration(settings =>
        {
        });

        // Act
        var queryableRecipes = testingServiceScope.DbContext().Recipes;
        var appliedQueryable = queryableRecipes.ApplyQueryKitFilter(input, config);
        var recipes = await appliedQueryable.ToListAsync();

        // Assert
        recipes.Count.Should().Be(1);
        recipes[0].Id.Should().Be(fakeRecipeOne.Id);
    }
    
    [Fact]
    public async Task can_filter_by_string_for_nested_collection_with_alias()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var faker = new Faker();
        var preparationOne = new FakeIngredientPreparation().Generate();
        var preparationTwo = new FakeIngredientPreparation().Generate();
        var fakeIngredientOne = new FakeIngredientBuilder()
            .WithPreparation(preparationOne)
            .Build();
        var fakeRecipeOne = new FakeRecipeBuilder().Build();
        fakeRecipeOne.AddIngredient(fakeIngredientOne);
        
        var fakeIngredientTwo = new FakeIngredientBuilder()
            .WithName(faker.Lorem.Sentence())
            .WithPreparation(preparationTwo)
            .Build();
        var fakeRecipeTwo = new FakeRecipeBuilder().Build();
        fakeRecipeTwo.AddIngredient(fakeIngredientTwo);
        await testingServiceScope.InsertAsync(fakeRecipeOne, fakeRecipeTwo);
        
        var input = $"""preparations == "{preparationOne.Text}" """;
        var config = new QueryKitConfiguration(settings =>
        {
            settings.Property<Recipe>(x => x.Ingredients
                .SelectMany(y => y.Preparations)
                .Select(y => y.Text))
                .HasQueryName("preparations");
        });

        // Act
        var queryableRecipes = testingServiceScope.DbContext().Recipes;
        var appliedQueryable = queryableRecipes.ApplyQueryKitFilter(input, config);
        var recipes = await appliedQueryable.ToListAsync();

        // Assert
        recipes.Count.Should().Be(1);
        recipes[0].Id.Should().Be(fakeRecipeOne.Id);
    }

    
    [Fact]
    public async Task can_filter_by_string_for_collection_does_not_contain()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var faker = new Faker();
        var fakeIngredientOne = new FakeIngredientBuilder()
            .WithName($"{faker.Lorem.Sentence()}partial")
            .Build();
        var fakeRecipeOne = new FakeRecipeBuilder().Build();
        fakeRecipeOne.AddIngredient(fakeIngredientOne);
        
        var fakeIngredientTwo = new FakeIngredientBuilder()
            .WithName(faker.Lorem.Sentence())
            .Build();
        var fakeRecipeTwo = new FakeRecipeBuilder().Build();
        fakeRecipeTwo.AddIngredient(fakeIngredientTwo);
        await testingServiceScope.InsertAsync(fakeRecipeOne, fakeRecipeTwo);
        
        var input = $"""Ingredients.Name !@= "partial" """;

        // Act
        var queryableRecipes = testingServiceScope.DbContext().Recipes;
        var appliedQueryable = queryableRecipes.ApplyQueryKitFilter(input);
        var recipes = await appliedQueryable.ToListAsync();

        // Assert
        recipes.FirstOrDefault(x => x.Id == fakeRecipeOne.Id).Should().BeNull();
        recipes.FirstOrDefault(x => x.Id == fakeRecipeTwo.Id).Should().NotBeNull();
    }
    
    [Fact]
    public async Task can_use_soundex_equals()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();

        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithTitle("DeVito")
            .Build();
        var fakePersonTwo = new FakeTestingPersonBuilder().Build();
        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo);
        
        var input = $"""{nameof(TestingPerson.Title)} ~~ "davito" """;

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input, new QueryKitConfiguration(o =>
        {
            o.DbContextType = typeof(TestingDbContext);
        }));
        var people = await appliedQueryable.ToListAsync();
        
        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(fakePersonOne.Id);
    }
    
    [Fact]
    public async Task can_use_soundex_not_equals()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();

        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithTitle("Jaime")
            .Build();
        var fakePersonTwo = new FakeTestingPersonBuilder().Build();
        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo);
        
        var input = $"""{nameof(TestingPerson.Title)} !~ "jaymee" """;

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input, new QueryKitConfiguration(o =>
        {
            o.DbContextType = typeof(TestingDbContext);
        }));
        var people = await appliedQueryable.ToListAsync();
        
        // Assert
        people.Count(x => x.Id == fakePersonOne.Id).Should().Be(0);
    }
    
    [Fact]
    public async Task can_filter_by_datetime_with_milliseconds()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var dateUtcNow = DateTime.UtcNow;
        var dateInMilliPast = dateUtcNow.AddMilliseconds(-100);
        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithSpecificDateTime(dateUtcNow)
            .Build();
        var fakePersonTwo = new FakeTestingPersonBuilder()
            .WithSpecificDateTime(dateInMilliPast)
            .Build();
        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo);

        var input = $"""SpecificDateTime == "{fakePersonOne.SpecificDateTime:yyyy-MM-ddTHH:mm:ss.ffffff}Z" """;

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;

        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(fakePersonOne.Id);
    }
    
    [Fact]
    public async Task can_filter_by_datetime_with_milliseconds_full_fractional()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var dateUtcNow = DateTime.UtcNow;
        var dateInMilliPast = dateUtcNow.AddMilliseconds(-100);
        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithSpecificDateTime(dateUtcNow)
            .Build();
        var fakePersonTwo = new FakeTestingPersonBuilder()
            .WithSpecificDateTime(dateInMilliPast)
            .Build();
        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo);

        var input = $"""SpecificDateTime == "{fakePersonOne.SpecificDateTime:o}" """;

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;

        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(fakePersonOne.Id);
    }
    
    [Fact]
    public async Task can_filter_by_dateonly()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var baseDate = new DateTime(1678, 01, 01);
        var dateToday = DateOnly.FromDateTime(baseDate);
        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithDate(dateToday)
            .Build();
        var fakePersonTwo = new FakeTestingPersonBuilder()
            .WithDate(DateOnly.FromDateTime(baseDate.AddDays(-1)))
            .Build();
        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo);

        var input = $"""Date == "{fakePersonOne.Date:yyyy-MM-dd}" """;

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;

        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(fakePersonOne.Id);
    }
    
    [Fact]
    public async Task can_filter_by_timeonly_with_micros()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var timeNow = TimeOnly.FromDateTime(DateTime.UtcNow);
        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithTime(timeNow)
            .Build();
        var fakePersonTwo = new FakeTestingPersonBuilder()
            .WithTime(TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(-1)))
            .Build();
        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo);

        var input = $"""Time == "{fakePersonOne.Time:HH:mm:ss.ffffff}" """;

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;

        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(fakePersonOne.Id);
    }
    
    [Fact]
    public async Task can_filter_by_timeonly_without_micros()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var timeNow = TimeOnly.FromDateTime(DateTime.UtcNow);
        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithTime(timeNow)
            .Build();
        var fakePersonTwo = new FakeTestingPersonBuilder()
            .WithTime(TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(-1)))
            .Build();
        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo);

        var input = $"""Time >= "{fakePersonOne.Time:HH:mm:ss}" """;

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;

        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().BeGreaterOrEqualTo(1);
        people.FirstOrDefault(x => x.Id == fakePersonOne.Id).Should().NotBeNull();
    }


    [Fact]
    public async Task can_filter_by_guid()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var fakePersonOne = new FakeTestingPersonBuilder().Build();
        var fakePersonTwo = new FakeTestingPersonBuilder().Build();
        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo);
        
        var input = $"""{nameof(TestingPerson.Id)} == "{fakePersonOne.Id}" """;

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(fakePersonOne.Id);
    }

    [Fact]
    public async Task can_filter_by_guid_contains()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithId(Guid.Parse("3644bceb-d362-4044-9edb-a3ec71c9b1a1"))
            .Build();
        var fakePersonTwo = new FakeTestingPersonBuilder().Build();
        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo);
        
        var input = $"""(id @=* "9edb")""";

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(fakePersonOne.Id);
    }

    [Fact]
    public async Task can_filter_by_nullable_guid_contains()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var fakeRecipeOne = new FakeRecipeBuilder()
            .WithSecondaryId(Guid.Parse("385b1d2c-3b10-4ce0-b19b-f2b76280d57d"))
            .Build();
        var fakeRecipeTwo = new FakeRecipeBuilder().Build();
        await testingServiceScope.InsertAsync(fakeRecipeOne, fakeRecipeTwo);
        
        var input = $"""(secondaryId @=* "4ce0")""";

        // Act
        var queryableRecipe = testingServiceScope.DbContext().Recipes;
        var appliedQueryable = queryableRecipe.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();
        // var people = testingServiceScope.DbContext().Recipes
        //     .Where(x => x.SecondaryId.ToString().Contains("4ce0")).ToList();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(fakeRecipeOne.Id);
    }

    [Fact]
    public async Task can_filter_by_nullable_guid_is_something()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var fakeRecipeOne = new FakeRecipeBuilder().Build();
        var fakeRecipeTwo = new FakeRecipeBuilder().Build();
        await testingServiceScope.InsertAsync(fakeRecipeOne, fakeRecipeTwo);
        
        var input = $"""(secondaryId == "{fakeRecipeTwo.SecondaryId}")""";

        // Act
        var queryableRecipe = testingServiceScope.DbContext().Recipes;
        var appliedQueryable = queryableRecipe.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();
        // var people = testingServiceScope.DbContext().Recipes
        //     .Where(x => x.SecondaryId.ToString() == null)
        //     .ToList();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(fakeRecipeTwo.Id);
    }

    [Fact]
    public async Task can_filter_by_nullable_guid_is_null()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var fakeRecipeOne = new FakeRecipeBuilder()
            .WithSecondaryId(null)
            .Build();
        var fakeRecipeTwo = new FakeRecipeBuilder().Build();
        await testingServiceScope.InsertAsync(fakeRecipeOne, fakeRecipeTwo);
        
        var input = $"""(secondaryId == null)""";

        // Act
        var queryableRecipe = testingServiceScope.DbContext().Recipes;
        var appliedQueryable = queryableRecipe.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();
        // var people = testingServiceScope.DbContext().Recipes
        //     .Where(x => x.SecondaryId == null)
        //     .ToList();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(fakeRecipeOne.Id);
    }
    
    [Fact]
    public async Task return_no_records_when_no_match()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var fakePersonOne = new FakeTestingPersonBuilder().Build();
        var fakePersonTwo = new FakeTestingPersonBuilder().Build();
        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo);
        
        var input = $"""{nameof(TestingPerson.Id)} == "{Guid.NewGuid()}" """;

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(0);
    }
    
    // var people = testingServiceScope.DbContext().People
    //     .Where(x => x.Email == fakePersonOne.Email)
    //     .OrderBy(x => x.Email)
    //     .ToList();
    // TODO needs to have `Email` not `Email.Value` if using `HasConversion`
    [Fact(Skip = "Will need something like this if i want to support HasConversion in efcore.")]
    public async Task can_filter_with_child_props()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var faker = new Faker();
        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithEmail(faker.Internet.Email())
            .Build();
        var fakePersonTwo = new FakeTestingPersonBuilder()
            .Build();
        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo);
        
        var input = $"""email == "{fakePersonOne.Email.Value}" """;

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<TestingPerson>(x => x.Email.Value).HasQueryName("email");
        });
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input, config);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(fakePersonOne.Id);
    }
    
    [Fact]
    public async Task can_filter_nested_property_using_ownsone()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var faker = new Faker();
        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithPhysicalAddress(new Address(faker.Address.StreetAddress()
                , faker.Address.SecondaryAddress()
                , faker.Address.City()
                , faker.Address.State()
                , faker.Address.ZipCode()
                , faker.Address.Country()))
            .Build();
        await testingServiceScope.InsertAsync(fakePersonOne);
        var input = $"""PhysicalAddress.State == "{fakePersonOne.PhysicalAddress.State}" """;

        // Act
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<TestingPerson>(x => x.PhysicalAddress.State); //.HasQueryName("PhysicalAddressPostalCode");
        });
        var people = testingServiceScope.DbContext().People.ApplyQueryKitFilter(input, config)
            // .Where(x => x.PhysicalAddress.State == fakePersonOne.PhysicalAddress.State
            // && x.PhysicalAddress.PostalCode == fakePersonOne.PhysicalAddress.PostalCode)
            // .OrderBy(x => x.Email.Value)
            .ToList();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(fakePersonOne.Id);
    }
    
    [Fact]
    public async Task can_filter_nested_property_using_ownsone_with_alias()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var faker = new Faker();
        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithPhysicalAddress(new Address(faker.Address.StreetAddress()
                , faker.Address.SecondaryAddress()
                , faker.Address.City()
                , Guid.NewGuid().ToString()
                , faker.Address.ZipCode()
                , faker.Address.Country()))
            .Build();
        await testingServiceScope.InsertAsync(fakePersonOne);
        var input = $"""state == "{fakePersonOne.PhysicalAddress.State}" """;

        // Act
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<TestingPerson>(x => x.PhysicalAddress.State).HasQueryName("state");
        });
        var people = testingServiceScope.DbContext().People.ApplyQueryKitFilter(input, config).ToList();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(fakePersonOne.Id);
    }
    
    [Fact]
    public async Task can_filter_by_decimal()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var faker = new Faker();
        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithRating(4M)
            .Build();
        var fakePersonTwo = new FakeTestingPersonBuilder()
            .WithRating(2M)
            .Build();
        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo);

        var input = $"""{nameof(TestingPerson.Rating)} > 3.5""";

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count(x => x.Id == fakePersonOne.Id).Should().Be(1);
    }
    
    [Fact]
    public async Task can_filter_by_negative_decimal()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var faker = new Faker();
        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithRating(-3.533M)
            .Build();
        var fakePersonTwo = new FakeTestingPersonBuilder()
            .WithRating(2M)
            .Build();
        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo);

        var input = $"""{nameof(TestingPerson.Rating)} == -3.533""";

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count(x => x.Id == fakePersonOne.Id).Should().Be(1);
    }
    
    [Fact]
    public async Task can_filter_complex_expression()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var faker = new Faker();
        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithTitle("Waffle & Chicken")
            .WithAge(35)
            .WithBirthMonth(BirthMonthEnum.January)
            .WithRating(4.0M)
            .WithSpecificDate(new DateTime(2022, 07, 01, 00, 00, 03, DateTimeKind.Utc))
            .WithDate(DateOnly.FromDateTime(new DateTime(2022, 07, 01)))
            .Build();
        var fakePersonTwo = new FakeTestingPersonBuilder()
            .WithTitle("Lamb")
            .WithAge(17)
            .WithBirthMonth(BirthMonthEnum.February)
            .WithRating(3.4M)
            .WithSpecificDate(new DateTime(2022, 07, 01, 00, 00, 03, DateTimeKind.Utc))
            .Build();
        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo);
        
        var input = $"""""((Title @=* "waffle & chicken" && Age > 30) || Id == "{fakePersonOne.Id}" || Title == "lamb" || Title == null) && (Age < 18 || (BirthMonth == 1 && Title _= "ally")) || Rating > 3.5 || SpecificDate == 2022-07-01T00:00:03Z && (Date == 2022-07-01 || Time == 00:00:03)""""";

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(fakePersonOne.Id);
    }

    [Fact]
    public async Task can_filter_by_string_with_special_characters()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithTitle("""lamb is great on a "gee-ro" not a "gy-ro" sandwich""")
            .WithAge(10)
            .Build();
        var fakePersonTwo = new FakeTestingPersonBuilder().Build();
        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo);

        var input = $"""""{nameof(TestingPerson.Title)} == """{fakePersonOne.Title}""" """"";

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();
        
        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(fakePersonOne.Id);
        people[0].Title.Should().Be(fakePersonOne.Title);
    }

    [Fact]
    public async Task can_handle_in_for_int()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithAge(-22)
            .Build();
        var fakePersonTwo = new FakeTestingPersonBuilder()
            .WithAge(40)
            .Build();
        var fakePersonThree = new FakeTestingPersonBuilder()
            .WithAge(60)
            .Build();
        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo, fakePersonThree);

        var input = """Age ^^ [-22, 60]""";

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();
        
        // Assert
        people.Count.Should().BeGreaterOrEqualTo(2);
        people.FirstOrDefault(x => x.Id == fakePersonOne.Id).Should().NotBeNull();
        people.FirstOrDefault(x => x.Id == fakePersonTwo.Id).Should().BeNull();
        people.FirstOrDefault(x => x.Id == fakePersonThree.Id).Should().NotBeNull();
    }

    [Fact]
    public async Task can_handle_in_for_decimal()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithRating(-22.44M)
            .Build();
        var fakePersonTwo = new FakeTestingPersonBuilder()
            .WithRating(40.55M)
            .Build();
        var fakePersonThree = new FakeTestingPersonBuilder()
            .WithRating(60.99M)
            .Build();
        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo, fakePersonThree);

        var input = """Rating ^^ [-22.44, 60.99]""";

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();
        
        // Assert
        people.Count.Should().BeGreaterOrEqualTo(2);
        people.FirstOrDefault(x => x.Id == fakePersonOne.Id).Should().NotBeNull();
        people.FirstOrDefault(x => x.Id == fakePersonTwo.Id).Should().BeNull();
        people.FirstOrDefault(x => x.Id == fakePersonThree.Id).Should().NotBeNull();
    }

    [Fact]
    public async Task can_handle_in_for_guid()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithAge(22)
            .Build();
        var fakePersonTwo = new FakeTestingPersonBuilder()
            .WithAge(60)
            .Build();
        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo);

        var input = $"""Id ^^ ["{fakePersonOne.Id.ToString()}"]""";

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();
        
        // Assert
        people.Count.Should().BeGreaterOrEqualTo(1);
        people.FirstOrDefault(x => x.Id == fakePersonOne.Id).Should().NotBeNull();
        people.FirstOrDefault(x => x.Id == fakePersonTwo.Id).Should().BeNull();
    }

    [Fact]
    public async Task can_handle_in_for_string()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var fakePersonOne = new FakeTestingPersonBuilder().Build();
        var fakePersonTwo = new FakeTestingPersonBuilder().Build();
        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo);

        var input = $"""Title ^^ ["{fakePersonOne.Title}"]""";

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();
        
        // Assert
        people.Count.Should().BeGreaterOrEqualTo(1);
        people.FirstOrDefault(x => x.Id == fakePersonOne.Id).Should().NotBeNull();
        people.FirstOrDefault(x => x.Id == fakePersonTwo.Id).Should().BeNull();
    }

    [Fact]
    public async Task can_handle_case_insensitive_in_for_string()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var fakePersonOne = new FakeTestingPersonBuilder().Build();
        var fakePersonTwo = new FakeTestingPersonBuilder().Build();
        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo);

        var input = $"""Title ^^* ["{fakePersonOne.Title.ToUpper()}"]""";

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();
        
        // Assert
        people.Count.Should().BeGreaterOrEqualTo(1);
        people.FirstOrDefault(x => x.Id == fakePersonOne.Id).Should().NotBeNull();
        people.FirstOrDefault(x => x.Id == fakePersonTwo.Id).Should().BeNull();
    }

    [Fact]
    public async Task can_handle_case_sensitive_in_for_string()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var fakePersonOne = new FakeTestingPersonBuilder().Build();
        await testingServiceScope.InsertAsync(fakePersonOne);

        var input = $"""Title ^^ ["{fakePersonOne.Title.ToUpper()}"]""";

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();
        
        // Assert
        people.FirstOrDefault(x => x.Id == fakePersonOne.Id).Should().BeNull();
    }

    [Fact]
    public async Task can_handle_not_in_for_int()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithAge(77435)
            .Build();
        var fakePersonTwo = new FakeTestingPersonBuilder()
            .WithAge(33451)
            .Build();
        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo);

        var input = """Age !^^ [77435]""";

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();
        
        // Assert
        people.Count.Should().BeGreaterOrEqualTo(1);
        people.FirstOrDefault(x => x.Id == fakePersonOne.Id).Should().BeNull();
        people.FirstOrDefault(x => x.Id == fakePersonTwo.Id).Should().NotBeNull();
    }

    [Fact]
    public async Task can_handle_case_insensitive_not_in_for_string()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var fakePersonOne = new FakeTestingPersonBuilder().Build();
        var fakePersonTwo = new FakeTestingPersonBuilder().Build();
        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo);

        var input = $"""Title !^^* ["{fakePersonOne.Title.ToUpper()}"]""";

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();
        
        // Assert
        people.Count.Should().BeGreaterOrEqualTo(1);
        people.FirstOrDefault(x => x.Id == fakePersonOne.Id).Should().BeNull();
        people.FirstOrDefault(x => x.Id == fakePersonTwo.Id).Should().NotBeNull();
    }

    [Fact]
    public async Task can_handle_case_sensitive_not_in_for_string()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var fakePersonOne = new FakeTestingPersonBuilder().Build();
        await testingServiceScope.InsertAsync(fakePersonOne);

        var input = $"""Title !^^ ["{fakePersonOne.Title}"]""";

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();
        
        // Assert
        people.FirstOrDefault(x => x.Id == fakePersonOne.Id).Should().BeNull();
    }

    [Fact]
    public async Task can_filter_on_child_entity()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var fakeAuthorOne = new FakeAuthorBuilder().Build();
        var fakeRecipeOne = new FakeRecipeBuilder().Build();
        fakeRecipeOne.SetAuthor(fakeAuthorOne);
        
        var fakeAuthorTwo = new FakeAuthorBuilder().Build();
        var fakeRecipeTwo = new FakeRecipeBuilder().Build();
        fakeRecipeTwo.SetAuthor(fakeAuthorTwo);
        await testingServiceScope.InsertAsync(fakeRecipeOne, fakeRecipeTwo);

        var input = $"""Author.Name == "{fakeAuthorOne.Name}" """;

        // Act
        var queryableRecipe = testingServiceScope.DbContext().Recipes
            .Include(x => x.Author);
        var appliedQueryable = queryableRecipe.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();
        
        // Assert
        people.Count.Should().Be(1);
        people.FirstOrDefault(x => x.Id == fakeRecipeOne.Id).Should().NotBeNull();
        people.FirstOrDefault(x => x.Id == fakeRecipeTwo.Id).Should().BeNull();
    }

    private class RecipeDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string AuthorName { get; set; }
        public string AuthorInfo { get; set; }
    }
    [Fact]
    public async Task can_filter_on_projection()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var fakeAuthorOne = new FakeAuthorBuilder().Build();
        var fakeRecipeOne = new FakeRecipeBuilder().Build();
        fakeRecipeOne.SetAuthor(fakeAuthorOne);
        
        var fakeAuthorTwo = new FakeAuthorBuilder().Build();
        var fakeRecipeTwo = new FakeRecipeBuilder().Build();
        fakeRecipeTwo.SetAuthor(fakeAuthorTwo);
        await testingServiceScope.InsertAsync(fakeRecipeOne, fakeRecipeTwo);

        var input = $"""title == "{fakeRecipeOne.Title}" """;

        var config = new QueryKitConfiguration(config =>
        {
            config.Property<RecipeDto>(x => x.Title).HasQueryName("title");
        });

        // Act
        var queryableRecipe = testingServiceScope.DbContext().Recipes
            .Include(x => x.Author)
            .Select(x => new RecipeDto
            {
                Id = x.Id,
                Title = x.Title,
                AuthorName = x.Author.Name
            });
        var appliedQueryable = queryableRecipe.ApplyQueryKitFilter(input, config);
        var recipes = await appliedQueryable.ToListAsync();
        
        // Assert
        recipes.Count.Should().Be(1);
        recipes.FirstOrDefault(x => x.Id == fakeRecipeOne.Id).Should().NotBeNull();
        recipes.FirstOrDefault(x => x.Id == fakeRecipeTwo.Id).Should().BeNull();
    }
    
    [Fact]
    public async Task can_filter_on_projections_nested()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var fakeAuthorOne = new FakeAuthorBuilder().Build();
        var fakeRecipeOne = new FakeRecipeBuilder().Build();
        fakeRecipeOne.SetAuthor(fakeAuthorOne);
        
        var fakeAuthorTwo = new FakeAuthorBuilder().Build();
        var fakeRecipeTwo = new FakeRecipeBuilder().Build();
        fakeRecipeTwo.SetAuthor(fakeAuthorTwo);
        await testingServiceScope.InsertAsync(fakeRecipeOne, fakeRecipeTwo);

        var input = $"""author == "{fakeAuthorOne.Name}" """;

        var config = new QueryKitConfiguration(config =>
        {
            config.Property<RecipeDto>(x => x.AuthorName).HasQueryName("author");
        });

        // Act
        var queryableRecipe = testingServiceScope.DbContext().Recipes
            .Include(x => x.Author)
            .Select(x => new RecipeDto
            {
                Id = x.Id,
                Title = x.Title,
                AuthorName = x.Author.Name
            });
        var appliedQueryable = queryableRecipe.ApplyQueryKitFilter(input, config);
        var recipes = await appliedQueryable.ToListAsync();
        
        // Assert
        recipes.Count.Should().Be(1);
        recipes.FirstOrDefault(x => x.Id == fakeRecipeOne.Id).Should().NotBeNull();
        recipes.FirstOrDefault(x => x.Id == fakeRecipeTwo.Id).Should().BeNull();
    }
    
    [Fact]
    public async Task can_filter_on_projections_nested_complex()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var fakeAuthorOne = new FakeAuthorBuilder().Build();
        var fakeRecipeOne = new FakeRecipeBuilder().Build();
        fakeRecipeOne.SetAuthor(fakeAuthorOne);
        
        var fakeAuthorTwo = new FakeAuthorBuilder().Build();
        var fakeRecipeTwo = new FakeRecipeBuilder().Build();
        fakeRecipeTwo.SetAuthor(fakeAuthorTwo);
        await testingServiceScope.InsertAsync(fakeRecipeOne, fakeRecipeTwo);

        var input = $"""info @=* "{fakeAuthorOne.Name}" """;

        var config = new QueryKitConfiguration(config =>
        {
            config.Property<RecipeDto>(x => x.AuthorInfo).HasQueryName("info");
        });

        // Act
        var queryableRecipe = testingServiceScope.DbContext().Recipes
            .Include(x => x.Author)
            .Select(x => new RecipeDto
            {
                Id = x.Id,
                Title = x.Title,
                AuthorName = x.Author.Name,
                AuthorInfo = x.Author.Name + " - " + x.Author.InternalIdentifier
            });
        var appliedQueryable = queryableRecipe.ApplyQueryKitFilter(input, config);
        var recipes = await appliedQueryable.ToListAsync();
        
        // Assert
        recipes.Count.Should().Be(1);
        recipes.FirstOrDefault(x => x.Id == fakeRecipeOne.Id).Should().NotBeNull();
        recipes.FirstOrDefault(x => x.Id == fakeRecipeTwo.Id).Should().BeNull();
    }

    [Fact]
    public async Task can_filter_on_child_entity_with_config()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var fakeAuthorOne = new FakeAuthorBuilder().Build();
        var fakeRecipeOne = new FakeRecipeBuilder().Build();
        fakeRecipeOne.SetAuthor(fakeAuthorOne);
        
        var fakeAuthorTwo = new FakeAuthorBuilder().Build();
        var fakeRecipeTwo = new FakeRecipeBuilder().Build();
        fakeRecipeTwo.SetAuthor(fakeAuthorTwo);
        await testingServiceScope.InsertAsync(fakeRecipeOne, fakeRecipeTwo);

        var input = $"""author == "{fakeAuthorOne.Name}" """;

        var config = new QueryKitConfiguration(config =>
        {
            config.Property<Recipe>(x => x.Author.Name).HasQueryName("author");
        });
        
        // Act
        var queryableRecipe = testingServiceScope.DbContext().Recipes
            .Include(x => x.Author);
        var appliedQueryable = queryableRecipe.ApplyQueryKitFilter(input, config);
        var people = await appliedQueryable.ToListAsync();
        
        // Assert
        people.Count.Should().Be(1);
        people.FirstOrDefault(x => x.Id == fakeRecipeOne.Id).Should().NotBeNull();
        people.FirstOrDefault(x => x.Id == fakeRecipeTwo.Id).Should().BeNull();
    }
    
    [Fact]
    public async Task can_filter_with_child_props_for_complex_property()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var faker = new Faker();
        var fakePersonOne = new FakeRecipeBuilder()
            .WithCollectionEmail(faker.Internet.Email())
            .Build();
        var fakePersonTwo = new FakeRecipeBuilder()
            .Build();
        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo);
        
        var input = $"""CollectionEmail.Value == "{fakePersonOne.CollectionEmail.Value}" """;

        // Act
        var queryableRecipes = testingServiceScope.DbContext().Recipes;
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<Recipe>(x => x.CollectionEmail.Value);
        });
        var appliedQueryable = queryableRecipes.ApplyQueryKitFilter(input, config);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(fakePersonOne.Id);
    }
    
    [Fact]
    public async Task can_filter_with_child_props_for_aliased_complex_property()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var faker = new Faker();
        var fakePersonOne = new FakeRecipeBuilder()
            .WithCollectionEmail(faker.Internet.Email())
            .Build();
        var fakePersonTwo = new FakeRecipeBuilder()
            .Build();
        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo);
        
        var input = $"""email == "{fakePersonOne.CollectionEmail.Value}" """;

        // Act
        var queryableRecipes = testingServiceScope.DbContext().Recipes;
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<Recipe>(x => x.CollectionEmail.Value).HasQueryName("email");
        });
        var appliedQueryable = queryableRecipes.ApplyQueryKitFilter(input, config);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(fakePersonOne.Id);
    }
    
    [Fact]
    public async Task can_filter_with_child_props_for_null_aliased_complex_property()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var faker = new Faker();
        var fakePersonOne = new FakeRecipeBuilder()
            .WithCollectionEmail(null)
            .Build();
        var fakePersonTwo = new FakeRecipeBuilder()
            .Build();
        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo);
        
        var input = $"""email == null""";

        // Act
        var queryableRecipes = testingServiceScope.DbContext().Recipes;
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<Recipe>(x => x.CollectionEmail.Value).HasQueryName("email");
        });
        var appliedQueryable = queryableRecipes.ApplyQueryKitFilter(input, config);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(fakePersonOne.Id);
    }
    
    [Fact]
    public async Task can_filter_with_child_props_for_complex_property_with_alias()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var faker = new Faker();
        var fakePersonOne = new FakeRecipeBuilder()
            .WithCollectionEmail(faker.Internet.Email())
            .Build();
        var fakePersonTwo = new FakeRecipeBuilder()
            .Build();
        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo);
        
        var input = $"""email == "{fakePersonOne.CollectionEmail.Value}" """;

        // Act
        var queryableRecipes = testingServiceScope.DbContext().Recipes;
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<Recipe>(x => x.CollectionEmail.Value).HasQueryName("email");
        });
        var appliedQueryable = queryableRecipes.ApplyQueryKitFilter(input, config);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(fakePersonOne.Id);
    }
    
    [Fact]
    public async Task can_filter_by_enum_name()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var faker = new Faker();
        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithTitle(faker.Lorem.Sentence())
            .WithBirthMonth(BirthMonthEnum.January)
            .Build();
        var fakePersonTwo = new FakeTestingPersonBuilder().Build();
        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo);
        
        var input = $"""{nameof(TestingPerson.BirthMonth)} == "January" && {nameof(TestingPerson.Title)} == "{fakePersonOne.Title}" """;
        
        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(fakePersonOne.Id);
    }
    
    [Fact]
    public async Task can_filter_by_in_enum_names()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var faker = new Faker();
        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithTitle(faker.Lorem.Sentence())
            .WithBirthMonth(BirthMonthEnum.January)
            .Build();
        var fakePersonTwo = new FakeTestingPersonBuilder().Build();
        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo);
        
        var input = $"""{nameof(TestingPerson.BirthMonth)} ^^ ["January", "March"] && {nameof(TestingPerson.Title)} == "{fakePersonOne.Title}" """;
        
        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(fakePersonOne.Id);
    }
    
    [Fact]
    public async Task can_filter_by_enum_number()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var faker = new Faker();
        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithTitle(faker.Lorem.Sentence())
            .WithBirthMonth(BirthMonthEnum.June)
            .Build();
        var fakePersonTwo = new FakeTestingPersonBuilder().Build();
        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo);
        
        var input = $"""{nameof(TestingPerson.BirthMonth)} == "6" && {nameof(TestingPerson.Title)} == "{fakePersonOne.Title}" """;
        
        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(fakePersonOne.Id);
    }
    
    [Fact]
    public async Task can_filter_by_in_enum_numbers()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var faker = new Faker();
        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithTitle(faker.Lorem.Sentence())
            .WithBirthMonth(BirthMonthEnum.January)
            .Build();
        var fakePersonTwo = new FakeTestingPersonBuilder().Build();
        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo);
        
        var input = $"""{nameof(TestingPerson.BirthMonth)} ^^ ["1", "3"] && {nameof(TestingPerson.Title)} == "{fakePersonOne.Title}" """;
        
        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(fakePersonOne.Id);
    }
    
    [Fact]
    public async Task can_have_derived_prop_work_with_collection_filters()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var ingredient = new FakeIngredientBuilder().Build();
        var recipe = new FakeRecipeBuilder().Build();
        recipe.AddIngredient(ingredient);
        await testingServiceScope.InsertAsync(recipe);
        
        var input = $"""special_title_directions == "{recipe.Title + recipe.Directions}" && Ingredients.Name == "{ingredient.Name}" """;
        var config = new QueryKitConfiguration(config =>
        {
            config.DerivedProperty<Recipe>(x => x.Title + x.Directions).HasQueryName("special_title_directions");
        });
        var queryableRecipes = testingServiceScope.DbContext().Recipes;
        var appliedQueryable = queryableRecipes.ApplyQueryKitFilter(input, config);
        var recipes = await appliedQueryable.ToListAsync();

        // Assert
        recipes.Count.Should().Be(1);
        recipes[0].Id.Should().Be(recipe.Id);
    }
    
    
    [Fact]
    public async Task can_have_custom_prop_work_with_collection_filters()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var ingredient = new FakeIngredientBuilder().Build();
        var recipe = new FakeRecipeBuilder().Build();
        recipe.AddIngredient(ingredient);
        await testingServiceScope.InsertAsync(recipe);
        
        var input = $"""special_title == "{recipe.Title}" && Ingredients.Name == "{ingredient.Name}" """;
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<Recipe>(x => x.Title).HasQueryName("special_title");
        });
        var queryableRecipes = testingServiceScope.DbContext().Recipes;
        var appliedQueryable = queryableRecipes.ApplyQueryKitFilter(input, config);
        var recipes = await appliedQueryable.ToListAsync();

        // Assert
        recipes.Count.Should().Be(1);
        recipes[0].Id.Should().Be(recipe.Id);
    }

    [Fact]
    public async Task can_filter_on_db_sequence()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var author = new FakeAuthorBuilder().Build();
        var recipe = new FakeRecipeBuilder().Build();
        recipe.SetAuthor(author);
        await testingServiceScope.InsertAsync(recipe);
        
        var authorInsertWithId = await testingServiceScope.DbContext().Authors
            .FirstOrDefaultAsync(x => x.Id == author.Id);
        var lastFourCharsOfInternalId = authorInsertWithId!.InternalIdentifier[^4..];

        var input = $"""internalId @=* "{lastFourCharsOfInternalId}" """;

        var config = new QueryKitConfiguration(config =>
        {
            config.Property<Author>(x => x.InternalIdentifier).HasQueryName("internalId");
        });
        
        // Act
        var queryableRecipe = testingServiceScope.DbContext().Authors;
        var appliedQueryable = queryableRecipe.ApplyQueryKitFilter(input, config);
        var dbAuthor = await appliedQueryable.ToListAsync();
        
        // Assert
        dbAuthor.Count.Should().Be(1);
        dbAuthor.FirstOrDefault(x => x.Id == author.Id).Should().NotBeNull();
    }

    [Fact]
    public async Task can_filter_on_db_sequence_as_child()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var fakeAuthorOne = new FakeAuthorBuilder().Build();
        var fakeRecipeOne = new FakeRecipeBuilder().Build();
        fakeRecipeOne.SetAuthor(fakeAuthorOne);
        
        var fakeAuthorTwo = new FakeAuthorBuilder().Build();
        var fakeRecipeTwo = new FakeRecipeBuilder().Build();
        fakeRecipeTwo.SetAuthor(fakeAuthorTwo);
        await testingServiceScope.InsertAsync(fakeRecipeOne, fakeRecipeTwo);
        
        var authorInsertWithId = await testingServiceScope.DbContext().Authors
            .FirstOrDefaultAsync(x => x.Id == fakeAuthorOne.Id);
        var lastFourCharsOfInternalId = authorInsertWithId!.InternalIdentifier[^4..];

        var input = $"""internalId @=* "{lastFourCharsOfInternalId}" """;
        
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<Recipe>(x => x.Author.InternalIdentifier).HasQueryName("internalId");
        });

        // Act
        var queryableRecipe = testingServiceScope.DbContext().Recipes
            .Include(x => x.Author);
        var appliedQueryable = queryableRecipe.ApplyQueryKitFilter(input, config);
        var people = await appliedQueryable.ToListAsync();
        
        // Assert
        people.Count.Should().Be(1);
        people.FirstOrDefault(x => x.Id == fakeRecipeOne.Id).Should().NotBeNull();
        people.FirstOrDefault(x => x.Id == fakeRecipeTwo.Id).Should().BeNull();
    }

    [Fact]
    public async Task can_filter_with_property_to_property_string_comparison()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var faker = new Faker();
        
        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithFirstName("John")
            .WithLastName("John")
            .WithTitle(faker.Lorem.Sentence())
            .Build();
        
        var fakePersonTwo = new FakeTestingPersonBuilder()
            .WithFirstName("Jane")
            .WithLastName("Doe")
            .WithTitle(faker.Lorem.Sentence())
            .Build();
        
        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo);
        
        var input = $"""FirstName == LastName && Title == "{fakePersonOne.Title}" """;
        
        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(fakePersonOne.Id);
        people[0].FirstName.Should().Be(people[0].LastName);
    }

    [Fact]
    public async Task can_filter_with_property_to_property_numeric_comparison()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var faker = new Faker();
        
        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithAge(25)
            .WithRating(20.5m)
            .WithTitle(faker.Lorem.Sentence())
            .Build();
        
        var fakePersonTwo = new FakeTestingPersonBuilder() 
            .WithAge(30)
            .WithRating(35.0m)
            .WithTitle(faker.Lorem.Sentence())
            .Build();
        
        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo);
        
        var input = $"""Age > Rating && Title == "{fakePersonOne.Title}" """;
        
        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(fakePersonOne.Id);
        people[0].Age.Should().BeGreaterThan((int)people[0].Rating!.Value);
    }

    [Fact]
    public async Task can_filter_with_property_to_property_different_operators()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var faker = new Faker();
        
        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithFirstName("Alice")
            .WithLastName("Bob") 
            .WithAge(25)
            .WithRating(20.5m)
            .WithTitle(faker.Lorem.Sentence())
            .Build();
        
        await testingServiceScope.InsertAsync(fakePersonOne);
        
        var input = $"""FirstName != LastName && Age > Rating && Title == "{fakePersonOne.Title}" """;
        
        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(fakePersonOne.Id);
        people[0].FirstName.Should().NotBe(people[0].LastName);
        people[0].Age.Should().BeGreaterThan((int)people[0].Rating!.Value);
    }

    [Fact]
    public async Task can_filter_with_property_to_property_combined_with_literal_values()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var faker = new Faker();
        
        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithFirstName("John")
            .WithLastName("John")
            .WithAge(25)
            .WithTitle(faker.Lorem.Sentence())
            .Build();
        
        var fakePersonTwo = new FakeTestingPersonBuilder()
            .WithFirstName("Jane")
            .WithLastName("Doe")
            .WithAge(18)
            .WithTitle(faker.Lorem.Sentence())
            .Build();
        
        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo);
        
        var input = $"""FirstName == LastName && Age > 21 && Title == "{fakePersonOne.Title}" """;
        
        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert - Only fakePersonOne matches both conditions
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(fakePersonOne.Id);
        people[0].FirstName.Should().Be(people[0].LastName);
        people[0].Age.Should().BeGreaterThan(21);
    }

    [Fact]
    public async Task can_filter_with_property_to_property_child_properties()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var faker = new Faker();
        
        var matchingName = "TestAuthor123";
        var fakeAuthor = new FakeAuthorBuilder()
            .WithName(matchingName)
            .Build();
        
        var fakeRecipe = new FakeRecipeBuilder()
            .WithTitle(matchingName) // Same as author name
            .Build();
        fakeRecipe.SetAuthor(fakeAuthor);
        
        var differentAuthor = new FakeAuthorBuilder()
            .WithName("DifferentAuthor")
            .Build();
        
        var differentRecipe = new FakeRecipeBuilder()
            .WithTitle("DifferentTitle")
            .Build();
        differentRecipe.SetAuthor(differentAuthor);
        
        await testingServiceScope.InsertAsync(fakeRecipe, differentRecipe);
        
        var input = """Author.Name == Title""";
        
        // Act
        var queryableRecipes = testingServiceScope.DbContext().Recipes
            .Include(x => x.Author);
        var appliedQueryable = queryableRecipes.ApplyQueryKitFilter(input);
        var recipes = await appliedQueryable.ToListAsync();

        // Assert
        recipes.Count.Should().Be(1);
        recipes[0].Id.Should().Be(fakeRecipe.Id);
        recipes[0].Author.Name.Should().Be(recipes[0].Title);
    }

    [Fact]
    public async Task can_filter_with_property_to_property_child_property_with_nested_access()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var faker = new Faker();
        
        var matchingValue = "matching@test.com";
        var fakeRecipe = new FakeRecipeBuilder()
            .WithTitle(matchingValue)
            .WithCollectionEmail(matchingValue) // Recipe.CollectionEmail.Value should equal Recipe.Title
            .Build();
        
        var differentRecipe = new FakeRecipeBuilder()
            .WithTitle("different-title")
            .WithCollectionEmail("different@email.com")
            .Build();
        
        await testingServiceScope.InsertAsync(fakeRecipe, differentRecipe);
        
        var input = """CollectionEmail.Value == Title""";
        
        // Act
        var queryableRecipes = testingServiceScope.DbContext().Recipes;
        var appliedQueryable = queryableRecipes.ApplyQueryKitFilter(input);
        var recipes = await appliedQueryable.ToListAsync();

        // Assert
        recipes.Count.Should().Be(1);
        recipes[0].Id.Should().Be(fakeRecipe.Id);
        recipes[0].CollectionEmail.Value.Should().Be(recipes[0].Title);
    }

    [Fact]
    public async Task can_filter_with_property_to_property_mixed_child_and_root_properties()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var faker = new Faker();
        
        var authorName = "TestAuthor";
        var rating = 5;
        
        var fakeAuthor = new FakeAuthorBuilder()
            .WithName(authorName)
            .Build();
        
        var fakeRecipe = new FakeRecipeBuilder()
            .WithTitle("SomeTitle")
            .WithRating(rating)
            .Build();
        fakeRecipe.SetAuthor(fakeAuthor);
        
        // Add a test person with matching values
        var fakePerson = new FakeTestingPersonBuilder()
            .WithFirstName(authorName) // Person.FirstName == Recipe.Author.Name
            .WithAge(rating) // Person.Age == Recipe.Rating  
            .WithTitle(faker.Lorem.Sentence())
            .Build();
        
        await testingServiceScope.InsertAsync(fakeRecipe);
        await testingServiceScope.InsertAsync(fakePerson);
        
        var input = $"""FirstName == "{authorName}" && Age == {rating} && Title == "{fakePerson.Title}" """;
        
        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(fakePerson.Id);
        people[0].FirstName.Should().Be(authorName);
        people[0].Age.Should().Be(rating);
    }
}
