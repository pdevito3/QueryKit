namespace QueryKit.IntegrationTests.Tests;

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
        var config = new QueryKitProcessorConfiguration(config =>
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
        var config = new QueryKitProcessorConfiguration(config =>
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
}