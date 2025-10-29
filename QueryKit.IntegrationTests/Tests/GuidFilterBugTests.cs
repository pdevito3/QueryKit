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
}
