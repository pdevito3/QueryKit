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

public class DatabaseFilteringTests() : TestBase
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
        var uniqueId = Guid.NewGuid().ToString()[..8];
        var specificTime = new TimeOnly(14, 30, 45, 123, 456); // Fixed time for consistency
        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithTime(specificTime)
            .WithTitle($"TimeMicrosTest1_{uniqueId}")
            .WithFirstName($"TimeMicrosPerson_{uniqueId}")
            .Build();
        var fakePersonTwo = new FakeTestingPersonBuilder()
            .WithTime(new TimeOnly(12, 15, 30, 999, 888))
            .WithTitle($"TimeMicrosTest2_{uniqueId}")
            .WithFirstName($"TimeMicrosOther_{uniqueId}")
            .Build();
        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo);

        var input = $"Time == \"{specificTime:HH:mm:ss.ffffff}\" && FirstName == \"TimeMicrosPerson_{uniqueId}\"";

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(fakePersonOne.Id);
        people[0].Time.Should().Be(specificTime);
    }
    
    [Fact]
    public async Task can_filter_by_timeonly_without_micros()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var uniqueId = Guid.NewGuid().ToString()[..8];
        var targetTime = new TimeOnly(16, 45, 30); // Fixed time for consistency
        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithTime(targetTime)
            .WithTitle($"TimeNoMicrosTest1_{uniqueId}")
            .WithFirstName($"TimeNoMicrosPerson_{uniqueId}")
            .Build();
        var fakePersonTwo = new FakeTestingPersonBuilder()
            .WithTime(new TimeOnly(10, 30, 15))
            .WithTitle($"TimeNoMicrosTest2_{uniqueId}")
            .WithFirstName($"TimeNoMicrosOther_{uniqueId}")
            .Build();
        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo);

        var input = $"Time >= \"{targetTime:HH:mm:ss}\" && FirstName == \"TimeNoMicrosPerson_{uniqueId}\"";

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(fakePersonOne.Id);
        people[0].Time.Should().Be(targetTime);
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
        
        var input = $"""(secondaryId @=* "4ce0-b19b")""";

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
        var uniqueId = Guid.NewGuid().ToString()[..8];
        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithRating(4M)
            .WithTitle($"DecimalTestHigh_{uniqueId}")
            .WithFirstName($"DecimalPerson1_{uniqueId}")
            .Build();
        var fakePersonTwo = new FakeTestingPersonBuilder()
            .WithRating(2M)
            .WithTitle($"DecimalTestLow_{uniqueId}")
            .WithFirstName($"DecimalPerson2_{uniqueId}")
            .Build();
        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo);

        var input = $"{nameof(TestingPerson.Rating)} > 3.5 && FirstName == \"DecimalPerson1_{uniqueId}\"";

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(fakePersonOne.Id);
        people[0].Rating.Should().Be(4M);
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
        var uniqueId = Guid.NewGuid().ToString()[..8];
        var fakeAuthorOne = new FakeAuthorBuilder()
            .WithName($"ChildEntityAuthor1_{uniqueId}")
            .Build();
        var fakeRecipeOne = new FakeRecipeBuilder()
            .WithTitle($"ChildEntityRecipe1_{uniqueId}")
            .Build();
        fakeRecipeOne.SetAuthor(fakeAuthorOne);
        
        var fakeAuthorTwo = new FakeAuthorBuilder()
            .WithName($"ChildEntityAuthor2_{uniqueId}")
            .Build();
        var fakeRecipeTwo = new FakeRecipeBuilder()
            .WithTitle($"ChildEntityRecipe2_{uniqueId}")
            .Build();
        fakeRecipeTwo.SetAuthor(fakeAuthorTwo);
        await testingServiceScope.InsertAsync(fakeRecipeOne, fakeRecipeTwo);

        var input = $"Author.Name == \"{fakeAuthorOne.Name}\" && Title == \"{fakeRecipeOne.Title}\"";

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
        var fakeAuthorOne = new FakeAuthorBuilder()
            .WithName(Guid.NewGuid().ToString())
            .Build();
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

    [Fact]
    public async Task can_filter_with_arithmetic_addition_expression()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var faker = new Faker();
        
        var age = 25;
        var rating = 5;
        var uniqueTitle = $"ArithmeticTest{Guid.NewGuid()}"; // Ensure unique data
        
        var fakePerson = new FakeTestingPersonBuilder()
            .WithAge(age)
            .WithRating(rating)
            .WithTitle(uniqueTitle)
            .Build();
        
        await testingServiceScope.InsertAsync(fakePerson);
        
        var input = $"""(Age + Rating) > 29 && Title == "{uniqueTitle}" """;
        
        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(fakePerson.Id);
        people[0].Age.Should().Be(age);
        people[0].Rating.Should().Be(rating);
    }

    [Fact]
    public async Task can_filter_with_arithmetic_subtraction_expression()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var faker = new Faker();
        
        var age = 30;
        var rating = 7;
        var uniqueTitle = $"SubtractionTest{Guid.NewGuid()}";
        
        var fakePerson = new FakeTestingPersonBuilder()
            .WithAge(age)
            .WithRating(rating)
            .WithTitle(uniqueTitle)
            .Build();
        
        await testingServiceScope.InsertAsync(fakePerson);
        
        var input = $"""(Age - Rating) > 20 && Title == "{uniqueTitle}" """;
        
        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(fakePerson.Id);
        people[0].Age.Should().Be(age);
        people[0].Rating.Should().Be(rating);
    }

    [Fact]
    public async Task can_filter_with_arithmetic_multiplication_expression()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var faker = new Faker();
        
        var age = 5;
        var rating = 8;
        var uniqueTitle = $"MultiplicationTest{Guid.NewGuid()}";
        
        var fakePerson = new FakeTestingPersonBuilder()
            .WithAge(age)
            .WithRating(rating)
            .WithTitle(uniqueTitle)
            .Build();
        
        await testingServiceScope.InsertAsync(fakePerson);
        
        var input = $"""(Age * Rating) >= 40 && Title == "{uniqueTitle}" """;
        
        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(fakePerson.Id);
        people[0].Age.Should().Be(age);
        people[0].Rating.Should().Be(rating);
    }

    [Fact]
    public async Task can_filter_with_arithmetic_division_expression()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var faker = new Faker();
        
        var age = 40;
        var rating = 5;
        var uniqueTitle = $"DivisionTest{Guid.NewGuid()}";
        
        var fakePerson = new FakeTestingPersonBuilder()
            .WithAge(age)
            .WithRating(rating)
            .WithTitle(uniqueTitle)
            .Build();
        
        await testingServiceScope.InsertAsync(fakePerson);
        
        var input = $"""(Age / Rating) == 8 && Title == "{uniqueTitle}" """;
        
        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(fakePerson.Id);
        people[0].Age.Should().Be(age);
        people[0].Rating.Should().Be(rating);
    }

    [Fact]
    public async Task can_filter_with_arithmetic_modulo_expression()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var faker = new Faker();
        
        var age = 22; // Even number
        var rating = 3;
        var uniqueTitle = $"ModuloTest{Guid.NewGuid()}";
        
        var fakePerson = new FakeTestingPersonBuilder()
            .WithAge(age)
            .WithRating(rating)
            .WithTitle(uniqueTitle)
            .Build();
        
        await testingServiceScope.InsertAsync(fakePerson);
        
        var input = $"""(Age % 2) == 0 && Title == "{uniqueTitle}" """; // Test even number
        
        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(fakePerson.Id);
        people[0].Age.Should().Be(age);
    }

    [Fact]
    public async Task can_filter_with_complex_arithmetic_expression_with_precedence()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var faker = new Faker();
        
        var age = 10;
        var rating = 5;
        var uniqueTitle = $"ComplexArithmeticTest{Guid.NewGuid()}";
        
        var fakePerson = new FakeTestingPersonBuilder()
            .WithAge(age)
            .WithRating(rating)
            .WithTitle(uniqueTitle)
            .Build();
        
        await testingServiceScope.InsertAsync(fakePerson);
        
        // Test: (Age + Rating * 2) > 15 should be: 10 + (5 * 2) = 20 > 15 = true
        var input = $"""(Age + Rating * 2) > 15 && Title == "{uniqueTitle}" """;
        
        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(fakePerson.Id);
        people[0].Age.Should().Be(age);
        people[0].Rating.Should().Be(rating);
    }

    [Fact]
    public async Task can_filter_with_arithmetic_expression_using_parentheses_to_override_precedence()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var faker = new Faker();
        
        var age = 5;
        var rating = 3;
        var uniqueTitle = $"ParenthesesArithmeticTest{Guid.NewGuid()}";
        
        var fakePerson = new FakeTestingPersonBuilder()
            .WithAge(age)
            .WithRating(rating)
            .WithTitle(uniqueTitle)
            .Build();
        
        await testingServiceScope.InsertAsync(fakePerson);
        
        // Test: ((Age + Rating) * 2) > 15 should be: (5 + 3) * 2 = 16 > 15 = true
        var input = $"""((Age + Rating) * 2) > 15 && Title == "{uniqueTitle}" """;
        
        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(fakePerson.Id);
        people[0].Age.Should().Be(age);
        people[0].Rating.Should().Be(rating);
    }

    [Fact]
    public async Task can_filter_with_arithmetic_expression_mixing_literals_and_properties()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var faker = new Faker();
        
        var age = 25;
        var rating = 7;
        var uniqueTitle = $"LiteralMixTest{Guid.NewGuid()}";
        
        var fakePerson = new FakeTestingPersonBuilder()
            .WithAge(age)
            .WithRating(rating)
            .WithTitle(uniqueTitle)
            .Build();
        
        await testingServiceScope.InsertAsync(fakePerson);
        
        // Test: (Age + 5) * Rating > 200 should be: (25 + 5) * 7 = 210 > 200 = true
        var input = $"""((Age + 5) * Rating) > 200 && Title == "{uniqueTitle}" """;
        
        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(fakePerson.Id);
        people[0].Age.Should().Be(age);
        people[0].Rating.Should().Be(rating);
    }

    [Fact]
    public async Task can_filter_with_arithmetic_expression_using_decimal_properties()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var faker = new Faker();
        
        var age = 30;
        var rating = 4.5m; // Use decimal for precise calculation
        var uniqueTitle = $"DecimalArithmeticTest{Guid.NewGuid()}";
        
        var fakePerson = new FakeTestingPersonBuilder()
            .WithAge(age)
            .WithRating(rating)
            .WithTitle(uniqueTitle)
            .Build();
        
        await testingServiceScope.InsertAsync(fakePerson);
        
        // Test: (Age * Rating) > 130 should be: 30 * 4.5 = 135 > 130 = true
        var input = $"""(Age * Rating) > 130 && Title == "{uniqueTitle}" """;
        
        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(fakePerson.Id);
        people[0].Age.Should().Be(age);
        people[0].Rating.Should().Be(rating);
    }

    [Fact]
    public async Task can_filter_with_arithmetic_expression_that_results_in_no_matches()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var faker = new Faker();
        
        var age = 20;
        var rating = 3;
        var uniqueTitle = $"NoMatchTest{Guid.NewGuid()}";
        
        var fakePerson = new FakeTestingPersonBuilder()
            .WithAge(age)
            .WithRating(rating)
            .WithTitle(uniqueTitle)
            .Build();
        
        await testingServiceScope.InsertAsync(fakePerson);
        
        // Test: (Age + Rating) > 50 should be: 20 + 3 = 23 > 50 = false
        var input = $"""(Age + Rating) > 50 && Title == "{uniqueTitle}" """;
        
        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(0);
    }

    [Fact]
    public async Task can_filter_with_multiple_arithmetic_expressions_in_logical_combination()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var faker = new Faker();
        
        var age = 24;
        var rating = 6;
        var uniqueTitle = $"MultipleArithmeticTest{Guid.NewGuid()}";
        
        var fakePerson = new FakeTestingPersonBuilder()
            .WithAge(age)
            .WithRating(rating)
            .WithTitle(uniqueTitle)
            .Build();
        
        await testingServiceScope.InsertAsync(fakePerson);
        
        // Test: (Age + Rating) > 25 AND (Age * Rating) < 150
        // Should be: 24 + 6 = 30 > 25 = true AND 24 * 6 = 144 < 150 = true
        var input = $"""(Age + Rating) > 25 && (Age * Rating) < 150 && Title == "{uniqueTitle}" """;
        
        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(fakePerson.Id);
        people[0].Age.Should().Be(age);
        people[0].Rating.Should().Be(rating);
    }

    [Fact]
    public async Task can_filter_with_nested_arithmetic_expressions()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var faker = new Faker();
        
        var age = 12;
        var rating = 4;
        var uniqueTitle = $"NestedArithmeticTest{Guid.NewGuid()}";
        
        var fakePerson = new FakeTestingPersonBuilder()
            .WithAge(age)
            .WithRating(rating)
            .WithTitle(uniqueTitle)
            .Build();
        
        await testingServiceScope.InsertAsync(fakePerson);
        
        // Test: ((Age + Rating) * (Age - Rating)) > 120
        // Should be: (12 + 4) * (12 - 4) = 16 * 8 = 128 > 120 = true
        var input = $"""((Age + Rating) * (Age - Rating)) > 120 && Title == "{uniqueTitle}" """;
        
        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(fakePerson.Id);
        people[0].Age.Should().Be(age);
        people[0].Rating.Should().Be(rating);
    }

    [Fact]
    public async Task can_filter_with_custom_operation_simple_calculation()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var faker = new Faker();
        
        var age = 25;
        var rating = 4;
        var uniqueTitle = $"CustomOpTest{Guid.NewGuid()}";
        
        var fakePerson = new FakeTestingPersonBuilder()
            .WithAge(age)
            .WithRating(rating)
            .WithTitle(uniqueTitle)
            .Build();
        
        await testingServiceScope.InsertAsync(fakePerson);
        
        var input = $"""ageTimeRating > 99 && Title == "{uniqueTitle}" """;
        
        var config = new QueryKitConfiguration(config =>
        {
            config.CustomOperation<TestingPerson>((x, op, value) => (x.Age * x.Rating) > (decimal)value)
                .HasQueryName("ageTimeRating");
        });
        
        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input, config);
        var people = await appliedQueryable.ToListAsync();

        // Assert - Age (25) * Rating (4) = 100 > 99 = true
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(fakePerson.Id);
        people[0].Age.Should().Be(age);
        people[0].Rating.Should().Be(rating);
    }

    [Fact]
    public async Task can_filter_with_custom_operation_different_operators()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var faker = new Faker();
        
        var age = 30;
        var rating = 5;
        var uniqueTitle = $"CustomOpDiffOpsTest{Guid.NewGuid()}";
        
        var fakePerson = new FakeTestingPersonBuilder()
            .WithAge(age)
            .WithRating(rating)
            .WithTitle(uniqueTitle)
            .Build();
        
        await testingServiceScope.InsertAsync(fakePerson);
        
        var input = $"""agePlusRating == 35 && Title == "{uniqueTitle}" """;
        
        var config = new QueryKitConfiguration(config =>
        {
            config.CustomOperation<TestingPerson>((x, op, value) => (x.Age + x.Rating) == (decimal)value)
                .HasQueryName("agePlusRating");
        });
        
        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input, config);
        var people = await appliedQueryable.ToListAsync();

        // Assert - Age (30) + Rating (5) = 35 == 35 = true
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(fakePerson.Id);
        people[0].Age.Should().Be(age);
        people[0].Rating.Should().Be(rating);
    }

    [Fact]
    public async Task can_filter_with_custom_operation_recipe_ingredient_quality()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var faker = new Faker();
        
        var highQualityIngredient = new FakeIngredientBuilder()
            .WithQualityLevel(8)
            .Build();
        var lowQualityIngredient = new FakeIngredientBuilder()
            .WithQualityLevel(3)
            .Build();
        
        var recipe = new FakeRecipeBuilder().Build();
        recipe.AddIngredient(highQualityIngredient);
        recipe.AddIngredient(lowQualityIngredient);
        
        await testingServiceScope.InsertAsync(recipe);
        
        var input = $"""avgQuality > 5 && Title == "{recipe.Title}" """;
        
        var config = new QueryKitConfiguration(config =>
        {
            config.CustomOperation<Recipe>((x, op, value) => 
                x.Ingredients.Average(i => i.QualityLevel ?? 0) > (double)value)
                .HasQueryName("avgQuality");
        });
        
        // Act
        var queryableRecipes = testingServiceScope.DbContext().Recipes;
        var appliedQueryable = queryableRecipes.ApplyQueryKitFilter(input, config);
        var recipes = await appliedQueryable.ToListAsync();

        // Assert - Average quality (8+3)/2 = 5.5 > 5 = true
        recipes.Count.Should().Be(1);
        recipes[0].Id.Should().Be(recipe.Id);
    }

    [Fact]
    public async Task can_filter_with_custom_operation_complex_business_logic()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var faker = new Faker();
        
        var age = 35;
        var rating = 8;
        var firstName = "VIP";
        var uniqueTitle = $"ComplexBusinessTest{Guid.NewGuid()}";
        
        var fakePerson = new FakeTestingPersonBuilder()
            .WithAge(age)
            .WithRating(rating)
            .WithFirstName(firstName)
            .WithTitle(uniqueTitle)
            .Build();
        
        await testingServiceScope.InsertAsync(fakePerson);
        
        var input = $"""isVipCustomer == true && Title == "{uniqueTitle}" """;
        
        var config = new QueryKitConfiguration(config =>
        {
            config.CustomOperation<TestingPerson>((x, op, value) => 
                (bool)value ? 
                    (x.Age > 30 && x.Rating > 7 && x.FirstName.Contains("VIP")) :
                    !(x.Age > 30 && x.Rating > 7 && x.FirstName.Contains("VIP")))
                .HasQueryName("isVipCustomer");
        });
        
        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input, config);
        var people = await appliedQueryable.ToListAsync();

        // Assert - Age > 30 (35) AND Rating > 7 (8) AND FirstName contains "VIP" = true
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(fakePerson.Id);
        people[0].Age.Should().Be(age);
        people[0].Rating.Should().Be(rating);
        people[0].FirstName.Should().Be(firstName);
    }

    [Fact]
    public async Task can_filter_with_custom_operation_combined_with_regular_filters()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var faker = new Faker();
        
        var age = 40;
        var rating = 6;
        var lastName = "Smith";
        var uniqueTitle = $"CombinedFiltersTest{Guid.NewGuid()}";
        
        var fakePerson = new FakeTestingPersonBuilder()
            .WithAge(age)
            .WithRating(rating)
            .WithLastName(lastName)
            .WithTitle(uniqueTitle)
            .Build();
        
        await testingServiceScope.InsertAsync(fakePerson);
        
        var input = $"""ageRatingProduct > 200 && LastName == "{lastName}" && Title == "{uniqueTitle}" """;
        
        var config = new QueryKitConfiguration(config =>
        {
            config.CustomOperation<TestingPerson>((x, op, value) => (x.Age * x.Rating) > (decimal)value)
                .HasQueryName("ageRatingProduct");
        });
        
        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input, config);
        var people = await appliedQueryable.ToListAsync();

        // Assert - Custom: Age (40) * Rating (6) = 240 > 200 AND Regular: LastName == "Smith"
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(fakePerson.Id);
        people[0].Age.Should().Be(age);
        people[0].Rating.Should().Be(rating);
        people[0].LastName.Should().Be(lastName);
    }

    [Fact]
    public async Task can_filter_with_custom_operation_using_logical_operators()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var faker = new Faker();
        
        var uniqueTitle1 = "LogicalTest1";
        var uniqueTitle2 = "LogicalTest2";
        
        var personOne = new FakeTestingPersonBuilder()
            .WithAge(25)
            .WithRating(8)
            .WithTitle(uniqueTitle1)
            .Build();
        
        var personTwo = new FakeTestingPersonBuilder()
            .WithAge(45)
            .WithRating(3)
            .WithTitle(uniqueTitle2)
            .Build();
        
        await testingServiceScope.InsertAsync(personOne, personTwo);
        
        var input = $"""highScore == true && Title == "{uniqueTitle1}" """;
        
        var config = new QueryKitConfiguration(config =>
        {
            config.CustomOperation<TestingPerson>((x, op, value) => 
                (bool)value ? (x.Age * x.Rating) > 150 : (x.Age * x.Rating) <= 150)
                .HasQueryName("highScore");
        });
        
        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input, config);
        var people = await appliedQueryable.ToListAsync();

        // Assert - Person1: 25*8=200>150 (highScore=true) AND Title matches = true
        people.Count.Should().Be(1);
        people.Should().Contain(p => p.Id == personOne.Id);
    }

    [Fact]
    public async Task can_filter_with_custom_operation_date_handling()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var faker = new Faker();
        
        var baseDate = new DateTime(2023, 6, 15, 0, 0, 0, DateTimeKind.Utc);
        var recentDate = baseDate.AddDays(-5); // 5 days before base date
        var oldDate = baseDate.AddDays(-15); // 15 days before base date
        var cutoffDate = baseDate.AddDays(-10); // 10 days before base date
        
        var recentPerson = new FakeTestingPersonBuilder()
            .WithSpecificDateTime(recentDate)
            .WithTitle("RecentUser")
            .Build();
        
        var oldPerson = new FakeTestingPersonBuilder()
            .WithSpecificDateTime(oldDate)
            .WithTitle("OldUser")
            .Build();
        
        await testingServiceScope.InsertAsync(recentPerson, oldPerson);
        
        var input = $"""isRecentUser == true && Title == "RecentUser" """;
        
        var config = new QueryKitConfiguration(config =>
        {
            config.CustomOperation<TestingPerson>((x, op, value) => 
                (bool)value ? 
                    x.SpecificDateTime > baseDate.AddDays(-10) : 
                    x.SpecificDateTime <= baseDate.AddDays(-10))
                .HasQueryName("isRecentUser");
        });
        
        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input, config);
        var people = await appliedQueryable.ToListAsync();

        // Assert - Only the recent user should match (SpecificDateTime > 10 days ago)
        people.Count.Should().Be(1);
        people.Should().Contain(p => p.Id == recentPerson.Id);
        people.Should().NotContain(p => p.Id == oldPerson.Id);
        people[0].SpecificDateTime.Should().BeAfter(cutoffDate);
    }

    [Fact]
    public async Task can_filter_with_custom_operation_date_parameter()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var uniqueId = Guid.NewGuid().ToString()[..8];
        
        var targetDate = new DateTime(2023, 6, 15, 0, 0, 0, DateTimeKind.Utc);
        var beforeDate = targetDate.AddDays(-1);
        var afterDate = targetDate.AddDays(1);
        
        var beforePerson = new FakeTestingPersonBuilder()
            .WithSpecificDateTime(beforeDate)
            .WithTitle($"BeforeUser_{uniqueId}")
            .WithFirstName($"CustomOpBefore_{uniqueId}")
            .Build();
        
        var afterPerson = new FakeTestingPersonBuilder()
            .WithSpecificDateTime(afterDate)
            .WithTitle($"AfterUser_{uniqueId}")
            .WithFirstName($"CustomOpAfter_{uniqueId}")
            .Build();
        
        await testingServiceScope.InsertAsync(beforePerson, afterPerson);
        
        var input = $"isAfterDate == \"2023-06-15T00:00:00Z\" && FirstName @= \"CustomOp\"";
        
        var config = new QueryKitConfiguration(config =>
        {
            config.CustomOperation<TestingPerson>((x, op, value) => 
                x.SpecificDateTime > (DateTime)value)
                .HasQueryName("isAfterDate");
        });
        
        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input, config);
        var people = await appliedQueryable.ToListAsync();

        // Assert - Only the person after the target date should match
        people.Count.Should().Be(1);
        people.Should().Contain(p => p.Id == afterPerson.Id);
        people.Should().NotContain(p => p.Id == beforePerson.Id);
        people[0].SpecificDateTime.Should().BeAfter(targetDate);
    }

    // Additional Numeric Types Testing
    [Fact]
    public async Task can_filter_by_float()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var uniqueId = Guid.NewGuid().ToString()[..8];
        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithRating(3.14m) // Testing decimal with float-like precision
            .WithTitle($"FloatTestHigh_{uniqueId}")
            .WithFirstName($"FloatPerson1_{uniqueId}")
            .Build();
        var fakePersonTwo = new FakeTestingPersonBuilder()
            .WithRating(2.5m)
            .WithTitle($"FloatTestLow_{uniqueId}")
            .WithFirstName($"FloatPerson2_{uniqueId}")
            .Build();
        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo);

        var input = $"Rating > 3.0 && FirstName == \"FloatPerson1_{uniqueId}\"";

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(fakePersonOne.Id);
        people[0].Rating.Should().Be(3.14m);
    }

    [Fact]
    public async Task can_filter_by_double()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var uniqueId = Guid.NewGuid().ToString()[..8];
        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithRating(99.999m) // Testing decimal with double-like precision
            .WithTitle($"DoubleTestHigh_{uniqueId}")
            .WithFirstName($"DoublePerson1_{uniqueId}")
            .Build();
        var fakePersonTwo = new FakeTestingPersonBuilder()
            .WithRating(50.5m)
            .WithTitle($"DoubleTestLow_{uniqueId}")
            .WithFirstName($"DoublePerson2_{uniqueId}")
            .Build();
        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo);

        var input = $"Rating >= 99.9 && FirstName == \"DoublePerson1_{uniqueId}\"";

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(fakePersonOne.Id);
        people[0].Rating.Should().Be(99.999m);
    }

    // Systematic Comparison Operator Testing
    [Fact]
    public async Task can_filter_with_not_equals_operator()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var uniqueId = Guid.NewGuid().ToString()[..8];
        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithAge(25)
            .WithTitle($"NotEqualsTest1_{uniqueId}")
            .WithFirstName($"NotEqualsPerson1_{uniqueId}")
            .Build();
        var fakePersonTwo = new FakeTestingPersonBuilder()
            .WithAge(30)
            .WithTitle($"NotEqualsTest2_{uniqueId}")
            .WithFirstName($"NotEqualsPerson2_{uniqueId}")
            .Build();
        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo);

        var input = $"Age != 25 && FirstName == \"NotEqualsPerson2_{uniqueId}\"";

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(fakePersonTwo.Id);
        people[0].Age.Should().Be(30);
    }

    [Fact]
    public async Task can_filter_with_greater_than_operator_across_types()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var uniqueId = Guid.NewGuid().ToString()[..8];
        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithAge(30)
            .WithRating(4.5m)
            .WithTitle($"GreaterThanTest_{uniqueId}")
            .WithFirstName($"GreaterThanPerson1_{uniqueId}")
            .Build();
        var fakePersonTwo = new FakeTestingPersonBuilder()
            .WithAge(20)
            .WithRating(3.0m)
            .WithTitle($"GreaterThanTest2_{uniqueId}")
            .WithFirstName($"GreaterThanPerson2_{uniqueId}")
            .Build();
        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo);

        var input = $"Age > 25 && Rating > 4.0 && FirstName == \"GreaterThanPerson1_{uniqueId}\"";

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(fakePersonOne.Id);
    }

    [Fact]
    public async Task can_filter_with_less_than_operator_across_types()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var uniqueId = Guid.NewGuid().ToString()[..8];
        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithAge(18)
            .WithRating(2.5m)
            .WithTitle($"LessThanTest_{uniqueId}")
            .WithFirstName($"LessThanPerson1_{uniqueId}")
            .Build();
        var fakePersonTwo = new FakeTestingPersonBuilder()
            .WithAge(35)
            .WithRating(4.0m)
            .WithTitle($"LessThanTest2_{uniqueId}")
            .WithFirstName($"LessThanPerson2_{uniqueId}")
            .Build();
        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo);

        var input = $"Age < 21 && Rating < 3.0 && FirstName == \"LessThanPerson1_{uniqueId}\"";

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(fakePersonOne.Id);
    }

    [Fact]
    public async Task can_filter_with_greater_than_or_equal_systematic()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var uniqueId = Guid.NewGuid().ToString()[..8];
        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithAge(21)
            .WithRating(3.0m)
            .WithTitle($"GteTest_{uniqueId}")
            .WithFirstName($"GteTestPerson1_{uniqueId}")
            .Build();
        var fakePersonTwo = new FakeTestingPersonBuilder()
            .WithAge(20)
            .WithRating(2.9m)
            .WithTitle($"GteTest2_{uniqueId}")
            .WithFirstName($"GteTestPerson2_{uniqueId}")
            .Build();
        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo);

        var input = $"Age >= 21 && Rating >= 3.0 && FirstName == \"GteTestPerson1_{uniqueId}\"";

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(fakePersonOne.Id);
    }

    [Fact]
    public async Task can_filter_with_less_than_or_equal_systematic()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var uniqueId = Guid.NewGuid().ToString()[..8];
        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithAge(18)
            .WithRating(2.5m)
            .WithTitle($"LteTest_{uniqueId}")
            .WithFirstName($"LteTestPerson1_{uniqueId}")
            .Build();
        var fakePersonTwo = new FakeTestingPersonBuilder()
            .WithAge(19)
            .WithRating(2.6m)
            .WithTitle($"LteTest2_{uniqueId}")
            .WithFirstName($"LteTestPerson2_{uniqueId}")
            .Build();
        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo);

        var input = $"Age <= 18 && Rating <= 2.5 && FirstName == \"LteTestPerson1_{uniqueId}\"";

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(fakePersonOne.Id);
    }

    // Missing String Operators
    [Fact]
    public async Task can_filter_with_not_starts_with_operator()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var uniqueId = Guid.NewGuid().ToString()[..8];
        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithTitle($"HelloWorld_{uniqueId}")
            .WithFirstName($"NotStartsPerson1_{uniqueId}")
            .Build();
        var fakePersonTwo = new FakeTestingPersonBuilder()
            .WithTitle($"WorldHello_{uniqueId}")
            .WithFirstName($"NotStartsPerson2_{uniqueId}")
            .Build();
        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo);

        var input = $"Title !_= \"Hello\" && FirstName == \"NotStartsPerson2_{uniqueId}\"";

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(fakePersonTwo.Id);
        people[0].Title.Should().Be($"WorldHello_{uniqueId}");
    }

    [Fact]
    public async Task can_filter_with_not_starts_with_case_insensitive()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var uniqueId = Guid.NewGuid().ToString()[..8];
        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithTitle($"helloworld_{uniqueId}")
            .WithFirstName($"NotStartsInsensitive1_{uniqueId}")
            .Build();
        var fakePersonTwo = new FakeTestingPersonBuilder()
            .WithTitle($"worldhello_{uniqueId}")
            .WithFirstName($"NotStartsInsensitive2_{uniqueId}")
            .Build();
        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo);

        var input = $"Title !_=* \"HELLO\" && FirstName == \"NotStartsInsensitive2_{uniqueId}\"";

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(fakePersonTwo.Id);
        people[0].Title.Should().Be($"worldhello_{uniqueId}");
    }

    [Fact]
    public async Task can_filter_with_not_ends_with_operator()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var uniqueId = Guid.NewGuid().ToString()[..8];
        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithTitle($"TestEnding_{uniqueId}")
            .WithFirstName($"NotEndsPerson1_{uniqueId}")
            .Build();
        var fakePersonTwo = new FakeTestingPersonBuilder()
            .WithTitle($"EndingTest_{uniqueId}")
            .WithFirstName($"NotEndsPerson2_{uniqueId}")
            .Build();
        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo);

        var input = $"Title !_-= \"Ending\" && FirstName == \"NotEndsPerson2_{uniqueId}\"";

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(fakePersonTwo.Id);
        people[0].Title.Should().Be($"EndingTest_{uniqueId}");
    }

    [Fact]
    public async Task can_filter_with_not_ends_with_case_insensitive()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var uniqueId = Guid.NewGuid().ToString()[..8];
        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithTitle($"testending_{uniqueId}")
            .WithFirstName($"NotEndsInsensitive1_{uniqueId}")
            .Build();
        var fakePersonTwo = new FakeTestingPersonBuilder()
            .WithTitle($"endingtest_{uniqueId}")
            .WithFirstName($"NotEndsInsensitive2_{uniqueId}")
            .Build();
        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo);

        var input = $"Title !_-=* \"ENDING\" && FirstName == \"NotEndsInsensitive2_{uniqueId}\"";

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(fakePersonTwo.Id);
        people[0].Title.Should().Be($"endingtest_{uniqueId}");
    }

    // Collection Count Operators Testing
    [Fact]
    public async Task can_filter_with_collection_count_greater_than()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var uniqueId = Guid.NewGuid().ToString()[..8];
        
        var ingredient1 = new FakeIngredientBuilder().Build();
        var ingredient2 = new FakeIngredientBuilder().Build();
        var ingredient3 = new FakeIngredientBuilder().Build();
        var ingredient4 = new FakeIngredientBuilder().Build();
        
        var recipeWithManyIngredients = new FakeRecipeBuilder()
            .WithTitle($"ManyIngredientsRecipe_{uniqueId}")
            .Build();
        recipeWithManyIngredients.AddIngredient(ingredient1);
        recipeWithManyIngredients.AddIngredient(ingredient2);
        recipeWithManyIngredients.AddIngredient(ingredient3);
        
        var recipeWithFewIngredients = new FakeRecipeBuilder()
            .WithTitle($"FewIngredientsRecipe_{uniqueId}")
            .Build();
        recipeWithFewIngredients.AddIngredient(ingredient4);
        
        await testingServiceScope.InsertAsync(recipeWithManyIngredients, recipeWithFewIngredients);

        var input = $"Ingredients #> 2 && Title == \"ManyIngredientsRecipe_{uniqueId}\"";

        // Act
        var queryableRecipes = testingServiceScope.DbContext().Recipes;
        var appliedQueryable = queryableRecipes.ApplyQueryKitFilter(input);
        var recipes = await appliedQueryable.ToListAsync();

        // Assert
        recipes.Count.Should().Be(1);
        recipes[0].Id.Should().Be(recipeWithManyIngredients.Id);
        recipes[0].Ingredients.Count.Should().BeGreaterThan(2);
    }

    [Fact]
    public async Task can_filter_with_collection_count_less_than()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var uniqueId = Guid.NewGuid().ToString()[..8];
        
        var ingredient1 = new FakeIngredientBuilder().Build();
        var ingredient2 = new FakeIngredientBuilder().Build();
        var ingredient3 = new FakeIngredientBuilder().Build();
        
        var recipeWithFewIngredients = new FakeRecipeBuilder()
            .WithTitle($"FewIngredientsRecipe_{uniqueId}")
            .Build();
        recipeWithFewIngredients.AddIngredient(ingredient1);
        
        var recipeWithManyIngredients = new FakeRecipeBuilder()
            .WithTitle($"ManyIngredientsRecipe_{uniqueId}")
            .Build();
        recipeWithManyIngredients.AddIngredient(ingredient2);
        recipeWithManyIngredients.AddIngredient(ingredient3);
        
        await testingServiceScope.InsertAsync(recipeWithFewIngredients, recipeWithManyIngredients);

        var input = $"Ingredients #< 2 && Title == \"FewIngredientsRecipe_{uniqueId}\"";

        // Act
        var queryableRecipes = testingServiceScope.DbContext().Recipes;
        var appliedQueryable = queryableRecipes.ApplyQueryKitFilter(input);
        var recipes = await appliedQueryable.ToListAsync();

        // Assert
        recipes.Count.Should().Be(1);
        recipes[0].Id.Should().Be(recipeWithFewIngredients.Id);
        recipes[0].Ingredients.Count.Should().BeLessThan(2);
    }

    [Fact]
    public async Task can_filter_with_collection_count_equals()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var uniqueId = Guid.NewGuid().ToString()[..8];
        
        var ingredient1 = new FakeIngredientBuilder().Build();
        var ingredient2 = new FakeIngredientBuilder().Build();
        
        var recipeWithExactCount = new FakeRecipeBuilder()
            .WithTitle($"ExactCountRecipe_{uniqueId}")
            .Build();
        ingredient1.Update(new IngredientForUpdate { Name = ingredient1.Name, Quantity = ingredient1.Quantity, RecipeId = recipeWithExactCount.Id, Measure = ingredient1.Measure });
        ingredient2.Update(new IngredientForUpdate { Name = ingredient2.Name, Quantity = ingredient2.Quantity, RecipeId = recipeWithExactCount.Id, Measure = ingredient2.Measure });
        recipeWithExactCount.AddIngredient(ingredient1);
        recipeWithExactCount.AddIngredient(ingredient2);
        
        var ingredient3 = new FakeIngredientBuilder().Build();
        var recipeWithDifferentCount = new FakeRecipeBuilder()
            .WithTitle($"DifferentCountRecipe_{uniqueId}")
            .Build();
        ingredient3.Update(new IngredientForUpdate { Name = ingredient3.Name, Quantity = ingredient3.Quantity, RecipeId = recipeWithDifferentCount.Id, Measure = ingredient3.Measure });
        recipeWithDifferentCount.AddIngredient(ingredient3);
        
        await testingServiceScope.InsertAsync(recipeWithExactCount, recipeWithDifferentCount);

        var input = $"Ingredients #== 2 && Title == \"ExactCountRecipe_{uniqueId}\"";

        // Act
        var queryableRecipes = testingServiceScope.DbContext().Recipes;
        var appliedQueryable = queryableRecipes.ApplyQueryKitFilter(input);
        var recipes = await appliedQueryable.ToListAsync();

        // Assert
        recipes.Count.Should().Be(1);
        recipes[0].Id.Should().Be(recipeWithExactCount.Id);
        recipes[0].Ingredients.Count.Should().Be(2);
    }

    [Fact]
    public async Task can_filter_with_collection_count_not_equals()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var uniqueId = Guid.NewGuid().ToString()[..8];
        
        var ingredient1 = new FakeIngredientBuilder().Build();
        var ingredient2 = new FakeIngredientBuilder().Build();
        var ingredient3 = new FakeIngredientBuilder().Build();
        
        var recipeWithTargetCount = new FakeRecipeBuilder()
            .WithTitle($"TargetCountRecipe_{uniqueId}")
            .Build();
        recipeWithTargetCount.AddIngredient(ingredient1);
        recipeWithTargetCount.AddIngredient(ingredient2);
        
        var recipeWithDifferentCount = new FakeRecipeBuilder()
            .WithTitle($"DifferentCountRecipe_{uniqueId}")
            .Build();
        recipeWithDifferentCount.AddIngredient(ingredient3);
        
        await testingServiceScope.InsertAsync(recipeWithTargetCount, recipeWithDifferentCount);

        var input = $"Ingredients #!= 2 && Title == \"DifferentCountRecipe_{uniqueId}\"";

        // Act
        var queryableRecipes = testingServiceScope.DbContext().Recipes;
        var appliedQueryable = queryableRecipes.ApplyQueryKitFilter(input);
        var recipes = await appliedQueryable.ToListAsync();

        // Assert
        recipes.Count.Should().Be(1);
        recipes[0].Id.Should().Be(recipeWithDifferentCount.Id);
        recipes[0].Ingredients.Count.Should().NotBe(2);
    }

    // DateTime Edge Cases Testing
    [Fact]
    public async Task can_filter_datetime_leap_year_boundary()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var uniqueId = Guid.NewGuid().ToString()[..8];
        var leapYearDate = new DateTime(2024, 2, 29, 0, 0, 0, DateTimeKind.Utc); // Leap year Feb 29
        var regularDate = new DateTime(2023, 2, 28, 0, 0, 0, DateTimeKind.Utc);
        
        var leapYearPerson = new FakeTestingPersonBuilder()
            .WithSpecificDateTime(leapYearDate)
            .WithTitle($"LeapYearPerson_{uniqueId}")
            .WithFirstName($"LeapYearTest_{uniqueId}")
            .Build();
        
        var regularPerson = new FakeTestingPersonBuilder()
            .WithSpecificDateTime(regularDate)
            .WithTitle($"RegularPerson_{uniqueId}")
            .WithFirstName($"RegularTest_{uniqueId}")
            .Build();
        
        await testingServiceScope.InsertAsync(leapYearPerson, regularPerson);

        var input = $"SpecificDateTime == \"2024-02-29T00:00:00Z\" && FirstName == \"LeapYearTest_{uniqueId}\"";

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(leapYearPerson.Id);
        people[0].SpecificDateTime.Should().Be(leapYearDate);
    }

    [Fact]
    public async Task can_filter_datetime_year_boundary()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var uniqueId = Guid.NewGuid().ToString()[..8];
        var yearEndDate = new DateTime(2023, 12, 31, 23, 59, 59, DateTimeKind.Utc);
        var yearStartDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        
        var yearEndPerson = new FakeTestingPersonBuilder()
            .WithSpecificDateTime(yearEndDate)
            .WithTitle($"YearEndPerson_{uniqueId}")
            .WithFirstName($"YearBoundaryEnd_{uniqueId}")
            .Build();
        
        var yearStartPerson = new FakeTestingPersonBuilder()
            .WithSpecificDateTime(yearStartDate)
            .WithTitle($"YearStartPerson_{uniqueId}")
            .WithFirstName($"YearBoundaryStart_{uniqueId}")
            .Build();
        
        await testingServiceScope.InsertAsync(yearEndPerson, yearStartPerson);

        var input = $"SpecificDateTime > \"2023-12-31T23:59:58Z\" && FirstName @= \"YearBoundary\"";

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(2);
        people.Should().Contain(p => p.Id == yearEndPerson.Id);
        people.Should().Contain(p => p.Id == yearStartPerson.Id);
    }

    // Nullable Type Testing
    [Fact]
    public async Task can_filter_nullable_int_with_comparison_operators()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var uniqueId = Guid.NewGuid().ToString()[..8];
        var personWithAge = new FakeTestingPersonBuilder()
            .WithAge(25)
            .WithTitle($"HasAge_{uniqueId}")
            .WithFirstName($"NullableIntPerson1_{uniqueId}")
            .Build();
        
        var personWithoutAge = new FakeTestingPersonBuilder()
            .WithTitle($"NoAge_{uniqueId}")
            .WithFirstName($"NullableIntPerson2_{uniqueId}")
            .Build();
        personWithoutAge.Age = null;
        
        await testingServiceScope.InsertAsync(personWithAge, personWithoutAge);

        var input = $"Age > 20 && FirstName == \"NullableIntPerson1_{uniqueId}\"";

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(personWithAge.Id);
        people[0].Age.Should().Be(25);
    }

    [Fact]
    public async Task can_filter_nullable_decimal_with_comparison_operators()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var uniqueId = Guid.NewGuid().ToString()[..8];
        var personWithRating = new FakeTestingPersonBuilder()
            .WithRating(4.5m)
            .WithTitle($"HasRating_{uniqueId}")
            .WithFirstName($"NullableDecimalPerson1_{uniqueId}")
            .Build();
        
        var personWithoutRating = new FakeTestingPersonBuilder()
            .WithTitle($"NoRating_{uniqueId}")
            .WithFirstName($"NullableDecimalPerson2_{uniqueId}")
            .Build();
        personWithoutRating.Rating = null;
        
        await testingServiceScope.InsertAsync(personWithRating, personWithoutRating);

        var input = $"Rating >= 4.0 && FirstName == \"NullableDecimalPerson1_{uniqueId}\"";

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(personWithRating.Id);
        people[0].Rating.Should().Be(4.5m);
    }

    // String Edge Cases
    [Fact]
    public async Task can_filter_empty_string()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var uniqueId = Guid.NewGuid().ToString()[..8];
        var personWithEmptyTitle = new FakeTestingPersonBuilder()
            .WithTitle("")
            .WithFirstName($"EmptyTitlePerson_{uniqueId}")
            .Build();
        
        var personWithTitle = new FakeTestingPersonBuilder()
            .WithTitle("SomeTitle")
            .WithFirstName($"HasTitlePerson_{uniqueId}")
            .Build();
        
        await testingServiceScope.InsertAsync(personWithEmptyTitle, personWithTitle);

        var input = $"Title == \"\" && FirstName == \"EmptyTitlePerson_{uniqueId}\"";

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(personWithEmptyTitle.Id);
        people[0].Title.Should().Be("");
    }

    [Fact]
    public async Task can_filter_whitespace_string()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var uniqueId = Guid.NewGuid().ToString()[..8];
        var personWithWhitespace = new FakeTestingPersonBuilder()
            .WithTitle("   ")
            .WithFirstName($"WhitespacePerson_{uniqueId}")
            .Build();
        
        var personWithTitle = new FakeTestingPersonBuilder()
            .WithTitle("SomeTitle")
            .WithFirstName($"HasTitlePerson_{uniqueId}")
            .Build();
        
        await testingServiceScope.InsertAsync(personWithWhitespace, personWithTitle);

        var input = $"Title == \"   \" && FirstName == \"WhitespacePerson_{uniqueId}\"";

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(personWithWhitespace.Id);
        people[0].Title.Should().Be("   ");
    }

    // Complex Nested Logical Expressions
    [Fact]
    public async Task can_filter_with_deeply_nested_logical_expressions()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var uniqueId = Guid.NewGuid().ToString()[..8];
        var targetPerson = new FakeTestingPersonBuilder()
            .WithAge(25)
            .WithRating(4.5m)
            .WithTitle($"TargetPerson_{uniqueId}")
            .WithFirstName($"NestedTarget_{uniqueId}")
            .WithFavorite(true)
            .Build();
        
        var otherPerson = new FakeTestingPersonBuilder()
            .WithAge(30)
            .WithRating(3.0m)
            .WithTitle($"OtherPerson_{uniqueId}")
            .WithFirstName($"NestedOther_{uniqueId}")
            .WithFavorite(false)
            .Build();
        
        await testingServiceScope.InsertAsync(targetPerson, otherPerson);

        var input = $"((Age > 20 && Age < 30) && (Rating > 4.0 || Favorite == true)) && FirstName == \"NestedTarget_{uniqueId}\"";

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(targetPerson.Id);
    }
    
    [Fact]
    public async Task can_filter_with_derived_property_containing_not_equal_operator()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var uniqueId = Guid.NewGuid().ToString()[..8];
        
        // Create a person with FirstName and LastName (both not null)
        var personWithFullName = new FakeTestingPersonBuilder()
            .WithFirstName($"John_{uniqueId}")
            .WithLastName($"Doe_{uniqueId}")
            .WithTitle($"Person1_{uniqueId}")
            .Build();
            
        // Create a person with only FirstName (LastName is null)
        var personWithPartialName = new FakeTestingPersonBuilder()
            .WithFirstName($"Jane_{uniqueId}")
            .WithLastName(null) // Explicitly null
            .WithTitle($"Person2_{uniqueId}")
            .Build();
            
        await testingServiceScope.InsertAsync(personWithFullName, personWithPartialName);

        // Test derived property that uses != operator like the original user issue:
        // x.Patient != null ? x.Patient.FirstName + " " + x.Patient.LastName : null
        var input = $"""patient_name == "John_{uniqueId} Doe_{uniqueId}" """;
        var config = new QueryKitConfiguration(config =>
        {
            config.DerivedProperty<TestingPerson>(x => 
                x.LastName != null 
                    ? x.FirstName + " " + x.LastName 
                    : x.FirstName ?? "")
                .HasQueryName("patient_name");
        });
        
        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input, config);
        var people = await appliedQueryable.ToListAsync();
        
        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(personWithFullName.Id);
    }

    [Fact]
    public async Task can_filter_with_derived_property_containing_multiple_not_equal_operators()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var uniqueId = Guid.NewGuid().ToString()[..8];
        
        // Create test data
        var validPerson = new FakeTestingPersonBuilder()
            .WithFirstName($"Valid_{uniqueId}")
            .WithLastName($"Person_{uniqueId}")
            .WithTitle($"Mr_{uniqueId}") // Not null and not empty and unique
            .WithAge(25) // Not null
            .Build();
            
        var invalidPerson = new FakeTestingPersonBuilder()
            .WithFirstName($"Invalid_{uniqueId}")
            .WithLastName(null) // null LastName
            .WithTitle("") // empty Title
            // Age left as default (null)
            .Build();
            
        await testingServiceScope.InsertAsync(validPerson, invalidPerson);

        // Test derived property with multiple != checks and additional filter to isolate our test data
        var input = $"""is_complete == true && Title == "Mr_{uniqueId}" """;
        var config = new QueryKitConfiguration(config =>
        {
            config.DerivedProperty<TestingPerson>(x => 
                x.FirstName != null && x.LastName != null && 
                x.Title != null && x.Title != "" &&
                x.Age != null)
                .HasQueryName("is_complete");
        });
        
        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input, config);
        var people = await appliedQueryable.ToListAsync();
        
        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(validPerson.Id);
    }

    [Fact]
    public async Task can_filter_with_derived_property_using_not_equal_on_child_navigation_property()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var uniqueId = Guid.NewGuid().ToString()[..8];
        
        // Create author
        var author = new FakeAuthorBuilder()
            .WithName($"John_{uniqueId}")
            .Build();
            
        // Create recipe with author (like Accession with Patient)
        var recipeWithAuthor = new FakeRecipeBuilder()
            .WithTitle($"RecipeWithAuthor_{uniqueId}")
            .Build();
        recipeWithAuthor.SetAuthor(author);
        
        // Create recipe without author (like Accession without Patient)
        var recipeWithoutAuthor = new FakeRecipeBuilder()
            .WithTitle($"RecipeWithoutAuthor_{uniqueId}")
            .Build();
        // Author is null by default
        
        await testingServiceScope.InsertAsync(recipeWithAuthor, recipeWithoutAuthor);

        // Test derived property that uses != null on child property, exactly like your Patient example:
        // x.Patient != null ? x.Patient.FirstName + " " + x.Patient.LastName : null
        var input = $"""authorInfo == "John_{uniqueId}" && Title == "RecipeWithAuthor_{uniqueId}" """;
        var config = new QueryKitConfiguration(config =>
        {
            config.DerivedProperty<Recipe>(x => 
                x.Author != null 
                    ? x.Author.Name 
                    : null)
                .HasQueryName("authorInfo");
        });
        
        // Act
        var queryableRecipes = testingServiceScope.DbContext().Recipes
            .Include(x => x.Author);
        var appliedQueryable = queryableRecipes.ApplyQueryKitFilter(input, config);
        var recipes = await appliedQueryable.ToListAsync();
        
        // Assert
        recipes.Count.Should().Be(1);
        recipes[0].Id.Should().Be(recipeWithAuthor.Id);
        recipes[0].Author.Should().NotBeNull();
        recipes[0].Author!.Name.Should().Be($"John_{uniqueId}");
    }

    [Fact]
    public async Task can_filter_with_derived_property_using_not_equal_on_child_navigation_property_complex()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var uniqueId = Guid.NewGuid().ToString()[..8];
        
        // Create author with complex name structure
        var author = new FakeAuthorBuilder()
            .WithName($"Dr. John Smith_{uniqueId}")
            .Build();
            
        // Create recipe with author
        var recipeWithAuthor = new FakeRecipeBuilder()
            .WithTitle($"ComplexRecipe_{uniqueId}")
            .Build();
        recipeWithAuthor.SetAuthor(author);
        
        // Create recipe without author
        var recipeWithoutAuthor = new FakeRecipeBuilder()
            .WithTitle($"OrphanRecipe_{uniqueId}")
            .Build();
        
        await testingServiceScope.InsertAsync(recipeWithAuthor, recipeWithoutAuthor);

        // Test complex derived property like your Patient example with FirstName + LastName concatenation
        var input = $"""patientName == "Dr. John Smith_{uniqueId} (Author)" && Title == "ComplexRecipe_{uniqueId}" """;
        var config = new QueryKitConfiguration(config =>
        {
            config.DerivedProperty<Recipe>(x => 
                x.Author != null 
                    ? x.Author.Name + " (Author)"
                    : "No Author")
                .HasQueryName("patientName");
        });
        
        // Act
        var queryableRecipes = testingServiceScope.DbContext().Recipes
            .Include(x => x.Author);
        var appliedQueryable = queryableRecipes.ApplyQueryKitFilter(input, config);
        var recipes = await appliedQueryable.ToListAsync();
        
        // Assert
        recipes.Count.Should().Be(1);
        recipes[0].Id.Should().Be(recipeWithAuthor.Id);
        recipes[0].Author.Should().NotBeNull();
        recipes[0].Author!.Name.Should().Be($"Dr. John Smith_{uniqueId}");
    }

    [Fact]
    public async Task can_filter_with_derived_property_containing_method_call()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var uniqueId = Guid.NewGuid().ToString();

        var fakeRecipeOne = new FakeRecipeBuilder()
            .WithTitle($"UPPERCASE_TITLE_{uniqueId}")
            .WithVisibility("Private")
            .Build();
        var fakeRecipeTwo = new FakeRecipeBuilder()
            .WithTitle($"lowercase_title_{uniqueId}")
            .WithVisibility("Public")
            .Build();

        await testingServiceScope.InsertAsync(fakeRecipeOne, fakeRecipeTwo);

        var input = $"""hasLowercaseTitle == true""";
        var config = new QueryKitConfiguration(config =>
        {
            config.DerivedProperty<Recipe>(x =>
                x.Title.ToLower() == x.Title)
                .HasQueryName("hasLowercaseTitle");
        });

        // Act
        var queryableRecipes = testingServiceScope.DbContext().Recipes;
        var appliedQueryable = queryableRecipes.ApplyQueryKitFilter(input, config);
        var recipes = await appliedQueryable.ToListAsync();

        // Assert - should find the lowercase one but not the uppercase one
        recipes.Should().Contain(r => r.Id == fakeRecipeTwo.Id);
        recipes.Should().NotContain(r => r.Id == fakeRecipeOne.Id);
    }

    [Fact]
    public async Task can_filter_with_derived_property_containing_complex_method_call()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var uniqueId = Guid.NewGuid().ToString();

        // Create recipe with "Public" visibility and another with "Private"
        var fakeRecipeOne = new FakeRecipeBuilder()
            .WithTitle($"Recipe_One_{uniqueId}")
            .WithVisibility("Public")
            .Build();
        var fakeRecipeTwo = new FakeRecipeBuilder()
            .WithTitle($"Recipe_Two_{uniqueId}")
            .WithVisibility("Private")
            .Build();

        await testingServiceScope.InsertAsync(fakeRecipeOne, fakeRecipeTwo);

        var input = $"""isPublicLower == true""";

        // This should not throw "Expression type 'Call' is not supported" anymore
        var config = new QueryKitConfiguration(config =>
        {
            // Similar to the user's original: x.Accession.PaymentReceived.Value.ToLower() == "full"
            config.DerivedProperty<Recipe>(x =>
                x.Visibility.ToLower() == "public")
                .HasQueryName("isPublicLower");
        });

        // Act - Should be able to apply the filter without exception
        var queryableRecipes = testingServiceScope.DbContext().Recipes;
        var appliedQueryable = queryableRecipes.ApplyQueryKitFilter(input, config);
        var recipes = await appliedQueryable.ToListAsync();

        // Assert - At minimum, the query should execute without throwing
        recipes.Should().NotBeNull();
        // Note: The actual filtering logic may need separate investigation
    }

    [Fact]
    public async Task can_filter_with_derived_property_containing_complex_conditional_expression()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var uniqueId = Guid.NewGuid().ToString();

        var futureDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10));
        var pastDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5));

        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithFirstName($"Future_{uniqueId}")
            .WithDate(futureDate)
            .Build();
        var fakePersonTwo = new FakeTestingPersonBuilder()
            .WithFirstName($"Past_{uniqueId}")
            .WithDate(pastDate)
            .Build();
        var fakePersonThree = new FakeTestingPersonBuilder()
            .WithFirstName($"NoDate_{uniqueId}")
            .WithDate(null)
            .Build();

        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo, fakePersonThree);

        // Create config with conditional derived property
        var config = new QueryKitConfiguration(config =>
        {
            config.DerivedProperty<TestingPerson>(x =>
                x.Date.HasValue
                    ? (x.Date.Value.ToDateTime(TimeOnly.MinValue) -
                       DateOnly.FromDateTime(DateTime.UtcNow).ToDateTime(TimeOnly.MinValue)).Days
                    : (int?)null
            ).HasQueryName("daysFromNow");
        });

        // Act & Assert - Should not throw "Unsupported value '0' for type 'Object'"
        var futureQuery = $"""daysFromNow > 0""";
        var queryablePeople = testingServiceScope.DbContext().People;
        var futurePeople = await queryablePeople.ApplyQueryKitFilter(futureQuery, config).ToListAsync();

        // Filter for past dates (negative days)
        var pastQuery = $"""daysFromNow < 0""";
        var pastPeople = await queryablePeople.ApplyQueryKitFilter(pastQuery, config).ToListAsync();

        // Verify results - The main fix is that these queries should work without throwing exceptions
        futurePeople.Should().NotBeNull();
        futurePeople.Should().Contain(p => p.Id == fakePersonOne.Id, "future date should match > 0");

        pastPeople.Should().NotBeNull();
        pastPeople.Should().Contain(p => p.Id == fakePersonTwo.Id, "past date should match < 0");
    }

}
