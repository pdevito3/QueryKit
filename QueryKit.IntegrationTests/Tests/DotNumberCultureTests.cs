namespace QueryKit.IntegrationTests.Tests;

using System.Globalization;
using Exceptions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SharedTestingHelper.Fakes;
using WebApiTestProject.Entities;

// Like v1.14.2, a '.' number on an integer property throws ParsingException in a culture
// whose decimal separator is not '.', and a '.' number on a decimal property still filters.
public class DotNumberCultureTests : TestBase
{
    [Fact]
    public async Task dot_number_on_an_integer_property_throws_parsing_exception_in_de_de()
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var title = $"culture {Guid.NewGuid()}";
        var fakePersonOne = new FakeTestingPersonBuilder()
            .WithTitle(title)
            .WithAge(5)
            .WithRating(4.6M)
            .Build();
        var fakePersonTwo = new FakeTestingPersonBuilder()
            .WithTitle(title)
            .WithAge(3)
            .WithRating(4.4M)
            .Build();
        await testingServiceScope.InsertAsync(fakePersonOne, fakePersonTwo);

        // Act
        var originalCulture = CultureInfo.CurrentCulture;
        IQueryable<TestingPerson> appliedQueryable;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            var integerFilter = () => testingServiceScope.DbContext().People
                .ApplyQueryKitFilter($"""Title == "{title}" && Age > 4.4""");
            integerFilter.Should().ThrowExactly<ParsingException>();

            appliedQueryable = testingServiceScope.DbContext().People
                .ApplyQueryKitFilter($"""Title == "{title}" && Rating > 4.5""");
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
}
