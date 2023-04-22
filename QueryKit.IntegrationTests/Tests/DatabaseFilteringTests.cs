namespace QueryKit.IntegrationTests.Tests;

using Bogus;
using Fakes;
using FluentAssertions;
using WebApiTestProject.Entities;
using WebApiTestProject.Features;

public class DatabaseFilteringTests : TestBase
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
        var query = new GetPersonList.Query(input);
        var people = await testingServiceScope.SendAsync(query);

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(fakePersonOne.Id);
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
        var query = new GetPersonList.Query(input);
        var people = await testingServiceScope.SendAsync(query);

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(fakePersonOne.Id);
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
        var query = new GetPersonList.Query(input);
        var people = await testingServiceScope.SendAsync(query);

        // Assert
        people.Count.Should().Be(0);
    }
}