namespace QueryKit.IntegrationTests.Tests;

using Bogus;
using Configuration;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SharedTestingHelper.Fakes;
using WebApiTestProject.Entities;
using Xunit.Abstractions;

public class GuidFilterBugTests(ITestOutputHelper testOutputHelper) : TestBase
{
    [Fact]
    public async Task guid_filter_with_converted_property_should_reproduce_ef_core_translation_error()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var faker = new Faker();

        var targetId = Guid.Parse("ab7afb17-abca-4530-9f8f-bd1497e0f6be");
        var targetEmail = faker.Internet.Email();
        var targetPerson = new FakeTestingPersonBuilder()
            .WithId(targetId)
            .WithEmail(targetEmail)
            .WithFirstName("Target")
            .Build();

        var otherSucceededEmail = faker.Internet.Email();
        var otherPerson = new FakeTestingPersonBuilder()
            .WithEmail(otherSucceededEmail)
            .WithFirstName("Other")
            .Build();

        await testingServiceScope.InsertAsync(targetPerson, otherPerson);

        // Configure QueryKit with custom query names like the user's real scenario
        // This mimics: Status (with EF HasConversion) and Id (Guid)
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<TestingPerson>(x => x.Email)
                .HasQueryName("email")
                .HasConversion<string>();
            config.Property<TestingPerson>(x => x.Id)
                .HasQueryName("id");
        });

        // Filter using both email (converted property) and id (Guid)
        var input = $"""email == "{targetEmail}" && id == "{targetId}" """;

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input, config);

        // This should throw: System.InvalidOperationException:
        // The LINQ expression 'DbSet<TestingPerson>()
        //     .Where(d => d.Id.ToString() == "ab7afb17-abca-4530-9f8f-bd1497e0f6be" && ...)'
        // could not be translated
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(targetId);
        people[0].Email.Value.Should().Be(targetEmail);
        people[0].FirstName.Should().Be("Target");
    }

    [Fact]
    public async Task can_filter_by_guid_id_with_custom_query_name_and_additional_filters()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();

        var targetId = Guid.Parse("cd8efb28-bcdb-5641-af9f-ce2498f1c7cf");
        var targetPerson = new FakeTestingPersonBuilder()
            .WithId(targetId)
            .WithFirstName("Target")
            .WithLastName("Person")
            .Build();

        var otherPerson = new FakeTestingPersonBuilder()
            .WithFirstName("Other")
            .WithLastName("Different")
            .Build();

        await testingServiceScope.InsertAsync(targetPerson, otherPerson);

        // Configure QueryKit with custom query name for Id property
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<TestingPerson>(x => x.Id)
                .HasQueryName("id");
        });

        // Filter with both id and lastname
        var input = $"""id == "{targetId}" && LastName == "Person" """;

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input, config);

        // This should throw the same EF Core translation error
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(targetId);
        people[0].FirstName.Should().Be("Target");
        people[0].LastName.Should().Be("Person");
    }

    [Fact]
    public async Task guid_filter_with_inequality_and_complex_conditions_reproduces_ef_core_issue()
    {
        // This test specifically reproduces the scenario from the user's bug report:
        // status != "Finalized" && id == "guid-value"
        // Without the Trim('"') fix in FilterParser.cs:450, this could fail if GUIDs
        // with quotes somehow bypass the Sprache parser's quote stripping.

        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var faker = new Faker();

        var targetId = Guid.Parse("ef123456-bcda-4321-9876-fedcba987654");
        var targetTitle = "NotFinalizedTitle";
        var targetPerson = new FakeTestingPersonBuilder()
            .WithId(targetId)
            .WithTitle(targetTitle)
            .WithFirstName("Target")
            .WithAge(30)
            .Build();

        var finalizedPerson = new FakeTestingPersonBuilder()
            .WithTitle("Finalized")
            .WithFirstName("Finalized")
            .WithAge(40)
            .Build();

        var otherPersonSameTitle = new FakeTestingPersonBuilder()
            .WithTitle(targetTitle)
            .WithFirstName("Other")
            .WithAge(50)
            .Build();

        await testingServiceScope.InsertAsync(targetPerson, finalizedPerson, otherPersonSameTitle);

        // Configure QueryKit similar to user's scenario with custom query names
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<TestingPerson>(x => x.Title)
                .HasQueryName("status");
            config.Property<TestingPerson>(x => x.Id)
                .HasQueryName("id");
        });

        // Filter mimicking user's exact scenario: status != "Finalized" && id == "{guid}"
        var input = $"""status != "Finalized" && id == "{targetId}" """;

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input, config);

        // Without the defensive Trim('"') in CreateRightExprFromType, this could throw:
        // System.InvalidOperationException: The LINQ expression could not be translated
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1, "Should only return the target person (not finalized and matching ID)");
        people[0].Id.Should().Be(targetId);
        people[0].Title.Should().Be(targetTitle);
        people[0].FirstName.Should().Be("Target");
    }

    [Fact]
    public async Task multiple_guid_filters_with_or_conditions_work_correctly()
    {
        // Test more complex GUID filtering scenarios that stress-test the parser

        // Arrange
        var testingServiceScope = new TestingServiceScope();

        var id1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var id2 = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var id3 = Guid.Parse("33333333-3333-3333-3333-333333333333");

        var person1 = new FakeTestingPersonBuilder()
            .WithId(id1)
            .WithFirstName("Person1")
            .Build();

        var person2 = new FakeTestingPersonBuilder()
            .WithId(id2)
            .WithFirstName("Person2")
            .Build();

        var person3 = new FakeTestingPersonBuilder()
            .WithId(id3)
            .WithFirstName("Person3")
            .Build();

        await testingServiceScope.InsertAsync(person1, person2, person3);

        var config = new QueryKitConfiguration(config =>
        {
            config.Property<TestingPerson>(x => x.Id)
                .HasQueryName("id");
        });

        // Complex filter with multiple GUID conditions
        var input = $"""(id == "{id1}" || id == "{id2}") && FirstName != "Person3" """;

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input, config);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(2);
        people.Should().Contain(p => p.Id == id1);
        people.Should().Contain(p => p.Id == id2);
        people.Should().NotContain(p => p.Id == id3);
    }

    [Fact]
    public async Task can_filter_by_email_and_guid_id_with_custom_query_names()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var faker = new Faker();

        var targetId = Guid.Parse("de9ffc39-cdec-6752-bf0f-df3509f2d8df");
        var targetEmail = faker.Internet.Email();
        var targetPerson = new FakeTestingPersonBuilder()
            .WithId(targetId)
            .WithEmail(targetEmail)
            .WithFirstName("TargetMatch")
            .Build();

        var otherEmail = faker.Internet.Email();
        var otherPerson = new FakeTestingPersonBuilder()
            .WithEmail(otherEmail)
            .WithFirstName("Other")
            .Build();

        var anotherPerson = new FakeTestingPersonBuilder()
            .WithEmail(faker.Internet.Email())
            .WithFirstName("Another")
            .Build();

        await testingServiceScope.InsertAsync(targetPerson, otherPerson, anotherPerson);

        // Configure QueryKit with custom query name for email and id
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<TestingPerson>(x => x.Email)
                .HasQueryName("email")
                .HasConversion<string>();
            config.Property<TestingPerson>(x => x.Id)
                .HasQueryName("id");
        });

        // Filter using both email (custom name) and id
        var input = $"""email == "{targetEmail}" && id == "{targetId}" """;

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input, config);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1, "Should only return person matching both email and ID");
        people[0].Id.Should().Be(targetId);
        people[0].Email.Value.Should().Be(targetEmail);
        people[0].FirstName.Should().Be("TargetMatch");
    }
}
