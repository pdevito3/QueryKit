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
    public async Task can_filter_by_email_with_query_name_and_has_conversion()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var testEmail = $"{Guid.NewGuid()}@example.com";
        var person = new FakeTestingPersonBuilder()
            .WithEmail(testEmail)
            .Build();
        var personTwo = new FakeTestingPersonBuilder().Build();

        await testingServiceScope.InsertAsync(person, personTwo);

        var input = $"""mail == "{testEmail}" """;
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<TestingPerson>(x => x.Email).HasQueryName("mail").HasConversion<string>();
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
    public async Task can_filter_by_email_property_path_when_query_name_and_has_conversion_are_configured()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var testEmail = $"{Guid.NewGuid()}@example.com";
        var person = new FakeTestingPersonBuilder()
            .WithEmail(testEmail)
            .Build();
        var personTwo = new FakeTestingPersonBuilder().Build();

        await testingServiceScope.InsertAsync(person, personTwo);

        var input = $"""Email == "{testEmail}" """;
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<TestingPerson>(x => x.Email).HasQueryName("mail").HasConversion<string>();
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
    public async Task can_filter_by_email_value_with_query_name_and_has_conversion()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var testEmail = $"{Guid.NewGuid()}@example.com";
        var person = new FakeTestingPersonBuilder()
            .WithEmail(testEmail)
            .Build();
        var personTwo = new FakeTestingPersonBuilder().Build();

        await testingServiceScope.InsertAsync(person, personTwo);

        var input = $"""Email.Value == "{testEmail}" """;
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<TestingPerson>(x => x.Email).HasQueryName("mail").HasConversion<string>();
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
    public async Task can_filter_by_null_email_with_query_name_and_has_conversion()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var title = Guid.NewGuid().ToString();
        var person = new FakeTestingPersonBuilder()
            .WithTitle(title)
            .Build();
        person.Email = null!;
        var personTwo = new FakeTestingPersonBuilder()
            .WithTitle(title)
            .Build();

        await testingServiceScope.InsertAsync(person, personTwo);

        var input = """mail == null""";
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<TestingPerson>(x => x.Email).HasQueryName("mail").HasConversion<string>();
        });

        // Act
        var queryablePeople = testingServiceScope.DbContext().People
            .Where(x => x.Title == title);
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input, config);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(person.Id);
    }

    [Fact]
    public async Task can_filter_by_nullable_int_with_query_name_and_has_conversion()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var title = Guid.NewGuid().ToString();
        var person = new FakeTestingPersonBuilder()
            .WithTitle(title)
            .WithAge(41)
            .Build();
        var personTwo = new FakeTestingPersonBuilder()
            .WithTitle(title)
            .WithAge(42)
            .Build();
        await testingServiceScope.InsertAsync(person, personTwo);

        var input = $"""Title == "{title}" && years == 41""";
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<TestingPerson>(x => x.Age).HasQueryName("years").HasConversion<string>();
        });

        // Act
        var people = await testingServiceScope.DbContext().People
            .ApplyQueryKitFilter(input, config)
            .ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(person.Id);
    }

    [Fact]
    public async Task can_filter_by_enum_with_query_name_and_has_conversion()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var title = Guid.NewGuid().ToString();
        var person = new FakeTestingPersonBuilder()
            .WithTitle(title)
            .WithBirthMonth(BirthMonthEnum.March)
            .Build();
        var personTwo = new FakeTestingPersonBuilder()
            .WithTitle(title)
            .WithBirthMonth(BirthMonthEnum.April)
            .Build();
        await testingServiceScope.InsertAsync(person, personTwo);

        var input = $"""Title == "{title}" && month == March""";
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<TestingPerson>(x => x.BirthMonth).HasQueryName("month").HasConversion<string>();
        });

        // Act
        var people = await testingServiceScope.DbContext().People
            .ApplyQueryKitFilter(input, config)
            .ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(person.Id);
    }

    [Fact]
    public async Task can_filter_by_guid_with_query_name_and_has_conversion()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var person = new FakeTestingPersonBuilder().Build();
        var personTwo = new FakeTestingPersonBuilder().Build();
        await testingServiceScope.InsertAsync(person, personTwo);

        var input = $"""identifier == "{person.Id}" """;
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<TestingPerson>(x => x.Id).HasQueryName("identifier").HasConversion<string>();
        });

        // Act
        var people = await testingServiceScope.DbContext().People
            .ApplyQueryKitFilter(input, config)
            .ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(person.Id);
    }

    [Fact]
    public async Task can_filter_by_null_email_with_has_conversion()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var title = Guid.NewGuid().ToString();
        var person = new FakeTestingPersonBuilder()
            .WithTitle(title)
            .Build();
        person.Email = null!;
        var personTwo = new FakeTestingPersonBuilder()
            .WithTitle(title)
            .Build();

        await testingServiceScope.InsertAsync(person, personTwo);

        var input = """Email == null""";
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<TestingPerson>(x => x.Email).HasConversion<string>();
        });

        // Act
        var queryablePeople = testingServiceScope.DbContext().People
            .Where(x => x.Title == title);
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
    public async Task can_filter_by_nested_postal_code_with_query_name_and_has_conversion()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var postalCode = Guid.NewGuid().ToString("N")[..10];
        var person = new FakeTestingPersonBuilder()
            .WithPhysicalAddress(new Address("Line1", "Line2", "City", "State", postalCode, "Country"))
            .Build();
        var personTwo = new FakeTestingPersonBuilder().Build();

        await testingServiceScope.InsertAsync(person, personTwo);

        var input = $"""zip == "{postalCode}" """;
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<TestingPerson>(x => x.PhysicalAddress.PostalCode).HasQueryName("zip").HasConversion<string>();
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

    [Fact]
    public async Task can_filter_guid_with_contains_and_has_conversion()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var person = new FakeTestingPersonBuilder().Build();
        var personTwo = new FakeTestingPersonBuilder().Build();

        await testingServiceScope.InsertAsync(person, personTwo);

        var input = $"""Id @= "{person.Id.ToString()[..13]}" """;
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<TestingPerson>(x => x.Id).HasConversion<string>();
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