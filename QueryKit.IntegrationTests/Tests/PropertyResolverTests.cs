namespace QueryKit.IntegrationTests.Tests;

using Bogus;
using Configuration;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SharedTestingHelper.Fakes;
using WebApiTestProject.Entities;

public class PropertyResolverTests : TestBase
{
    [Fact]
    public async Task unknown_property_clause_under_or_is_true_when_replaced_with_true()
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
            config.IgnoredClauseBehavior = IgnoredClauseBehavior.ReplaceWithTrue;
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
            config.Property<TestingPerson>(x => x.Rating!).PreventFilter();
        });

        // Act
        var people = await testingServiceScope.DbContext().People
            .ApplyQueryKitFilter(input, config)
            .ToListAsync();

        // Assert
        people.Should().BeEmpty();
    }

    [Fact]
    public async Task prevented_property_is_found_by_its_path_when_another_query_name_matches_that_path()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var firstName = new Faker().Lorem.Sentence();
        var fakePerson = new FakeTestingPersonBuilder()
            .WithFirstName(firstName)
            .Build();
        var otherPerson = new FakeTestingPersonBuilder().Build();
        await testingServiceScope.InsertAsync(fakePerson, otherPerson);

        var input = $"""nick == "{firstName}" """;
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<TestingPerson>(x => x.Title!).HasQueryName("firstname");
            config.Property<TestingPerson>(x => x.FirstName!).HasQueryName("nick").PreventFilter();
        });

        // Act
        var queryable = testingServiceScope.DbContext().People.ApplyQueryKitFilter(input, config);
        var people = await queryable.ToListAsync();

        // Assert
        queryable.ToQueryString().Should().NotContain("WHERE");
        people.Should().Contain(x => x.Id == fakePerson.Id);
        people.Should().Contain(x => x.Id == otherPerson.Id);
    }

    [Fact]
    public async Task property_is_not_prevented_by_another_property_whose_query_name_matches_its_path()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var firstName = new Faker().Lorem.Sentence();
        var fakePerson = new FakeTestingPersonBuilder()
            .WithFirstName(firstName)
            .Build();
        var otherPerson = new FakeTestingPersonBuilder().Build();
        await testingServiceScope.InsertAsync(fakePerson, otherPerson);

        var input = $"""nick == "{firstName}" """;
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<TestingPerson>(x => x.Title!).HasQueryName("firstname").PreventFilter();
            config.Property<TestingPerson>(x => x.FirstName!).HasQueryName("nick");
        });

        // Act
        var people = await testingServiceScope.DbContext().People
            .ApplyQueryKitFilter(input, config)
            .ToListAsync();

        // Assert
        people.Should().ContainSingle();
        people[0].Id.Should().Be(fakePerson.Id);
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
            config.Property<TestingPerson>(x => x.Age!).PreventFilter();
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
            config.Property<TestingPerson>(x => x.LastName!).PreventFilter();
        });

        // Act
        var people = await testingServiceScope.DbContext().People
            .ApplyQueryKitFilter(input, config)
            .ToListAsync();

        // Assert
        people.Should().BeEmpty();
    }

    [Fact]
    public async Task prevented_property_on_the_right_side_is_not_compared_when_another_query_name_matches_its_name()
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
            config.Property<TestingPerson>(x => x.FirstName!).HasQueryName("lastname");
            config.Property<TestingPerson>(x => x.LastName!).PreventFilter();
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
            config.Property<TestingPerson>(x => x.FirstName!).PreventFilter();
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
            config.Property<TestingPerson>(x => x.FirstName!).HasQueryName("first").PreventSort();
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
            config.Property<TestingPerson>(x => x.FirstName!).HasQueryName("first");
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
            config.Property<TestingPerson>(x => x.Age!).HasQueryName("years");
        });

        // Act
        var people = await testingServiceScope.DbContext().People
            .ApplyQueryKitFilter(input, config)
            .ToListAsync();

        // Assert
        people.Should().ContainSingle(x => x.Id == fakePerson.Id);
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
            config.Property<TestingPerson>(x => x.FirstName!).HasQueryName("first").PreventFilter().PreventSort();
        });

        // Act
        var people = await testingServiceScope.DbContext().People
            .ApplyQueryKitFilter(input, config)
            .ToListAsync();

        // Assert
        people.Should().BeEmpty();
    }

    [Theory]
    [InlineData("first-name")]
    [InlineData("_first")]
    [InlineData("first name")]
    public async Task query_name_that_is_not_a_plain_identifier_filters_by_its_property(string queryName)
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var firstName = new Faker().Lorem.Sentence();
        var fakePerson = new FakeTestingPersonBuilder()
            .WithFirstName(firstName)
            .Build();
        await testingServiceScope.InsertAsync(fakePerson);

        var input = $"""{queryName} == "{firstName}" """;
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<TestingPerson>(x => x.FirstName!).HasQueryName(queryName);
        });

        // Act
        var people = await testingServiceScope.DbContext().People
            .ApplyQueryKitFilter(input, config)
            .ToListAsync();

        // Assert
        people.Should().ContainSingle();
        people[0].Id.Should().Be(fakePerson.Id);
    }

    [Fact]
    public async Task non_public_mapped_property_filters_in_the_database()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var nickname = new Faker().Lorem.Sentence();
        var fakePerson = new FakeTestingPersonBuilder().Build();
        fakePerson.Nickname = nickname;
        var otherPerson = new FakeTestingPersonBuilder().Build();
        otherPerson.Nickname = new Faker().Lorem.Sentence();
        await testingServiceScope.InsertAsync(fakePerson, otherPerson);

        var input = $"""nickname == "{nickname}" """;

        // Act
        var queryable = testingServiceScope.DbContext().People.ApplyQueryKitFilter(input);
        var people = await queryable.ToListAsync();

        // Assert
        queryable.ToQueryString().Should().Contain("""p.nickname = """);
        people.Should().ContainSingle();
        people[0].Id.Should().Be(fakePerson.Id);
    }

    [Fact]
    public async Task non_public_mapped_property_filters_when_unknown_properties_are_allowed()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var nickname = new Faker().Lorem.Sentence();
        var fakePerson = new FakeTestingPersonBuilder().Build();
        fakePerson.Nickname = nickname;
        var otherPerson = new FakeTestingPersonBuilder().Build();
        otherPerson.Nickname = new Faker().Lorem.Sentence();
        await testingServiceScope.InsertAsync(fakePerson, otherPerson);

        var input = $"""Nickname == "{nickname}" """;
        var config = new QueryKitConfiguration(config =>
        {
            config.AllowUnknownProperties = true;
        });

        // Act
        var people = await testingServiceScope.DbContext().People
            .ApplyQueryKitFilter(input, config)
            .ToListAsync();

        // Assert
        people.Should().ContainSingle();
        people[0].Id.Should().Be(fakePerson.Id);
    }
}
