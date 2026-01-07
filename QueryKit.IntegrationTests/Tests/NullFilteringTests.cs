namespace QueryKit.IntegrationTests.Tests;

using Bogus;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SharedTestingHelper.Fakes;
using SharedTestingHelper.Fakes.Recipes;
using WebApiTestProject.Entities;
using WebApiTestProject.Entities.Recipes;
using Xunit;

public class NullFilteringTests : TestBase
{
    [Fact]
    public async Task can_filter_nullable_string_equals_null()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var uniqueFirstName = $"NullStringTest_{Guid.NewGuid()}";
        var personWithNullTitle = new FakeTestingPersonBuilder()
            .WithTitle(null)
            .WithFirstName(uniqueFirstName)
            .Build();
        var personWithTitle = new FakeTestingPersonBuilder()
            .WithTitle("Mr.")
            .WithFirstName($"OtherName_{Guid.NewGuid()}")
            .Build();
        await testingServiceScope.InsertAsync(personWithNullTitle, personWithTitle);

        var input = $"""Title == null && FirstName == "{uniqueFirstName}" """;

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(personWithNullTitle.Id);
        people[0].Title.Should().BeNull();
    }

    [Fact]
    public async Task can_filter_nullable_string_not_equals_null()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var uniqueFirstName = $"NotNullStringTest_{Guid.NewGuid()}";
        var personWithNullTitle = new FakeTestingPersonBuilder()
            .WithTitle(null)
            .WithFirstName($"OtherName_{Guid.NewGuid()}")
            .Build();
        var personWithTitle = new FakeTestingPersonBuilder()
            .WithTitle("Mr.")
            .WithFirstName(uniqueFirstName)
            .Build();
        await testingServiceScope.InsertAsync(personWithNullTitle, personWithTitle);

        var input = $"""Title != null && FirstName == "{uniqueFirstName}" """;

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(personWithTitle.Id);
        people[0].Title.Should().NotBeNull();
    }

    [Fact]
    public async Task can_filter_nullable_int_equals_null()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var uniqueFirstName = $"NullIntTest_{Guid.NewGuid()}";
        var personWithNullAge = new FakeTestingPersonBuilder()
            .WithAge(null)
            .WithFirstName(uniqueFirstName)
            .Build();
        var personWithAge = new FakeTestingPersonBuilder()
            .WithAge(25)
            .WithFirstName($"OtherName_{Guid.NewGuid()}")
            .Build();
        await testingServiceScope.InsertAsync(personWithNullAge, personWithAge);

        var input = $"""Age == null && FirstName == "{uniqueFirstName}" """;

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(personWithNullAge.Id);
        people[0].Age.Should().BeNull();
    }

    [Fact]
    public async Task can_filter_nullable_int_not_equals_null()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var uniqueFirstName = $"NotNullIntTest_{Guid.NewGuid()}";
        var personWithNullAge = new FakeTestingPersonBuilder()
            .WithAge(null)
            .WithFirstName($"OtherName_{Guid.NewGuid()}")
            .Build();
        var personWithAge = new FakeTestingPersonBuilder()
            .WithAge(25)
            .WithFirstName(uniqueFirstName)
            .Build();
        await testingServiceScope.InsertAsync(personWithNullAge, personWithAge);

        var input = $"""Age != null && FirstName == "{uniqueFirstName}" """;

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(personWithAge.Id);
        people[0].Age.Should().NotBeNull();
        people[0].Age.Should().Be(25);
    }

    [Fact]
    public async Task can_filter_nullable_decimal_equals_null()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var uniqueFirstName = $"NullDecimalTest_{Guid.NewGuid()}";
        var personWithNullRating = new FakeTestingPersonBuilder()
            .WithRating(null)
            .WithFirstName(uniqueFirstName)
            .Build();
        var personWithRating = new FakeTestingPersonBuilder()
            .WithRating(4.5M)
            .WithFirstName($"OtherName_{Guid.NewGuid()}")
            .Build();
        await testingServiceScope.InsertAsync(personWithNullRating, personWithRating);

        var input = $"""Rating == null && FirstName == "{uniqueFirstName}" """;

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(personWithNullRating.Id);
        people[0].Rating.Should().BeNull();
    }

    [Fact]
    public async Task can_filter_nullable_decimal_not_equals_null()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var uniqueFirstName = $"NotNullDecimalTest_{Guid.NewGuid()}";
        var personWithNullRating = new FakeTestingPersonBuilder()
            .WithRating(null)
            .WithFirstName($"OtherName_{Guid.NewGuid()}")
            .Build();
        var personWithRating = new FakeTestingPersonBuilder()
            .WithRating(4.5M)
            .WithFirstName(uniqueFirstName)
            .Build();
        await testingServiceScope.InsertAsync(personWithNullRating, personWithRating);

        var input = $"""Rating != null && FirstName == "{uniqueFirstName}" """;

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(personWithRating.Id);
        people[0].Rating.Should().NotBeNull();
        people[0].Rating.Should().Be(4.5M);
    }

    [Fact]
    public async Task can_filter_nullable_guid_equals_null()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var uniqueTitle = $"NullGuidTest_{Guid.NewGuid()}";
        var recipeWithNullGuid = new FakeRecipeBuilder()
            .WithSecondaryId(null)
            .WithTitle(uniqueTitle)
            .Build();
        var recipeWithGuid = new FakeRecipeBuilder()
            .WithSecondaryId(Guid.NewGuid())
            .WithTitle($"OtherTitle_{Guid.NewGuid()}")
            .Build();
        await testingServiceScope.InsertAsync(recipeWithNullGuid, recipeWithGuid);

        var input = $"""SecondaryId == null && Title == "{uniqueTitle}" """;

        // Act
        var queryableRecipes = testingServiceScope.DbContext().Recipes;
        var appliedQueryable = queryableRecipes.ApplyQueryKitFilter(input);
        var recipes = await appliedQueryable.ToListAsync();

        // Assert
        recipes.Count.Should().Be(1);
        recipes[0].Id.Should().Be(recipeWithNullGuid.Id);
        recipes[0].SecondaryId.Should().BeNull();
    }

    [Fact]
    public async Task can_filter_nullable_guid_not_equals_null()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var uniqueTitle = $"NotNullGuidTest_{Guid.NewGuid()}";
        var recipeWithNullGuid = new FakeRecipeBuilder()
            .WithSecondaryId(null)
            .WithTitle($"OtherTitle_{Guid.NewGuid()}")
            .Build();
        var recipeWithGuid = new FakeRecipeBuilder()
            .WithSecondaryId(Guid.NewGuid())
            .WithTitle(uniqueTitle)
            .Build();
        await testingServiceScope.InsertAsync(recipeWithNullGuid, recipeWithGuid);

        var input = $"""SecondaryId != null && Title == "{uniqueTitle}" """;

        // Act
        var queryableRecipes = testingServiceScope.DbContext().Recipes;
        var appliedQueryable = queryableRecipes.ApplyQueryKitFilter(input);
        var recipes = await appliedQueryable.ToListAsync();

        // Assert
        recipes.Count.Should().Be(1);
        recipes[0].Id.Should().Be(recipeWithGuid.Id);
        recipes[0].SecondaryId.Should().NotBeNull();
    }

    [Fact]
    public async Task can_filter_nullable_dateonly_equals_null()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var uniqueFirstName = $"NullDateTest_{Guid.NewGuid()}";
        var personWithNullDate = new FakeTestingPersonBuilder()
            .WithDate(null)
            .WithFirstName(uniqueFirstName)
            .Build();
        var personWithDate = new FakeTestingPersonBuilder()
            .WithDate(new DateOnly(2023, 5, 15))
            .WithFirstName($"OtherName_{Guid.NewGuid()}")
            .Build();
        await testingServiceScope.InsertAsync(personWithNullDate, personWithDate);

        var input = $"""Date == null && FirstName == "{uniqueFirstName}" """;

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(personWithNullDate.Id);
        people[0].Date.Should().BeNull();
    }

    [Fact]
    public async Task can_filter_nullable_dateonly_not_equals_null()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var uniqueFirstName = $"NotNullDateTest_{Guid.NewGuid()}";
        var personWithNullDate = new FakeTestingPersonBuilder()
            .WithDate(null)
            .WithFirstName($"OtherName_{Guid.NewGuid()}")
            .Build();
        var personWithDate = new FakeTestingPersonBuilder()
            .WithDate(new DateOnly(2023, 5, 15))
            .WithFirstName(uniqueFirstName)
            .Build();
        await testingServiceScope.InsertAsync(personWithNullDate, personWithDate);

        var input = $"""Date != null && FirstName == "{uniqueFirstName}" """;

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(personWithDate.Id);
        people[0].Date.Should().NotBeNull();
        people[0].Date.Should().Be(new DateOnly(2023, 5, 15));
    }

    [Fact]
    public async Task can_filter_nullable_datetimeoffset_equals_null()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var uniqueFirstName = $"NullDateTimeOffsetTest_{Guid.NewGuid()}";
        var personWithNullDate = new FakeTestingPersonBuilder()
            .WithSpecificDate(null)
            .WithFirstName(uniqueFirstName)
            .Build();
        var personWithDate = new FakeTestingPersonBuilder()
            .WithSpecificDate(new DateTimeOffset(2023, 5, 15, 10, 30, 0, TimeSpan.Zero))
            .WithFirstName($"OtherName_{Guid.NewGuid()}")
            .Build();
        await testingServiceScope.InsertAsync(personWithNullDate, personWithDate);

        var input = $"""SpecificDate == null && FirstName == "{uniqueFirstName}" """;

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(personWithNullDate.Id);
        people[0].SpecificDate.Should().BeNull();
    }

    [Fact]
    public async Task can_filter_nullable_datetimeoffset_not_equals_null()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var uniqueFirstName = $"NotNullDateTimeOffsetTest_{Guid.NewGuid()}";
        var personWithNullDate = new FakeTestingPersonBuilder()
            .WithSpecificDate(null)
            .WithFirstName($"OtherName_{Guid.NewGuid()}")
            .Build();
        var personWithDate = new FakeTestingPersonBuilder()
            .WithSpecificDate(new DateTimeOffset(2023, 5, 15, 10, 30, 0, TimeSpan.Zero))
            .WithFirstName(uniqueFirstName)
            .Build();
        await testingServiceScope.InsertAsync(personWithNullDate, personWithDate);

        var input = $"""SpecificDate != null && FirstName == "{uniqueFirstName}" """;

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(personWithDate.Id);
        people[0].SpecificDate.Should().NotBeNull();
    }

    [Fact]
    public async Task can_filter_nullable_timeonly_equals_null()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var uniqueFirstName = $"NullTimeTest_{Guid.NewGuid()}";
        var personWithNullTime = new FakeTestingPersonBuilder()
            .WithTime(null)
            .WithFirstName(uniqueFirstName)
            .Build();
        var personWithTime = new FakeTestingPersonBuilder()
            .WithTime(new TimeOnly(14, 30, 0))
            .WithFirstName($"OtherName_{Guid.NewGuid()}")
            .Build();
        await testingServiceScope.InsertAsync(personWithNullTime, personWithTime);

        var input = $"""Time == null && FirstName == "{uniqueFirstName}" """;

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(personWithNullTime.Id);
        people[0].Time.Should().BeNull();
    }

    [Fact]
    public async Task can_filter_nullable_timeonly_not_equals_null()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var uniqueFirstName = $"NotNullTimeTest_{Guid.NewGuid()}";
        var personWithNullTime = new FakeTestingPersonBuilder()
            .WithTime(null)
            .WithFirstName($"OtherName_{Guid.NewGuid()}")
            .Build();
        var personWithTime = new FakeTestingPersonBuilder()
            .WithTime(new TimeOnly(14, 30, 0))
            .WithFirstName(uniqueFirstName)
            .Build();
        await testingServiceScope.InsertAsync(personWithNullTime, personWithTime);

        var input = $"""Time != null && FirstName == "{uniqueFirstName}" """;

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(personWithTime.Id);
        people[0].Time.Should().NotBeNull();
        people[0].Time.Should().Be(new TimeOnly(14, 30, 0));
    }

    [Fact]
    public async Task can_filter_nullable_bool_equals_null()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var uniqueFirstName = $"NullBoolTest_{Guid.NewGuid()}";
        var personWithNullFavorite = new FakeTestingPersonBuilder()
            .WithFavorite(null)
            .WithFirstName(uniqueFirstName)
            .Build();
        var personWithFavorite = new FakeTestingPersonBuilder()
            .WithFavorite(true)
            .WithFirstName($"OtherName_{Guid.NewGuid()}")
            .Build();
        await testingServiceScope.InsertAsync(personWithNullFavorite, personWithFavorite);

        var input = $"""Favorite == null && FirstName == "{uniqueFirstName}" """;

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(personWithNullFavorite.Id);
        people[0].Favorite.Should().BeNull();
    }

    [Fact]
    public async Task can_filter_nullable_bool_not_equals_null()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var uniqueFirstName = $"NotNullBoolTest_{Guid.NewGuid()}";
        var personWithNullFavorite = new FakeTestingPersonBuilder()
            .WithFavorite(null)
            .WithFirstName($"OtherName_{Guid.NewGuid()}")
            .Build();
        var personWithFavorite = new FakeTestingPersonBuilder()
            .WithFavorite(true)
            .WithFirstName(uniqueFirstName)
            .Build();
        await testingServiceScope.InsertAsync(personWithNullFavorite, personWithFavorite);

        var input = $"""Favorite != null && FirstName == "{uniqueFirstName}" """;

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(personWithFavorite.Id);
        people[0].Favorite.Should().NotBeNull();
        people[0].Favorite.Should().Be(true);
    }

    [Fact]
    public async Task can_filter_nullable_enum_equals_null()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var uniqueFirstName = $"NullEnumTest_{Guid.NewGuid()}";
        var personWithNullMonth = new FakeTestingPersonBuilder()
            .WithBirthMonth(null)
            .WithFirstName(uniqueFirstName)
            .Build();
        var personWithMonth = new FakeTestingPersonBuilder()
            .WithBirthMonth(BirthMonthEnum.June)
            .WithFirstName($"OtherName_{Guid.NewGuid()}")
            .Build();
        await testingServiceScope.InsertAsync(personWithNullMonth, personWithMonth);

        var input = $"""BirthMonth == null && FirstName == "{uniqueFirstName}" """;

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(personWithNullMonth.Id);
        people[0].BirthMonth.Should().BeNull();
    }

    [Fact]
    public async Task can_filter_nullable_enum_not_equals_null()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var uniqueFirstName = $"NotNullEnumTest_{Guid.NewGuid()}";
        var personWithNullMonth = new FakeTestingPersonBuilder()
            .WithBirthMonth(null)
            .WithFirstName($"OtherName_{Guid.NewGuid()}")
            .Build();
        var personWithMonth = new FakeTestingPersonBuilder()
            .WithBirthMonth(BirthMonthEnum.June)
            .WithFirstName(uniqueFirstName)
            .Build();
        await testingServiceScope.InsertAsync(personWithNullMonth, personWithMonth);

        var input = $"""BirthMonth != null && FirstName == "{uniqueFirstName}" """;

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(personWithMonth.Id);
        people[0].BirthMonth.Should().NotBeNull();
        people[0].BirthMonth.Should().Be(BirthMonthEnum.June);
    }

    [Fact]
    public async Task can_filter_nullable_string_with_case_insensitive_contains()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var uniqueLastName = $"CaseInsensitiveContainsTest_{Guid.NewGuid()}";
        var personWithNullTitle = new FakeTestingPersonBuilder()
            .WithTitle(null)
            .WithLastName(uniqueLastName)
            .WithFirstName("NullTitle")
            .Build();
        var personWithMatchingTitle = new FakeTestingPersonBuilder()
            .WithTitle("Doctor Smith")
            .WithLastName(uniqueLastName)
            .WithFirstName("MatchingTitle")
            .Build();
        var personWithNonMatchingTitle = new FakeTestingPersonBuilder()
            .WithTitle("Mr. Jones")
            .WithLastName(uniqueLastName)
            .WithFirstName("NonMatchingTitle")
            .Build();
        await testingServiceScope.InsertAsync(personWithNullTitle, personWithMatchingTitle, personWithNonMatchingTitle);

        var input = $"""Title @=* "doctor" && LastName == "{uniqueLastName}" """;

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(personWithMatchingTitle.Id);
    }

    [Fact]
    public async Task can_filter_nullable_string_with_case_insensitive_starts_with()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var uniqueLastName = $"CaseInsensitiveStartsWithTest_{Guid.NewGuid()}";
        var personWithNullTitle = new FakeTestingPersonBuilder()
            .WithTitle(null)
            .WithLastName(uniqueLastName)
            .WithFirstName("NullTitle")
            .Build();
        var personWithMatchingTitle = new FakeTestingPersonBuilder()
            .WithTitle("Doctor Smith")
            .WithLastName(uniqueLastName)
            .WithFirstName("MatchingTitle")
            .Build();
        var personWithNonMatchingTitle = new FakeTestingPersonBuilder()
            .WithTitle("Mr. Jones")
            .WithLastName(uniqueLastName)
            .WithFirstName("NonMatchingTitle")
            .Build();
        await testingServiceScope.InsertAsync(personWithNullTitle, personWithMatchingTitle, personWithNonMatchingTitle);

        var input = $"""Title _=* "doctor" && LastName == "{uniqueLastName}" """;

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(personWithMatchingTitle.Id);
    }

    [Fact]
    public async Task can_filter_nullable_string_with_case_insensitive_ends_with()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var uniqueLastName = $"CaseInsensitiveEndsWithTest_{Guid.NewGuid()}";
        var personWithNullTitle = new FakeTestingPersonBuilder()
            .WithTitle(null)
            .WithLastName(uniqueLastName)
            .WithFirstName("NullTitle")
            .Build();
        var personWithMatchingTitle = new FakeTestingPersonBuilder()
            .WithTitle("Doctor Smith")
            .WithLastName(uniqueLastName)
            .WithFirstName("MatchingTitle")
            .Build();
        var personWithNonMatchingTitle = new FakeTestingPersonBuilder()
            .WithTitle("Mr. Jones")
            .WithLastName(uniqueLastName)
            .WithFirstName("NonMatchingTitle")
            .Build();
        await testingServiceScope.InsertAsync(personWithNullTitle, personWithMatchingTitle, personWithNonMatchingTitle);

        var input = $"""Title _-=* "smith" && LastName == "{uniqueLastName}" """;

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(personWithMatchingTitle.Id);
    }

    [Fact]
    public async Task can_filter_nullable_string_with_case_insensitive_equals()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var uniqueLastName = $"CaseInsensitiveEqualsTest_{Guid.NewGuid()}";
        var personWithNullTitle = new FakeTestingPersonBuilder()
            .WithTitle(null)
            .WithLastName(uniqueLastName)
            .WithFirstName("NullTitle")
            .Build();
        var personWithMatchingTitle = new FakeTestingPersonBuilder()
            .WithTitle("Doctor")
            .WithLastName(uniqueLastName)
            .WithFirstName("MatchingTitle")
            .Build();
        var personWithNonMatchingTitle = new FakeTestingPersonBuilder()
            .WithTitle("Mr.")
            .WithLastName(uniqueLastName)
            .WithFirstName("NonMatchingTitle")
            .Build();
        await testingServiceScope.InsertAsync(personWithNullTitle, personWithMatchingTitle, personWithNonMatchingTitle);

        var input = $"""Title ==* "doctor" && LastName == "{uniqueLastName}" """;

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(personWithMatchingTitle.Id);
    }

    [Fact]
    public async Task can_filter_nullable_string_with_case_insensitive_not_equals()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var uniqueLastName = $"CaseInsensitiveNotEqualsTest_{Guid.NewGuid()}";
        var personWithNullTitle = new FakeTestingPersonBuilder()
            .WithTitle(null)
            .WithLastName(uniqueLastName)
            .WithFirstName("NullTitle")
            .Build();
        var personWithMatchingTitle = new FakeTestingPersonBuilder()
            .WithTitle("Doctor")
            .WithLastName(uniqueLastName)
            .WithFirstName("MatchingTitle")
            .Build();
        var personWithNonMatchingTitle = new FakeTestingPersonBuilder()
            .WithTitle("Mr.")
            .WithLastName(uniqueLastName)
            .WithFirstName("NonMatchingTitle")
            .Build();
        await testingServiceScope.InsertAsync(personWithNullTitle, personWithMatchingTitle, personWithNonMatchingTitle);

        // Title !=* "doctor" should return records where Title is not "doctor" (case-insensitive)
        // null values should be treated as non-matching (i.e. they don't equal "doctor")
        var input = $"""Title !=* "doctor" && LastName == "{uniqueLastName}" """;

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert - should return null title and "Mr." title (both are != "doctor")
        people.Count.Should().Be(2);
        people.Should().Contain(p => p.Id == personWithNullTitle.Id);
        people.Should().Contain(p => p.Id == personWithNonMatchingTitle.Id);
    }

    [Fact]
    public async Task can_filter_nullable_string_with_case_insensitive_in_operator()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var uniqueLastName = $"CaseInsensitiveInTest_{Guid.NewGuid()}";
        var personWithNullTitle = new FakeTestingPersonBuilder()
            .WithTitle(null)
            .WithLastName(uniqueLastName)
            .WithFirstName("NullTitle")
            .Build();
        var personWithMatchingTitle = new FakeTestingPersonBuilder()
            .WithTitle("Doctor")
            .WithLastName(uniqueLastName)
            .WithFirstName("MatchingTitle")
            .Build();
        var personWithNonMatchingTitle = new FakeTestingPersonBuilder()
            .WithTitle("Mr.")
            .WithLastName(uniqueLastName)
            .WithFirstName("NonMatchingTitle")
            .Build();
        await testingServiceScope.InsertAsync(personWithNullTitle, personWithMatchingTitle, personWithNonMatchingTitle);

        var input = $"""Title ^^* ["doctor", "professor"] && LastName == "{uniqueLastName}" """;

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(personWithMatchingTitle.Id);
    }

    [Fact]
    public async Task can_filter_with_null_in_complex_expression()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var baseLastName = $"ComplexTest_{Guid.NewGuid()}";
        var personOne = new FakeTestingPersonBuilder()
            .WithTitle(null)
            .WithAge(30)
            .WithLastName(baseLastName)
            .WithFirstName("Person1")
            .Build();
        var personTwo = new FakeTestingPersonBuilder()
            .WithTitle("Mr.")
            .WithAge(null)
            .WithLastName(baseLastName)
            .WithFirstName("Person2")
            .Build();
        var personThree = new FakeTestingPersonBuilder()
            .WithTitle("Dr.")
            .WithAge(25)
            .WithLastName(baseLastName)
            .WithFirstName("Person3")
            .Build();
        await testingServiceScope.InsertAsync(personOne, personTwo, personThree);

        var input = $"""((Title == null && Age != null) || (Title != null && Age == null)) && LastName == "{baseLastName}" """;

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(2);
        people.Should().Contain(p => p.Id == personOne.Id);
        people.Should().Contain(p => p.Id == personTwo.Id);
        people.Should().NotContain(p => p.Id == personThree.Id);
    }

    [Fact]
    public async Task can_filter_multiple_nullable_fields_with_null()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var baseLastName = $"MultiNullTest_{Guid.NewGuid()}";
        var personAllNull = new FakeTestingPersonBuilder()
            .WithTitle(null)
            .WithAge(null)
            .WithRating(null)
            .WithLastName(baseLastName)
            .WithFirstName("AllNull")
            .Build();
        var personSomeNull = new FakeTestingPersonBuilder()
            .WithTitle("Mr.")
            .WithAge(null)
            .WithRating(4.5M)
            .WithLastName(baseLastName)
            .WithFirstName("SomeNull")
            .Build();
        var personNoneNull = new FakeTestingPersonBuilder()
            .WithTitle("Dr.")
            .WithAge(25)
            .WithRating(3.5M)
            .WithLastName(baseLastName)
            .WithFirstName("NoneNull")
            .Build();
        await testingServiceScope.InsertAsync(personAllNull, personSomeNull, personNoneNull);

        var input = $"""Title == null && Age == null && Rating == null && LastName == "{baseLastName}" """;

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(personAllNull.Id);
    }
}
