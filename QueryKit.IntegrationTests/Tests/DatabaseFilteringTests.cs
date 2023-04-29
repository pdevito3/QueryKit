namespace QueryKit.IntegrationTests.Tests;

using System.Linq.Expressions;
using Bogus;
using Fakes;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
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
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

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
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

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
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(0);
    }
    
    // var people = testingServiceScope.DbContext().People
    //     .Where(x => x.Email == fakePersonOne.Email)
    //     .OrderBy(x => x.Email)
    //     .ToList();
    // TODO needs to have `Email` not `Email.Value` if using `HasConversion`
    [Fact(Skip = "Will need something like this if i want to support HasConversion in efcore.")]
    public async Task can_filter_with_child_props()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var faker = new Faker();
        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithEmail(faker.Internet.Email())
            .Build();
        var fakePersonTwo = new FakeTestingPersonBuilder()
            .Build();
        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo);
        
        var input = $"""email == "{fakePersonOne.Email.Value}" """;

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<TestingPerson>(x => x.Email.Value).HasQueryName("email");
        });
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input, config);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(fakePersonOne.Id);
    }
    
    [Fact]
    public async Task can_filter_nested_property_using_ownsone()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var faker = new Faker();
        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithPhysicalAddress(new Address(faker.Address.StreetAddress()
                , faker.Address.SecondaryAddress()
                , faker.Address.City()
                , faker.Address.State()
                , faker.Address.ZipCode()
                , faker.Address.Country()))
            .Build();
        await testingServiceScope.InsertAsync(fakePersonOne);
        var input = $"""PhysicalAddress.State == "{fakePersonOne.PhysicalAddress.State}" """;

        // Act
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<TestingPerson>(x => x.PhysicalAddress.State); //.HasQueryName("PhysicalAddressPostalCode");
        });
        var people = testingServiceScope.DbContext().People.ApplyQueryKitFilter(input, config)
            // .Where(x => x.PhysicalAddress.State == fakePersonOne.PhysicalAddress.State
            // && x.PhysicalAddress.PostalCode == fakePersonOne.PhysicalAddress.PostalCode)
            // .OrderBy(x => x.Email.Value)
            .ToList();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(fakePersonOne.Id);
    }
    
    [Fact]
    public async Task can_filter_by_decimal()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var faker = new Faker();
        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithRating(4M)
            .Build();
        var fakePersonTwo = new FakeTestingPersonBuilder()
            .WithRating(2M)
            .Build();
        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo);

        var input = $"""{nameof(TestingPerson.Rating)} > 3.5""";

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count(x => x.Id == fakePersonOne.Id).Should().Be(1);
    }
    
    [Fact]
    public async Task can_filter_complex_expression()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var faker = new Faker();
        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithTitle("Waffle & Chicken")
            .WithAge(35)
            .WithBirthMonth("January")
            .WithRating(4.0M)
            .WithSpecificDate(new DateTime(2022, 07, 01, 00, 00, 03, DateTimeKind.Utc))
            .WithDate(DateOnly.FromDateTime(new DateTime(2022, 07, 01)))
            .Build();
        var fakePersonTwo = new FakeTestingPersonBuilder()
            .WithTitle("Lamb")
            .WithAge(17)
            .WithBirthMonth("February")
            .WithRating(3.4M)
            .WithSpecificDate(new DateTime(2022, 07, 01, 00, 00, 03, DateTimeKind.Utc))
            .Build();
        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo);
        
        var input = $"""""((Title @=* "waffle & chicken" && Age > 30) || Id == "{fakePersonOne.Id}" || Title == "lamb" || Title == null) && (Age < 18 || (BirthMonth == "January" && Title _= "ally")) || Rating > 3.5 || SpecificDate == 2022-07-01T00:00:03Z && (Date == 2022-07-01 || Time == 00:00:03)""""";

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(fakePersonOne.Id);
    }

    [Fact]
    public async Task can_filter_by_string_with_special_characters()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithTitle("""lamb is great on a "gee-ro" not a "gy-ro" sandwich""")
            .WithAge(10)
            .Build();
        var fakePersonTwo = new FakeTestingPersonBuilder().Build();
        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo);

        var input = $"""""{nameof(TestingPerson.Title)} == """{fakePersonOne.Title}""" """"";

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();
        
        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(fakePersonOne.Id);
        people[0].Title.Should().Be(fakePersonOne.Title);
    }

    [Fact]
    public async Task can_handle_in_for_int()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithAge(22)
            .Build();
        var fakePersonTwo = new FakeTestingPersonBuilder()
            .WithAge(60)
            .Build();
        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo);

        var input = """Age ^^ [22, 30, 40]""";

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();
        
        // Assert
        people.Count.Should().BeGreaterOrEqualTo(1);
        people.FirstOrDefault(x => x.Id == fakePersonOne.Id).Should().NotBeNull();
        people.FirstOrDefault(x => x.Id == fakePersonTwo.Id).Should().BeNull();
    }

    [Fact]
    public async Task can_handle_in_for_guid()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithAge(22)
            .Build();
        var fakePersonTwo = new FakeTestingPersonBuilder()
            .WithAge(60)
            .Build();
        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo);

        var input = $"""Id ^^ ["{fakePersonOne.Id.ToString()}"]""";

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();
        
        // Assert
        people.Count.Should().BeGreaterOrEqualTo(1);
        people.FirstOrDefault(x => x.Id == fakePersonOne.Id).Should().NotBeNull();
        people.FirstOrDefault(x => x.Id == fakePersonTwo.Id).Should().BeNull();
    }

    [Fact]
    public async Task can_handle_in_for_string()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var fakePersonOne = new FakeTestingPersonBuilder().Build();
        var fakePersonTwo = new FakeTestingPersonBuilder().Build();
        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo);

        var input = $"""Title ^^ ["{fakePersonOne.Title}"]""";

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();
        
        // Assert
        people.Count.Should().BeGreaterOrEqualTo(1);
        people.FirstOrDefault(x => x.Id == fakePersonOne.Id).Should().NotBeNull();
        people.FirstOrDefault(x => x.Id == fakePersonTwo.Id).Should().BeNull();
    }

    [Fact]
    public async Task can_handle_case_insensitive_in_for_string()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var fakePersonOne = new FakeTestingPersonBuilder().Build();
        var fakePersonTwo = new FakeTestingPersonBuilder().Build();
        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo);

        var input = $"""Title ^^* ["{fakePersonOne.Title.ToUpper()}"]""";

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input);
        var people = await appliedQueryable.ToListAsync();
        
        // Assert
        people.Count.Should().BeGreaterOrEqualTo(1);
        people.FirstOrDefault(x => x.Id == fakePersonOne.Id).Should().NotBeNull();
        people.FirstOrDefault(x => x.Id == fakePersonTwo.Id).Should().BeNull();
    }
}