namespace QueryKit.UnitTests;

using FluentAssertions;
using QueryKit.WebApiTestProject.Entities;
using SharedTestingHelper.Fakes;

public class EnumerableSortingTests
{
    [Fact]
    public async Task can_sort_items_with_mixed_order_directions()
    {
        // Arrange
        var personOne = new FakeTestingPersonBuilder()
            .WithTitle("alpha")
            .WithAge(10)
            .WithBirthMonth(BirthMonthEnum.January)
            .Build();
        var personTwo = new FakeTestingPersonBuilder()
            .WithTitle("beta")
            .WithAge(100)
            .WithBirthMonth(BirthMonthEnum.February)
            .Build();
        var personThree = new FakeTestingPersonBuilder()
            .WithTitle("beta")
            .WithAge(50)
            .WithBirthMonth(BirthMonthEnum.March)
            .Build();
        var personFour = new FakeTestingPersonBuilder()
            .WithTitle("beta")
            .WithAge(20)
            .WithBirthMonth(BirthMonthEnum.April)
            .Build();
        List<TestingPerson> peopleList = [personOne, personTwo, personThree, personFour];

        var input = $"Title desc, Age asc, BirthMonth desc";

        // Act
        var appliedQueryable = peopleList.ApplyQueryKitSort(input);
        var people = appliedQueryable.ToList();

        // Assert
        people.Count.Should().Be(4);
        people[0].Id.Should().Be(personFour.Id);
        people[1].Id.Should().Be(personThree.Id);
        people[2].Id.Should().Be(personTwo.Id);
        people[3].Id.Should().Be(personOne.Id);
    }
}
