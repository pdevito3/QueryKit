namespace QueryKit.IntegrationTests.Tests;

using Bogus;
using Fakes;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

public class DatabaseSortingTests : TestBase
{
    [Fact]
    public async Task can_sort_items_with_mixed_order_directions()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
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
        var fakePersonThree = new FakeTestingPersonBuilder()
            .WithTitle("beta")
            .WithAge(50)
            .WithBirthMonth("March")
            .Build();
        var fakePersonFour = new FakeTestingPersonBuilder()
            .WithTitle("beta")
            .WithAge(20)
            .WithBirthMonth("April")
            .Build();
        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo, fakePersonThree, fakePersonFour);

        var input = $"Title desc, Age asc, BirthMonth desc";

        // Act
        var queryablePeople = testingServiceScope.DbContext().People
            .Where(x => new[] { fakePersonOne.Id, fakePersonTwo.Id, fakePersonThree.Id, fakePersonFour.Id }.Contains(x.Id));
        var appliedQueryable = queryablePeople.ApplyQueryKitSort(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(4);
        people[0].Id.Should().Be(fakePersonFour.Id);
        people[1].Id.Should().Be(fakePersonThree.Id);
        people[2].Id.Should().Be(fakePersonTwo.Id);
        people[3].Id.Should().Be(fakePersonOne.Id);
    }
    
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
    
    [Fact]
    public async Task can_sort_items_with_null_value_property()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithTitle("alpha")
            .WithAge(10)
            .WithBirthMonth(null)
            .Build();
        var fakePersonTwo = new FakeTestingPersonBuilder()
            .WithTitle("alpha")
            .WithAge(100)
            .WithBirthMonth("January")
            .Build();
        var fakePersonThree = new FakeTestingPersonBuilder()
            .WithTitle("beta")
            .WithAge(50)
            .WithBirthMonth("March")
            .Build();
        var fakePersonFour = new FakeTestingPersonBuilder()
            .WithTitle("beta")
            .WithAge(20)
            .WithBirthMonth("February")
            .Build();
        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo, fakePersonThree, fakePersonFour);

        var input = $"Title asc, BirthMonth asc";

        // Act
        var queryablePeople = testingServiceScope.DbContext().People
            .Where(x => new[] { fakePersonOne.Id, fakePersonTwo.Id, fakePersonThree.Id, fakePersonFour.Id }.Contains(x.Id));
        var appliedQueryable = queryablePeople.ApplyQueryKitSort(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(4);
        people[0].Id.Should().Be(fakePersonTwo.Id); 
        people[1].Id.Should().Be(fakePersonOne.Id); // null BirthMonth
        people[2].Id.Should().Be(fakePersonFour.Id);
        people[3].Id.Should().Be(fakePersonThree.Id);
    }
}