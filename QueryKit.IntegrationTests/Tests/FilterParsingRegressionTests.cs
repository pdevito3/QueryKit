namespace QueryKit.IntegrationTests.Tests;

using System.Globalization;
using Configuration;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SharedTestingHelper.Fakes;
using WebApiTestProject.Entities;

public class FilterParsingRegressionTests : TestBase
{
    [Fact]
    public async Task operator_alias_text_inside_quoted_value_is_kept()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var title = $"salt and pepper or eq {Guid.NewGuid()}";
        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithTitle(title)
            .Build();
        var fakePersonTwo = new FakeTestingPersonBuilder().Build();
        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo);

        var input = $"""{nameof(TestingPerson.Title)} eq "{title}" and {nameof(TestingPerson.Id)} neq "{Guid.NewGuid()}" """;
        var config = new QueryKitConfiguration(settings =>
        {
            settings.EqualsOperator = "eq";
            settings.NotEqualsOperator = "neq";
            settings.AndOperator = "and";
            settings.OrOperator = "or";
        });

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var appliedQueryable = queryablePeople.ApplyQueryKitFilter(input, config);
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(fakePersonOne.Id);
    }

    [Fact]
    public async Task decimal_value_uses_invariant_culture()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var title = $"culture {Guid.NewGuid()}";
        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithTitle(title)
            .WithRating(4.6m)
            .Build();
        var fakePersonTwo = new FakeTestingPersonBuilder()
            .WithTitle(title)
            .WithRating(4.4m)
            .Build();
        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo);

        var input = $"""{nameof(TestingPerson.Title)} == "{title}" && {nameof(TestingPerson.Rating)} > 4.5""";

        // Act
        var originalCulture = CultureInfo.CurrentCulture;
        IQueryable<TestingPerson> appliedQueryable;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            appliedQueryable = testingServiceScope.DbContext().People.ApplyQueryKitFilter(input);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
        var people = await appliedQueryable.ToListAsync();

        // Assert
        people.Count.Should().Be(1);
        people[0].Id.Should().Be(fakePersonOne.Id);
    }

    [Fact]
    public async Task list_value_with_comma_is_one_item()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var suffix = Guid.NewGuid().ToString();
        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithTitle($"Warm, with syrup {suffix}")
            .WithFirstName(suffix)
            .Build();
        var fakePersonTwo = new FakeTestingPersonBuilder()
            .WithTitle("Warm")
            .WithFirstName(suffix)
            .Build();
        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo);

        var inInput = $"""{nameof(TestingPerson.FirstName)} == "{suffix}" && {nameof(TestingPerson.Title)} ^^ ["Warm, with syrup {suffix}"]""";
        var notInInput = $"""{nameof(TestingPerson.FirstName)} == "{suffix}" && {nameof(TestingPerson.Title)} !^^ ["Warm, with syrup {suffix}"]""";

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var inPeople = await queryablePeople.ApplyQueryKitFilter(inInput).ToListAsync();
        var notInPeople = await queryablePeople.ApplyQueryKitFilter(notInInput).ToListAsync();

        // Assert
        inPeople.Select(x => x.Id).Should().Equal(fakePersonOne.Id);
        notInPeople.Select(x => x.Id).Should().Equal(fakePersonTwo.Id);
    }

    [Theory]
    [InlineData("SpecificDate == 2024-01-15T10:00:00+02:00")]
    [InlineData("SpecificDate ^^ [2024-01-15T10:00:00+02:00]")]
    public async Task date_time_offset_value_with_offset_matches_same_instant(string valueFilter)
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var title = $"offset {Guid.NewGuid()}";
        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithTitle(title)
            .WithSpecificDate(new DateTimeOffset(2024, 1, 15, 8, 0, 0, TimeSpan.Zero))
            .Build();
        var fakePersonTwo = new FakeTestingPersonBuilder()
            .WithTitle(title)
            .WithSpecificDate(new DateTimeOffset(2024, 1, 15, 10, 0, 0, TimeSpan.Zero))
            .Build();
        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo);

        var input = $"""{nameof(TestingPerson.Title)} == "{title}" && {valueFilter}""";

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var people = await queryablePeople.ApplyQueryKitFilter(input).ToListAsync();

        // Assert
        people.Select(x => x.Id).Should().Equal(fakePersonOne.Id);
    }

    [Theory]
    [InlineData("SpecificDateTime == 2024-01-15T08:00:00.500Z")]
    [InlineData("SpecificDate == 2024-01-15T10:00:00.5+02:00")]
    [InlineData("Time == 08:30:00.5")]
    [InlineData("Time == \"08:30:00.5\"")]
    public async Task fractional_seconds_are_kept(string valueFilter)
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var title = $"fraction {Guid.NewGuid()}";
        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithTitle(title)
            .WithSpecificDateTime(new DateTime(2024, 1, 15, 8, 0, 0, 500, DateTimeKind.Utc))
            .WithSpecificDate(new DateTimeOffset(2024, 1, 15, 8, 0, 0, 500, TimeSpan.Zero))
            .WithTime(new TimeOnly(8, 30, 0, 500))
            .Build();
        var fakePersonTwo = new FakeTestingPersonBuilder()
            .WithTitle(title)
            .WithSpecificDateTime(new DateTime(2024, 1, 15, 8, 0, 0, DateTimeKind.Utc))
            .WithSpecificDate(new DateTimeOffset(2024, 1, 15, 8, 0, 0, TimeSpan.Zero))
            .WithTime(new TimeOnly(8, 30, 0))
            .Build();
        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo);

        var input = $"""{nameof(TestingPerson.Title)} == "{title}" && {valueFilter}""";

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var people = await queryablePeople.ApplyQueryKitFilter(input).ToListAsync();

        // Assert
        people.Select(x => x.Id).Should().Equal(fakePersonOne.Id);
    }

    [Theory]
    [InlineData("SpecificDateTime == 2024-01-15T08:00:00")]
    [InlineData("SpecificDateTime == 2024-01-15T10:00:00+02:00")]
    [InlineData("SpecificDateTime ^^ [2024-01-15T08:00:00]")]
    [InlineData("SpecificDateTime ^^ [2024-01-15T10:00:00+02:00]")]
    [InlineData("SpecificDate == 2024-01-15T08:00:00")]
    [InlineData("SpecificDate == 2024-01-15T10:00:00+02:00")]
    [InlineData("SpecificDate ^^ [2024-01-15T08:00:00]")]
    [InlineData("SpecificDate ^^ [2024-01-15T10:00:00+02:00]")]
    public async Task date_time_without_offset_is_utc(string valueFilter)
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var title = $"utc {Guid.NewGuid()}";
        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithTitle(title)
            .WithSpecificDateTime(new DateTime(2024, 1, 15, 8, 0, 0, DateTimeKind.Utc))
            .WithSpecificDate(new DateTimeOffset(2024, 1, 15, 8, 0, 0, TimeSpan.Zero))
            .Build();
        var fakePersonTwo = new FakeTestingPersonBuilder()
            .WithTitle(title)
            .WithSpecificDateTime(new DateTime(2024, 1, 15, 9, 0, 0, DateTimeKind.Utc))
            .WithSpecificDate(new DateTimeOffset(2024, 1, 15, 9, 0, 0, TimeSpan.Zero))
            .Build();
        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo);

        var input = $"""{nameof(TestingPerson.Title)} == "{title}" && {valueFilter}""";

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var people = await queryablePeople.ApplyQueryKitFilter(input).ToListAsync();

        // Assert
        people.Select(x => x.Id).Should().Equal(fakePersonOne.Id);
    }

    [Theory]
    [InlineData("Age == Rating", true)]
    [InlineData("Rating == Age", true)]
    [InlineData("Age != Rating", false)]
    [InlineData("Rating != Age", false)]
    public async Task int_property_compares_to_decimal_property(string valueFilter, bool expectEqualPerson)
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var title = $"numeric {Guid.NewGuid()}";
        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithTitle(title)
            .WithAge(4)
            .WithRating(4m)
            .Build();
        var fakePersonTwo = new FakeTestingPersonBuilder()
            .WithTitle(title)
            .WithAge(4)
            .WithRating(4.5m)
            .Build();
        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo);

        var input = $"""{nameof(TestingPerson.Title)} == "{title}" && {valueFilter}""";

        // Act
        var queryablePeople = testingServiceScope.DbContext().People;
        var people = await queryablePeople.ApplyQueryKitFilter(input).ToListAsync();

        // Assert
        people.Select(x => x.Id).Should().Equal(expectEqualPerson ? fakePersonOne.Id : fakePersonTwo.Id);
    }
}
