namespace QueryKit.IntegrationTests.Tests;

using Bogus;
using Fakes;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

public class DatabaseSortingTests : TestBase
{
    [Fact]
    public async Task can_sort_multiple_items()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var faker = new Faker();
        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithTitle("alpha")
            .WithAge(10)
            .Build();
        var fakePersonTwo = new FakeTestingPersonBuilder()
            .WithTitle("alpha")
            .WithAge(100)
            .Build();
        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo);
        
        var input = $"Title asc, Age desc";

        // Act
        var queryablePeople = testingServiceScope.DbContext().People
            .Where(x => x.Id == fakePersonOne.Id || x.Id == fakePersonTwo.Id);
        var appliedQueryable = queryablePeople.ApplyQueryKitSort(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(2);
        people[0].Id.Should().Be(fakePersonTwo.Id);
        people[1].Id.Should().Be(fakePersonOne.Id);
    }
    
    [Fact]
    public async Task can_sort_multiple_items_with_same_order_direction()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var faker = new Faker();
        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithTitle("alpha")
            .WithAge(10)
            .WithBirthMonth("January")
            .Build();
        var fakePersonTwo = new FakeTestingPersonBuilder()
            .WithTitle("beta")
            .WithAge(100)
            .WithBirthMonth("February")
            .Build();
        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo);

        var input = $"Title asc, Age asc, BirthMonth asc";

        // Act
        var queryablePeople = testingServiceScope.DbContext().People
            .Where(x => x.Id == fakePersonOne.Id || x.Id == fakePersonTwo.Id);
        var appliedQueryable = queryablePeople.ApplyQueryKitSort(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(2);
        people[0].Id.Should().Be(fakePersonOne.Id);
        people[1].Id.Should().Be(fakePersonTwo.Id);
    }
}