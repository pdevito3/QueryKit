namespace QueryKit.IntegrationTests.Tests;

using Bogus;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using QueryKit.Configuration;
using QueryKit.Exceptions;
using SharedTestingHelper.Fakes;
using WebApiTestProject.Entities;

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
            .WithBirthMonth(BirthMonthEnum.January)
            .Build();
        var fakePersonTwo = new FakeTestingPersonBuilder()
            .WithTitle("beta")
            .WithAge(100)
            .WithBirthMonth(BirthMonthEnum.February)
            .Build();
        var fakePersonThree = new FakeTestingPersonBuilder()
            .WithTitle("beta")
            .WithAge(50)
            .WithBirthMonth(BirthMonthEnum.March)
            .Build();
        var fakePersonFour = new FakeTestingPersonBuilder()
            .WithTitle("beta")
            .WithAge(20)
            .WithBirthMonth(BirthMonthEnum.April)
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
    public async Task can_sort_with_child_props_using_ownsone()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var faker = new Faker();
        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithPhysicalAddress(new Address(faker.Address.StreetAddress()
                , faker.Address.SecondaryAddress()
                , faker.Address.City()
                , "az"
                , faker.Address.ZipCode()
                , faker.Address.Country()))
            .Build();
        var fakePersonTwo = new FakeTestingPersonBuilder()
            .WithPhysicalAddress(new Address(faker.Address.StreetAddress()
                , faker.Address.SecondaryAddress()
                , faker.Address.City()
                , "zulu"
                , faker.Address.ZipCode()
                , faker.Address.Country()))
            .Build();
        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo);
        
        var input = $"PhysicalAddress.State desc";

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
    public async Task can_sort_with_child_props_using_hasconversion()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithEmail("alpha")
            .Build();
        var fakePersonTwo = new FakeTestingPersonBuilder()
            .WithEmail("bravo")
            .Build();
        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo);
        
        // must use email desc because the HasConversion is on the EmailAddress class
        var input = $"Email desc";

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
    public async Task a_prop_with_hasconversion_must_match_that_configuration()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var fakePersonOne = new FakeTestingPersonBuilder().Build();
        var fakePersonTwo = new FakeTestingPersonBuilder().Build();
        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo);
        
        // must use email desc because the HasConversion is on the EmailAddress class
        var input = $"Email.Value desc";

        // Act
        var queryablePeople = testingServiceScope.DbContext().People
            .Where(x => x.Id == fakePersonOne.Id || x.Id == fakePersonTwo.Id);
        var appliedQueryable = queryablePeople.ApplyQueryKitSort(input);
        var act = () => appliedQueryable.ToListAsync();

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
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
            .WithBirthMonth(BirthMonthEnum.January)
            .Build();
        var fakePersonTwo = new FakeTestingPersonBuilder()
            .WithTitle("beta")
            .WithAge(100)
            .WithBirthMonth(BirthMonthEnum.February)
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
            .WithBirthMonth(BirthMonthEnum.January)
            .Build();
        var fakePersonThree = new FakeTestingPersonBuilder()
            .WithTitle("beta")
            .WithAge(50)
            .WithBirthMonth(BirthMonthEnum.March)
            .Build();
        var fakePersonFour = new FakeTestingPersonBuilder()
            .WithTitle("beta")
            .WithAge(20)
            .WithBirthMonth(BirthMonthEnum.February)
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

    [Fact]
    public async Task can_sort_by_derived_property_with_navigation_property_like_scenario()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var uniqueId = Guid.NewGuid().ToString()[..8];
        
        // Create people with different name patterns to test sorting
        var personA = new FakeTestingPersonBuilder()
            .WithFirstName($"Alice_{uniqueId}")
            .WithLastName($"Anderson_{uniqueId}")
            .WithTitle($"SortTest_{uniqueId}")
            .Build();
            
        var personZ = new FakeTestingPersonBuilder()
            .WithFirstName($"Zoe_{uniqueId}")
            .WithLastName($"Wilson_{uniqueId}")
            .WithTitle($"SortTest_{uniqueId}")
            .Build();
            
        // Create person without last name (should sort last with null handling)
        var personPartial = new FakeTestingPersonBuilder()
            .WithFirstName($"Bob_{uniqueId}")
            .WithLastName(null)
            .WithTitle($"SortTest_{uniqueId}")
            .Build();
        
        await testingServiceScope.InsertAsync(personZ, personA, personPartial);

        // Test sorting by derived property that mimics navigation property patterns
        // This mimics: x.Patient != null ? x.Patient.FirstName + " " + x.Patient.LastName : null
        var config = new QueryKitConfiguration(config =>
        {
            config.DerivedProperty<TestingPerson>(x => 
                x.LastName != null 
                    ? x.FirstName + " " + x.LastName 
                    : "ZZZ_" + (x.FirstName ?? "Unknown")) // Sort nulls last
                .HasQueryName("fullName");
        });
        
        // Act - This should not cause Entity Framework translation errors
        var queryablePeople = testingServiceScope.DbContext().People
            .Where(p => p.Title == $"SortTest_{uniqueId}");
            
        var appliedQueryable = queryablePeople.ApplyQueryKitSort("fullName asc", config);
        var people = await appliedQueryable.ToListAsync();
        
        // Assert - Should be sorted by derived full name
        people.Count.Should().Be(3);
        people[0].FirstName.Should().Be($"Alice_{uniqueId}"); // "Alice Anderson" - first alphabetically
        people[1].FirstName.Should().Be($"Zoe_{uniqueId}"); // "Zoe Wilson" - second alphabetically  
        people[2].FirstName.Should().Be($"Bob_{uniqueId}"); // "ZZZ_Bob" - should be last
    }

    [Fact]
    public async Task can_sort_by_derived_property_using_complex_patient_like_scenario()
    {
        // Arrange - This test mimics the exact scenario from the user's error
        var testingServiceScope = new TestingServiceScope();
        var uniqueId = Guid.NewGuid().ToString()[..8];
        
        // Create people that represent "patients" with null safety checks
        var personWithFullName = new FakeTestingPersonBuilder()
            .WithFirstName($"John_{uniqueId}")
            .WithLastName($"Doe_{uniqueId}")
            .WithTitle($"Patient1_{uniqueId}")
            .Build();
            
        var personWithPartialName = new FakeTestingPersonBuilder()
            .WithFirstName($"Jane_{uniqueId}")
            .WithLastName(null) // This represents cases where Patient.LastName might be null
            .WithTitle($"Patient2_{uniqueId}")
            .Build();
            
        var personWithoutName = new FakeTestingPersonBuilder()
            .WithFirstName(null)
            .WithLastName(null)
            .WithTitle($"Patient3_{uniqueId}")
            .Build();
            
        await testingServiceScope.InsertAsync(personWithoutName, personWithPartialName, personWithFullName);

        // Test the EXACT pattern from user's error - derived property with != null checks for sorting
        // This exactly matches: x.Patient != null ? x.Patient.FirstName + " " + x.Patient.LastName : null
        var config = new QueryKitConfiguration(config =>
        {
            config.DerivedProperty<TestingPerson>(x => 
                x.FirstName != null && x.LastName != null
                    ? x.FirstName + " " + x.LastName 
                    : x.FirstName ?? "ZZZ_Unknown")
                .HasQueryName("patient");
        });
        
        // Act - Sort by the derived property (this previously caused Entity Framework translation errors)
        var queryablePeople = testingServiceScope.DbContext().People
            .Where(p => p.Title.Contains($"Patient") && p.Title.Contains(uniqueId));
            
        // This should NOT generate LeftJoin with `.OrderBy(a => (object)a.Inner)' error
        var appliedQueryable = queryablePeople.ApplyQueryKitSort("patient asc", config);
        var people = await appliedQueryable.ToListAsync();
        
        // Assert - Should be sorted by computed patient name
        people.Count.Should().Be(3);
        
        // First: Jane (partial name = "Jane_uniqueId")
        people[0].FirstName.Should().Be($"Jane_{uniqueId}");
        people[0].LastName.Should().BeNull();
        
        // Second: John (full name = "John_uniqueId Doe_uniqueId") 
        people[1].FirstName.Should().Be($"John_{uniqueId}");
        people[1].LastName.Should().Be($"Doe_{uniqueId}");
        
        // Third: null person (unknown name = "ZZZ_Unknown")
        people[2].FirstName.Should().BeNull();
        people[2].LastName.Should().BeNull();
    }
}
