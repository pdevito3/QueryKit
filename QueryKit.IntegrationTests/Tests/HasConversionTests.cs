namespace QueryKit.IntegrationTests.Tests;

using Configuration;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SharedTestingHelper.Fakes;
using WebApiTestProject.Database;
using WebApiTestProject.Entities;
using Xunit.Abstractions;

public class HasConversionTests : TestBase
{
    [Fact]
    public async Task can_filter_by_email_with_has_conversion()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var faker = new Bogus.Faker();
        
        var testEmail = $"{Guid.NewGuid()}{faker.Internet.Email()}";
        var person = new FakeTestingPersonBuilder()
            .WithEmail(testEmail)
            .Build();
        var personTwo = new FakeTestingPersonBuilder().Build();
        
        await testingServiceScope.InsertAsync(person, personTwo);
        
        var input = $"""Email == "{testEmail}" """;
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<TestingPerson>(x => x.Email).HasConversion<string>();
        });
        
        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input, config);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(person.Id);
    }

    [Fact]
    public async Task can_filter_by_nested_postal_code_with_has_conversion()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var postalCode = Guid.NewGuid().ToString("N")[..10];
        var person = new FakeTestingPersonBuilder()
            .WithPhysicalAddress(new Address("Line1", "Line2", "City", "State", postalCode, "Country"))
            .Build();
        var personTwo = new FakeTestingPersonBuilder().Build();

        await testingServiceScope.InsertAsync(person, personTwo);

        var input = $"""PhysicalAddress.PostalCode == "{postalCode}" """;
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<TestingPerson>(x => x.PhysicalAddress.PostalCode).HasConversion<string>();
        });

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input, config);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(person.Id);
    }

    [Fact]
    public async Task can_filter_guid_with_contains_query_name_and_has_conversion()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var person = new FakeTestingPersonBuilder().Build();
        var personTwo = new FakeTestingPersonBuilder().Build();

        await testingServiceScope.InsertAsync(person, personTwo);

        var input = $"""identifier @= "{person.Id.ToString()[..13]}" """;
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<TestingPerson>(x => x.Id).HasQueryName("identifier").HasConversion<string>();
        });

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input, config);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(person.Id);
    }
}