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
}
