namespace QueryKit.IntegrationTests.Tests;

using Bogus;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SharedTestingHelper.Fakes;
using WebApiTestProject.Entities;

public class PropertyListGroupingEdgeCasesTests : TestBase
{
    [Fact]
    public async Task can_filter_with_property_list_numeric_greater_than()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();

        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithAge(30)
            .WithRating(4.5m)
            .Build();

        var fakePersonTwo = new FakeTestingPersonBuilder()
            .WithAge(20)
            .WithRating(5.5m)
            .Build();

        var fakePersonThree = new FakeTestingPersonBuilder()
            .WithAge(15)
            .WithRating(3.0m)
            .Build();

        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo, fakePersonThree);

        var personIds = new[] { fakePersonOne.Id, fakePersonTwo.Id, fakePersonThree.Id };
        var input = """(Age, Rating) > 25""";

        // Act
        var queryablePeople = testingServiceScope.DbContext().People
            .Where(p => personIds.Contains(p.Id));
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert - Should match age > 25 OR rating > 25 (none have rating > 25, so just age)
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(fakePersonOne.Id);
    }

    [Fact]
    public async Task can_filter_with_property_list_numeric_less_than_or_equal()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();

        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithAge(10)
            .WithRating(2.0m)
            .Build();

        var fakePersonTwo = new FakeTestingPersonBuilder()
            .WithAge(20)
            .WithRating(3.5m)
            .Build();

        var fakePersonThree = new FakeTestingPersonBuilder()
            .WithAge(30)
            .WithRating(4.5m)
            .Build();

        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo, fakePersonThree);

        var personIds = new[] { fakePersonOne.Id, fakePersonTwo.Id, fakePersonThree.Id };
        var input = """(Age, Age) <= 20""";

        // Act
        var queryablePeople = testingServiceScope.DbContext().People
            .Where(p => personIds.Contains(p.Id));
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert - Should match age <= 20
        people.Count.Should().Be(2);
        people.Should().Contain(p => p.Id == fakePersonOne.Id);
        people.Should().Contain(p => p.Id == fakePersonTwo.Id);
    }

    [Fact]
    public async Task can_filter_with_property_list_decimal_properties()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();

        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithRating(2.5m)
            .Build();

        var fakePersonTwo = new FakeTestingPersonBuilder()
            .WithRating(4.5m)
            .Build();

        var fakePersonThree = new FakeTestingPersonBuilder()
            .WithRating(5.0m)
            .Build();

        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo, fakePersonThree);

        var personIds = new[] { fakePersonOne.Id, fakePersonTwo.Id, fakePersonThree.Id };
        var input = """(Rating, Rating) >= 4.5""";

        // Act
        var queryablePeople = testingServiceScope.DbContext().People
            .Where(p => personIds.Contains(p.Id));
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(2);
        people.Should().Contain(p => p.Id == fakePersonTwo.Id);
        people.Should().Contain(p => p.Id == fakePersonThree.Id);
    }

    [Fact]
    public async Task can_filter_with_property_list_five_properties()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var uniqueString = Guid.NewGuid().ToString();

        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithFirstName("Alice")
            .WithLastName("Smith")
            .WithTitle("Developer")
            .Build();

        var fakePersonTwo = new FakeTestingPersonBuilder()
            .WithFirstName("Bob")
            .WithLastName("Johnson")
            .WithTitle(uniqueString)
            .Build();

        var fakePersonThree = new FakeTestingPersonBuilder()
            .WithFirstName("Charlie")
            .WithLastName("Brown")
            .WithTitle("Designer")
            .Build();

        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo, fakePersonThree);

        var personIds = new[] { fakePersonOne.Id, fakePersonTwo.Id, fakePersonThree.Id };
        var input = $"""(FirstName, LastName, Title, FirstName, LastName) @=* "{uniqueString}" """;

        // Act
        var queryablePeople = testingServiceScope.DbContext().People
            .Where(p => personIds.Contains(p.Id));
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(fakePersonTwo.Id);
    }

    [Fact]
    public async Task can_filter_with_property_list_case_sensitive_contains()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var uniqueString = "TestString";

        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithFirstName(uniqueString)
            .WithLastName("Smith")
            .Build();

        var fakePersonTwo = new FakeTestingPersonBuilder()
            .WithFirstName("teststring") // Different case
            .WithLastName("Johnson")
            .Build();

        var fakePersonThree = new FakeTestingPersonBuilder()
            .WithFirstName("Other")
            .WithLastName("Brown")
            .Build();

        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo, fakePersonThree);

        var personIds = new[] { fakePersonOne.Id, fakePersonTwo.Id, fakePersonThree.Id };
        var input = $"""(FirstName, LastName) @= "{uniqueString}" """; // Case sensitive

        // Act
        var queryablePeople = testingServiceScope.DbContext().People
            .Where(p => personIds.Contains(p.Id));
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert - Should only match exact case
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(fakePersonOne.Id);
    }

    [Fact]
    public async Task can_filter_with_property_list_whitespace_in_list()
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
        // Extra whitespace around commas and properties
        var input = $"""(  FirstName  ,  LastName  ) @=* "{uniqueString}" """;

        // Act
        var queryablePeople = testingServiceScope.DbContext().People
            .Where(p => personIds.Contains(p.Id));
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(fakePersonOne.Id);
    }

    [Fact]
    public async Task can_filter_with_property_list_not_equals()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var uniqueString = Guid.NewGuid().ToString();

        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithFirstName(uniqueString)
            .WithLastName(uniqueString)
            .Build();

        var fakePersonTwo = new FakeTestingPersonBuilder()
            .WithFirstName("John")
            .WithLastName("Doe")
            .Build();

        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo);

        var personIds = new[] { fakePersonOne.Id, fakePersonTwo.Id };
        var input = $"""(FirstName, LastName) != "{uniqueString}" """;

        // Act
        var queryablePeople = testingServiceScope.DbContext().People
            .Where(p => personIds.Contains(p.Id));
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert - Should match where both FirstName != uniqueString AND LastName != uniqueString
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(fakePersonTwo.Id);
    }

    [Fact]
    public async Task can_filter_with_property_list_not_starts_with()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var uniquePrefix = Guid.NewGuid().ToString().Substring(0, 8);

        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithFirstName($"{uniquePrefix}Alice")
            .WithLastName($"{uniquePrefix}Smith")
            .Build();

        var fakePersonTwo = new FakeTestingPersonBuilder()
            .WithFirstName("Bob")
            .WithLastName("Johnson")
            .Build();

        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo);

        var personIds = new[] { fakePersonOne.Id, fakePersonTwo.Id };
        var input = $"""(FirstName, LastName) !_=* "{uniquePrefix}" """;

        // Act
        var queryablePeople = testingServiceScope.DbContext().People
            .Where(p => personIds.Contains(p.Id));
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert - Should match where FirstName doesn't start with prefix AND LastName doesn't start with prefix
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(fakePersonTwo.Id);
    }

    [Fact]
    public async Task can_filter_with_property_list_not_ends_with()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var uniqueSuffix = Guid.NewGuid().ToString().Substring(0, 8);

        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithFirstName($"Alice{uniqueSuffix}")
            .WithLastName($"Smith{uniqueSuffix}")
            .Build();

        var fakePersonTwo = new FakeTestingPersonBuilder()
            .WithFirstName("Bob")
            .WithLastName("Johnson")
            .Build();

        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo);

        var personIds = new[] { fakePersonOne.Id, fakePersonTwo.Id };
        var input = $"""(FirstName, LastName) !_-=* "{uniqueSuffix}" """;

        // Act
        var queryablePeople = testingServiceScope.DbContext().People
            .Where(p => personIds.Contains(p.Id));
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(fakePersonTwo.Id);
    }

    [Fact]
    public async Task can_filter_with_property_list_multiple_logical_conditions()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var uniqueString1 = Guid.NewGuid().ToString();
        var uniqueString2 = Guid.NewGuid().ToString();

        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithFirstName(uniqueString1)
            .WithLastName("Smith")
            .WithAge(30)
            .Build();

        var fakePersonTwo = new FakeTestingPersonBuilder()
            .WithFirstName("John")
            .WithLastName(uniqueString2)
            .WithAge(25)
            .Build();

        var fakePersonThree = new FakeTestingPersonBuilder()
            .WithFirstName("Jane")
            .WithLastName("Doe")
            .WithAge(35)
            .Build();

        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo, fakePersonThree);

        var personIds = new[] { fakePersonOne.Id, fakePersonTwo.Id, fakePersonThree.Id };
        var input = $"""((FirstName, LastName) @=* "{uniqueString1}" && Age >= 30) || ((FirstName, LastName) @=* "{uniqueString2}" && Age >= 25)""";

        // Act
        var queryablePeople = testingServiceScope.DbContext().People
            .Where(p => personIds.Contains(p.Id));
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(2);
        people.Should().Contain(p => p.Id == fakePersonOne.Id);
        people.Should().Contain(p => p.Id == fakePersonTwo.Id);
    }

    [Fact]
    public async Task can_filter_with_property_list_deeply_nested_logical_conditions()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var uniqueString = Guid.NewGuid().ToString();

        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithFirstName(uniqueString)
            .WithLastName("Smith")
            .WithAge(30)
            .WithRating(4.5m)
            .Build();

        var fakePersonTwo = new FakeTestingPersonBuilder()
            .WithFirstName("John")
            .WithLastName("Doe")
            .WithAge(25)
            .WithRating(5.0m)
            .Build();

        var fakePersonThree = new FakeTestingPersonBuilder()
            .WithFirstName("Jane")
            .WithLastName("Brown")
            .WithAge(35)
            .WithRating(3.0m)
            .Build();

        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo, fakePersonThree);

        var personIds = new[] { fakePersonOne.Id, fakePersonTwo.Id, fakePersonThree.Id };
        var input = $"""(((FirstName, LastName) @=* "{uniqueString}" || Age > 24) && Rating >= 4.5) || (Age == 35 && Rating < 4.0)""";

        // Act
        var queryablePeople = testingServiceScope.DbContext().People
            .Where(p => personIds.Contains(p.Id));
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(3);
        people.Should().Contain(p => p.Id == fakePersonOne.Id);
        people.Should().Contain(p => p.Id == fakePersonTwo.Id);
        people.Should().Contain(p => p.Id == fakePersonThree.Id);
    }

    [Fact]
    public async Task can_filter_with_property_list_mixed_with_regular_conditions()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var uniqueString = Guid.NewGuid().ToString();

        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithFirstName(uniqueString)
            .WithLastName("Smith")
            .WithAge(30)
            .WithTitle("Developer")
            .Build();

        var fakePersonTwo = new FakeTestingPersonBuilder()
            .WithFirstName("John")
            .WithLastName(uniqueString)
            .WithAge(25)
            .WithTitle("Designer")
            .Build();

        var fakePersonThree = new FakeTestingPersonBuilder()
            .WithFirstName("Jane")
            .WithLastName("Doe")
            .WithAge(30)
            .WithTitle("Developer")
            .Build();

        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo, fakePersonThree);

        var personIds = new[] { fakePersonOne.Id, fakePersonTwo.Id, fakePersonThree.Id };
        var input = $"""(FirstName, LastName) @=* "{uniqueString}" && Age == 30 && Title == "Developer" """;

        // Act
        var queryablePeople = testingServiceScope.DbContext().People
            .Where(p => personIds.Contains(p.Id));
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(fakePersonOne.Id);
    }

    [Fact]
    public async Task can_filter_with_property_list_multiple_property_lists_in_same_query()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var uniqueString1 = Guid.NewGuid().ToString();
        var uniqueString2 = Guid.NewGuid().ToString();

        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithFirstName(uniqueString1)
            .WithLastName("Smith")
            .WithTitle(uniqueString2)
            .Build();

        var fakePersonTwo = new FakeTestingPersonBuilder()
            .WithFirstName("John")
            .WithLastName("Doe")
            .WithTitle("Developer")
            .Build();

        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo);

        var personIds = new[] { fakePersonOne.Id, fakePersonTwo.Id };
        var input = $"""(FirstName, LastName) @=* "{uniqueString1}" && (Title, FirstName) @=* "{uniqueString2}" """;

        // Act
        var queryablePeople = testingServiceScope.DbContext().People
            .Where(p => personIds.Contains(p.Id));
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(fakePersonOne.Id);
    }

    [Fact]
    public async Task can_filter_with_property_list_nullable_properties()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();

        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithAge(30)
            .WithRating(4.5m)
            .Build();

        var fakePersonTwo = new FakeTestingPersonBuilder()
            .WithAge(null)
            .WithRating(5.0m)
            .Build();

        var fakePersonThree = new FakeTestingPersonBuilder()
            .WithAge(25)
            .WithRating(null)
            .Build();

        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo, fakePersonThree);

        var personIds = new[] { fakePersonOne.Id, fakePersonTwo.Id, fakePersonThree.Id };
        var input = """(Age, Rating) >= 30""";

        // Act
        var queryablePeople = testingServiceScope.DbContext().People
            .Where(p => personIds.Contains(p.Id));
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert - Should handle nulls gracefully
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(fakePersonOne.Id);
    }

    [Fact]
    public async Task can_filter_with_property_list_or_conditions_between_lists()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var uniqueString1 = Guid.NewGuid().ToString();
        var uniqueString2 = Guid.NewGuid().ToString();

        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithFirstName(uniqueString1)
            .WithLastName("Smith")
            .WithTitle("Developer")
            .Build();

        var fakePersonTwo = new FakeTestingPersonBuilder()
            .WithFirstName("John")
            .WithLastName("Doe")
            .WithTitle(uniqueString2)
            .Build();

        var fakePersonThree = new FakeTestingPersonBuilder()
            .WithFirstName("Jane")
            .WithLastName("Brown")
            .WithTitle("Designer")
            .Build();

        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo, fakePersonThree);

        var personIds = new[] { fakePersonOne.Id, fakePersonTwo.Id, fakePersonThree.Id };
        var input = $"""(FirstName, LastName) @=* "{uniqueString1}" || (Title, LastName) @=* "{uniqueString2}" """;

        // Act
        var queryablePeople = testingServiceScope.DbContext().People
            .Where(p => personIds.Contains(p.Id));
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(2);
        people.Should().Contain(p => p.Id == fakePersonOne.Id);
        people.Should().Contain(p => p.Id == fakePersonTwo.Id);
    }

    [Fact]
    public async Task can_filter_with_property_list_negated_group()
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

        var personIds = new[] { fakePersonOne.Id, fakePersonTwo.Id, fakePersonThree.Id };
        // Use AND with negation: !(matches in FirstName OR matches in LastName)
        var input = $"""Age > 20 && (FirstName, LastName) !@=* "{uniqueString}" """;

        // Act
        var queryablePeople = testingServiceScope.DbContext().People
            .Where(p => personIds.Contains(p.Id));
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert - Should only get person with no match in either field
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(fakePersonThree.Id);
    }

    [Fact]
    public async Task can_filter_with_property_list_boolean_properties()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();

        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithFavorite(true)
            .Build();

        var fakePersonTwo = new FakeTestingPersonBuilder()
            .WithFavorite(false)
            .Build();

        var fakePersonThree = new FakeTestingPersonBuilder()
            .WithFavorite(null)
            .Build();

        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo, fakePersonThree);

        var personIds = new[] { fakePersonOne.Id, fakePersonTwo.Id, fakePersonThree.Id };
        var input = """(Favorite, Favorite) == true""";

        // Act
        var queryablePeople = testingServiceScope.DbContext().People
            .Where(p => personIds.Contains(p.Id));
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(fakePersonOne.Id);
    }

    [Fact]
    public async Task can_filter_with_property_list_same_property_multiple_times()
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

        var personIds = new[] { fakePersonOne.Id, fakePersonTwo.Id };
        // Same property listed multiple times - should still work
        var input = $"""(FirstName, FirstName, FirstName) @=* "{uniqueString}" """;

        // Act
        var queryablePeople = testingServiceScope.DbContext().People
            .Where(p => personIds.Contains(p.Id));
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(fakePersonOne.Id);
    }

    [Fact]
    public async Task can_filter_with_property_list_very_complex_expression()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var uniqueString = Guid.NewGuid().ToString();

        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithFirstName(uniqueString)
            .WithLastName("Smith")
            .WithAge(30)
            .WithRating(4.5m)
            .WithTitle("Developer")
            .Build();

        var fakePersonTwo = new FakeTestingPersonBuilder()
            .WithFirstName("John")
            .WithLastName("Doe")
            .WithAge(25)
            .WithRating(5.0m)
            .WithTitle("Designer")
            .Build();

        var fakePersonThree = new FakeTestingPersonBuilder()
            .WithFirstName("Jane")
            .WithLastName("Brown")
            .WithAge(35)
            .WithRating(3.5m)
            .WithTitle("Manager")
            .Build();

        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo, fakePersonThree);

        var personIds = new[] { fakePersonOne.Id, fakePersonTwo.Id, fakePersonThree.Id };
        var input = $"""
            (((FirstName, LastName) @=* "{uniqueString}" && Age >= 30) ||
            (Rating >= 5.0 && Age < 30)) &&
            ((Title, FirstName) @=* "Developer" || Title @=* "Designer")
            """;

        // Act
        var queryablePeople = testingServiceScope.DbContext().People
            .Where(p => personIds.Contains(p.Id));
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(2);
        people.Should().Contain(p => p.Id == fakePersonOne.Id);
        people.Should().Contain(p => p.Id == fakePersonTwo.Id);
    }
}
