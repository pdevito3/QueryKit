namespace QueryKit.IntegrationTests.Tests;

using Bogus;
using Configuration;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SharedTestingHelper.Fakes;
using SharedTestingHelper.Fakes.Author;
using SharedTestingHelper.Fakes.Recipes;
using WebApiTestProject.Entities;

public class PropertyResolverTests : TestBase
{
    [Fact]
    public async Task unknown_property_clause_under_or_is_true_by_default()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var title = new Faker().Lorem.Sentence();
        var fakePerson = new FakeTestingPersonBuilder()
            .WithTitle(title)
            .WithAge(30)
            .Build();
        await testingServiceScope.InsertAsync(fakePerson);

        var input = $"""Title == "{title}" && (Nope == "x" || Age > 100)""";
        var config = new QueryKitConfiguration(config =>
        {
            config.AllowUnknownProperties = true;
        });

        // Act
        var people = await testingServiceScope.DbContext().People
            .ApplyQueryKitFilter(input, config)
            .ToListAsync();

        // Assert
        people.Should().ContainSingle(x => x.Id == fakePerson.Id);
    }

    [Fact]
    public async Task unknown_property_clause_under_or_does_not_return_every_row()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var title = new Faker().Lorem.Sentence();
        var fakePerson = new FakeTestingPersonBuilder()
            .WithTitle(title)
            .WithAge(30)
            .Build();
        await testingServiceScope.InsertAsync(fakePerson);

        var input = $"""Title == "{title}" && (Nope == "x" || Age > 100)""";
        var config = new QueryKitConfiguration(config =>
        {
            config.IgnoredClauseBehavior = IgnoredClauseBehavior.Remove;
            config.AllowUnknownProperties = true;
        });

        // Act
        var people = await testingServiceScope.DbContext().People
            .ApplyQueryKitFilter(input, config)
            .ToListAsync();

        // Assert
        people.Should().BeEmpty();
    }

    [Fact]
    public async Task prevented_property_clause_under_or_does_not_return_every_row()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var title = new Faker().Lorem.Sentence();
        var fakePerson = new FakeTestingPersonBuilder()
            .WithTitle(title)
            .WithAge(30)
            .WithRating(1)
            .Build();
        await testingServiceScope.InsertAsync(fakePerson);

        var input = $"""Title == "{title}" && (Rating == 1 || Age > 100)""";
        var config = new QueryKitConfiguration(config =>
        {
            config.IgnoredClauseBehavior = IgnoredClauseBehavior.Remove;
            config.Property<TestingPerson>(x => x.Rating).PreventFilter();
        });

        // Act
        var people = await testingServiceScope.DbContext().People
            .ApplyQueryKitFilter(input, config)
            .ToListAsync();

        // Assert
        people.Should().BeEmpty();
    }

    [Fact]
    public async Task prevented_property_clause_by_its_query_name_under_or_does_not_return_every_row()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var title = new Faker().Lorem.Sentence();
        var fakePerson = new FakeTestingPersonBuilder()
            .WithTitle(title)
            .WithFirstName("Paul")
            .WithAge(30)
            .Build();
        await testingServiceScope.InsertAsync(fakePerson);

        var input = $"""Title == "{title}" && (first == "Paul" || Age > 100)""";
        var config = new QueryKitConfiguration(config =>
        {
            config.IgnoredClauseBehavior = IgnoredClauseBehavior.Remove;
            config.Property<TestingPerson>(x => x.FirstName).HasQueryName("first").PreventFilter().PreventSort();
        });

        // Act
        var people = await testingServiceScope.DbContext().People
            .ApplyQueryKitFilter(input, config)
            .ToListAsync();

        // Assert
        people.Should().BeEmpty();
    }

    [Fact]
    public async Task prevented_property_in_arithmetic_is_not_filtered()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var title = new Faker().Lorem.Sentence();
        var fakePerson = new FakeTestingPersonBuilder()
            .WithTitle(title)
            .WithAge(5)
            .Build();
        await testingServiceScope.InsertAsync(fakePerson);

        var input = $"""Title == "{title}" && (Age + 0) > 10""";
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<TestingPerson>(x => x.Age).PreventFilter();
        });

        // Act
        var people = await testingServiceScope.DbContext().People
            .ApplyQueryKitFilter(input, config)
            .ToListAsync();

        // Assert
        people.Should().ContainSingle(x => x.Id == fakePerson.Id);
    }

    [Fact]
    public async Task prevented_property_on_the_right_side_is_not_compared()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var title = new Faker().Lorem.Sentence();
        var fakePerson = new FakeTestingPersonBuilder()
            .WithTitle(title)
            .WithFirstName("Same")
            .WithLastName("Same")
            .WithAge(30)
            .Build();
        await testingServiceScope.InsertAsync(fakePerson);

        var input = $"""Title == "{title}" && (FirstName == LastName || Age > 100)""";
        var config = new QueryKitConfiguration(config =>
        {
            config.IgnoredClauseBehavior = IgnoredClauseBehavior.Remove;
            config.Property<TestingPerson>(x => x.LastName).PreventFilter();
        });

        // Act
        var people = await testingServiceScope.DbContext().People
            .ApplyQueryKitFilter(input, config)
            .ToListAsync();

        // Assert
        people.Should().BeEmpty();
    }

    [Fact]
    public async Task prevented_property_in_a_list_is_not_filtered_in_any_case()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var title = new Faker().Lorem.Sentence();
        var fakePerson = new FakeTestingPersonBuilder()
            .WithTitle(title)
            .WithFirstName("Paul")
            .WithLastName("Other")
            .Build();
        await testingServiceScope.InsertAsync(fakePerson);

        var input = $"""Title == "{title}" && (firstname, LastName) == "Paul" """;
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<TestingPerson>(x => x.FirstName).PreventFilter();
        });

        // Act
        var people = await testingServiceScope.DbContext().People
            .ApplyQueryKitFilter(input, config)
            .ToListAsync();

        // Assert
        people.Should().BeEmpty();
    }

    [Fact]
    public async Task prevented_sort_property_with_a_query_name_is_not_sorted_when_written_by_its_member_name()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var title = new Faker().Lorem.Sentence();
        var firstPerson = new FakeTestingPersonBuilder()
            .WithTitle(title)
            .WithFirstName("A")
            .WithAge(1)
            .Build();
        var secondPerson = new FakeTestingPersonBuilder()
            .WithTitle(title)
            .WithFirstName("B")
            .WithAge(2)
            .Build();
        await testingServiceScope.InsertAsync(firstPerson, secondPerson);

        var input = "firstname desc, Age";
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<TestingPerson>(x => x.FirstName).HasQueryName("first").PreventSort();
        });

        // Act
        var people = await testingServiceScope.DbContext().People
            .Where(x => x.Title == title)
            .ApplyQueryKitSort(input, config)
            .ToListAsync();

        // Assert
        people.Select(x => x.Id).Should().Equal(firstPerson.Id, secondPerson.Id);
    }

    [Fact]
    public async Task prevented_custom_operation_is_not_filtered()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var title = new Faker().Lorem.Sentence();
        var fakePerson = new FakeTestingPersonBuilder()
            .WithTitle(title)
            .WithAge(5)
            .Build();
        await testingServiceScope.InsertAsync(fakePerson);

        var input = $"""Title == "{title}" && adult == true""";
        var config = new QueryKitConfiguration(config =>
        {
            config.CustomOperation<TestingPerson>((x, op, value) => x.Age > 17).HasQueryName("adult").PreventFilter();
        });

        // Act
        var people = await testingServiceScope.DbContext().People
            .ApplyQueryKitFilter(input, config)
            .ToListAsync();

        // Assert
        people.Should().ContainSingle(x => x.Id == fakePerson.Id);
    }

    [Fact]
    public async Task prevented_derived_property_is_not_filtered()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var title = new Faker().Lorem.Sentence();
        var fakePerson = new FakeTestingPersonBuilder()
            .WithTitle(title)
            .WithFirstName("Paul")
            .WithLastName("Other")
            .Build();
        await testingServiceScope.InsertAsync(fakePerson);

        var input = $"""Title == "{title}" && full == "no match" """;
        var config = new QueryKitConfiguration(config =>
        {
            config.DerivedProperty<TestingPerson>(x => x.FirstName + " " + x.LastName).HasQueryName("full").PreventFilter();
        });

        // Act
        var people = await testingServiceScope.DbContext().People
            .ApplyQueryKitFilter(input, config)
            .ToListAsync();

        // Assert
        people.Should().ContainSingle(x => x.Id == fakePerson.Id);
    }

    [Fact]
    public async Task prevented_derived_sort_property_is_not_sorted()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var title = new Faker().Lorem.Sentence();
        var firstPerson = new FakeTestingPersonBuilder()
            .WithTitle(title)
            .WithFirstName("A")
            .WithAge(1)
            .Build();
        var secondPerson = new FakeTestingPersonBuilder()
            .WithTitle(title)
            .WithFirstName("B")
            .WithAge(2)
            .Build();
        await testingServiceScope.InsertAsync(firstPerson, secondPerson);

        var input = "full desc, Age";
        var config = new QueryKitConfiguration(config =>
        {
            config.DerivedProperty<TestingPerson>(x => x.FirstName + " " + x.LastName).HasQueryName("full").PreventSort();
        });

        // Act
        var people = await testingServiceScope.DbContext().People
            .Where(x => x.Title == title)
            .ApplyQueryKitSort(input, config)
            .ToListAsync();

        // Assert
        people.Select(x => x.Id).Should().Equal(firstPerson.Id, secondPerson.Id);
    }

    [Fact]
    public async Task query_name_in_a_property_list_is_filtered()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var title = new Faker().Lorem.Sentence();
        var fakePerson = new FakeTestingPersonBuilder()
            .WithTitle(title)
            .WithFirstName("Paul")
            .WithLastName("Other")
            .Build();
        await testingServiceScope.InsertAsync(fakePerson);

        var input = $"""Title == "{title}" && (first, LastName) == "Paul" """;
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<TestingPerson>(x => x.FirstName).HasQueryName("first");
        });

        // Act
        var people = await testingServiceScope.DbContext().People
            .ApplyQueryKitFilter(input, config)
            .ToListAsync();

        // Assert
        people.Should().ContainSingle(x => x.Id == fakePerson.Id);
    }

    [Fact]
    public async Task query_name_in_arithmetic_is_filtered()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var title = new Faker().Lorem.Sentence();
        var fakePerson = new FakeTestingPersonBuilder()
            .WithTitle(title)
            .WithAge(30)
            .Build();
        await testingServiceScope.InsertAsync(fakePerson);

        var input = $"""Title == "{title}" && (years + 0) > 20""";
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<TestingPerson>(x => x.Age).HasQueryName("years");
        });

        // Act
        var people = await testingServiceScope.DbContext().People
            .ApplyQueryKitFilter(input, config)
            .ToListAsync();

        // Assert
        people.Should().ContainSingle(x => x.Id == fakePerson.Id);
    }

    [Fact]
    public async Task property_path_on_the_right_side_is_compared()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var name = Guid.NewGuid().ToString();
        var matchingRecipe = new FakeRecipeBuilder()
            .WithTitle(name)
            .Build();
        matchingRecipe.SetAuthor(new FakeAuthorBuilder().WithName(name).Build());
        var otherRecipe = new FakeRecipeBuilder()
            .WithTitle(name)
            .Build();
        otherRecipe.SetAuthor(new FakeAuthorBuilder().WithName(Guid.NewGuid().ToString()).Build());
        await testingServiceScope.InsertAsync(matchingRecipe, otherRecipe);

        var input = $"""Title == "{name}" && Title == Author.Name""";

        // Act
        var recipes = await testingServiceScope.DbContext().Recipes
            .ApplyQueryKitFilter(input)
            .ToListAsync();

        // Assert
        recipes.Should().ContainSingle(x => x.Id == matchingRecipe.Id);
    }

    [Fact]
    public async Task unknown_property_in_arithmetic_removes_the_clause_when_unknown_properties_are_allowed()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var title = new Faker().Lorem.Sentence();
        var fakePerson = new FakeTestingPersonBuilder()
            .WithTitle(title)
            .WithAge(30)
            .Build();
        await testingServiceScope.InsertAsync(fakePerson);

        var input = $"""Title == "{title}" && ((Nope + 1) > 3 || Age > 100)""";
        var config = new QueryKitConfiguration(config =>
        {
            config.IgnoredClauseBehavior = IgnoredClauseBehavior.Remove;
            config.AllowUnknownProperties = true;
        });

        // Act
        var people = await testingServiceScope.DbContext().People
            .ApplyQueryKitFilter(input, config)
            .ToListAsync();

        // Assert
        people.Should().BeEmpty();
    }
}
