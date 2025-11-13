namespace QueryKit.IntegrationTests.Tests;

using Bogus;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SharedTestingHelper.Fakes;
using WebApiTestProject.Entities;

public class PropertyListGroupingTests : TestBase
{
    [Fact]
    public async Task can_filter_with_property_list_case_insensitive_contains()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var uniqueString = Guid.NewGuid().ToString();

        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithFirstName(uniqueString)
            .WithLastName("Smith")
            .Build();

        var fakePersonTwo = new FakeTestingPersonBuilder()
            .WithFirstName("John")
            .WithLastName(uniqueString)
            .Build();

        var fakePersonThree = new FakeTestingPersonBuilder()
            .WithFirstName("Jane")
            .WithLastName("Doe")
            .Build();

        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo, fakePersonThree);

        var input = $"""(FirstName, LastName) @=* "{uniqueString}" """;

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(2);
        people.Should().Contain(p => p.Id == fakePersonOne.Id);
        people.Should().Contain(p => p.Id == fakePersonTwo.Id);
    }

    [Fact]
    public async Task can_filter_with_property_list_exact_match()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var uniqueString = Guid.NewGuid().ToString();

        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithFirstName(uniqueString)
            .WithLastName("Smith")
            .WithTitle(uniqueString)
            .Build();

        var fakePersonTwo = new FakeTestingPersonBuilder()
            .WithFirstName("Inactive")
            .WithLastName("Doe")
            .WithTitle("Developer")
            .Build();

        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo);

        var input = $"""(FirstName, Title) == "{uniqueString}" """;

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(fakePersonOne.Id);
    }

    [Fact]
    public async Task can_filter_with_property_list_three_properties()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var uniqueString = Guid.NewGuid().ToString();

        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithFirstName(uniqueString)
            .WithLastName("Smith")
            .WithTitle("Developer")
            .Build();

        var fakePersonTwo = new FakeTestingPersonBuilder()
            .WithFirstName("John")
            .WithLastName(uniqueString)
            .WithTitle("Designer")
            .Build();

        var fakePersonThree = new FakeTestingPersonBuilder()
            .WithFirstName("Bob")
            .WithLastName("Brown")
            .WithTitle(uniqueString)
            .Build();

        var fakePersonFour = new FakeTestingPersonBuilder()
            .WithFirstName("Jane")
            .WithLastName("Doe")
            .WithTitle("Manager")
            .Build();

        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo, fakePersonThree, fakePersonFour);

        var input = $"""(FirstName, LastName, Title) @=* "{uniqueString}" """;

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(3);
        people.Should().Contain(p => p.Id == fakePersonOne.Id);
        people.Should().Contain(p => p.Id == fakePersonTwo.Id);
        people.Should().Contain(p => p.Id == fakePersonThree.Id);
    }

    [Fact]
    public async Task can_filter_with_property_list_starts_with()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var uniquePrefix = Guid.NewGuid().ToString().Substring(0, 8);

        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithFirstName($"{uniquePrefix}Alice")
            .WithLastName("Anderson")
            .Build();

        var fakePersonTwo = new FakeTestingPersonBuilder()
            .WithFirstName("Bob")
            .WithLastName($"{uniquePrefix}Adams")
            .Build();

        var fakePersonThree = new FakeTestingPersonBuilder()
            .WithFirstName("Charlie")
            .WithLastName("Brown")
            .Build();

        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo, fakePersonThree);

        var input = $"""(FirstName, LastName) _=* "{uniquePrefix}" """;

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(2);
        people.Should().Contain(p => p.Id == fakePersonOne.Id);
        people.Should().Contain(p => p.Id == fakePersonTwo.Id);
    }

    [Fact]
    public async Task can_filter_with_property_list_ends_with()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var uniqueSuffix = Guid.NewGuid().ToString().Substring(0, 8);

        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithFirstName("James")
            .WithLastName($"Smith{uniqueSuffix}")
            .Build();

        var fakePersonTwo = new FakeTestingPersonBuilder()
            .WithFirstName($"John{uniqueSuffix}")
            .WithLastName("Johnson")
            .Build();

        var fakePersonThree = new FakeTestingPersonBuilder()
            .WithFirstName("Jane")
            .WithLastName("Doe")
            .Build();

        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo, fakePersonThree);

        var input = $"""(FirstName, LastName) _-=* "{uniqueSuffix}" """;

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(2);
        people.Should().Contain(p => p.Id == fakePersonOne.Id);
        people.Should().Contain(p => p.Id == fakePersonTwo.Id);
    }

    [Fact]
    public async Task can_filter_with_property_list_combined_with_other_conditions()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var uniqueString = Guid.NewGuid().ToString();

        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithFirstName(uniqueString)
            .WithLastName("Smith")
            .WithAge(30)
            .Build();

        var fakePersonTwo = new FakeTestingPersonBuilder()
            .WithFirstName("John")
            .WithLastName(uniqueString)
            .WithAge(25)
            .Build();

        var fakePersonThree = new FakeTestingPersonBuilder()
            .WithFirstName("Jane")
            .WithLastName("Doe")
            .WithAge(35)
            .Build();

        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo, fakePersonThree);

        var input = $"""(FirstName, LastName) @=* "{uniqueString}" && Age > 25""";

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(fakePersonOne.Id);
    }

    [Fact]
    public async Task can_filter_with_property_list_single_property()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var uniqueString = Guid.NewGuid().ToString();

        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithFirstName(uniqueString)
            .Build();

        var fakePersonTwo = new FakeTestingPersonBuilder()
            .WithFirstName("John")
            .Build();

        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo);

        var input = $"""(FirstName) @=* "{uniqueString}" """;

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(fakePersonOne.Id);
    }

    [Fact]
    public async Task can_filter_with_property_list_and_parentheses_for_grouping()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var uniqueString = Guid.NewGuid().ToString();

        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithFirstName(uniqueString)
            .WithLastName("Smith")
            .WithAge(30)
            .Build();

        var fakePersonTwo = new FakeTestingPersonBuilder()
            .WithFirstName("John")
            .WithLastName("Doe")
            .WithAge(25)
            .Build();

        var fakePersonThree = new FakeTestingPersonBuilder()
            .WithFirstName("Jane")
            .WithLastName(uniqueString)
            .WithAge(20)
            .Build();

        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo, fakePersonThree);

        var personIds = new[] { fakePersonOne.Id, fakePersonTwo.Id, fakePersonThree.Id };
        var input = $"""((FirstName, LastName) @=* "{uniqueString}" || Age == 25) && Age >= 25""";

        // Act
        var queryablePeople = testingServiceScope.DbContext().People
            .Where(p => personIds.Contains(p.Id)); // Only look at records we just created
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(2);
        people.Should().Contain(p => p.Id == fakePersonOne.Id);
        people.Should().Contain(p => p.Id == fakePersonTwo.Id);
    }

    [Fact]
    public async Task can_filter_with_property_list_not_contains()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var uniqueString = Guid.NewGuid().ToString();

        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithFirstName(uniqueString)
            .WithLastName("Smith")
            .Build();

        var fakePersonTwo = new FakeTestingPersonBuilder()
            .WithFirstName("John")
            .WithLastName("Doe")
            .Build();

        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo);

        var personIds = new[] { fakePersonOne.Id, fakePersonTwo.Id };
        var input = $"""(FirstName, LastName) !@=* "{uniqueString}" """;

        // Act
        var queryablePeople = testingServiceScope.DbContext().People
            .Where(p => personIds.Contains(p.Id)); // Only look at records we just created
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert - Should get the person who doesn't have the unique string in either field
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(fakePersonTwo.Id);
    }
}
