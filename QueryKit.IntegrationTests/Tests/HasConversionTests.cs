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
}