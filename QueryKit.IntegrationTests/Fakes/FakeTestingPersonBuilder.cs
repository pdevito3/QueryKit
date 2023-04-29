namespace QueryKit.IntegrationTests.Fakes;

using AutoBogus;
using WebApiTestProject.Entities;

public class FakeTestingPersonBuilder
{
    private readonly TestingPerson _baseTestingPerson = new AutoFaker<TestingPerson>()
        .RuleFor(x => x.Title, faker => faker.Lorem.Sentence())
        .Generate();
    
    public FakeTestingPersonBuilder WithTitle(string title)
    {
        _baseTestingPerson.Title = title;
        return this;
    }

    public FakeTestingPersonBuilder WithAge(int age)
    {
        _baseTestingPerson.Age = age;
        return this;
    }

    public FakeTestingPersonBuilder WithEmail(string email)
    {
        _baseTestingPerson.Email = new EmailAddress(email);
        return this;
    }

    public FakeTestingPersonBuilder WithBirthMonth(string value)
    {
        _baseTestingPerson.BirthMonth = value;
        return this;
    }
    
    public FakeTestingPersonBuilder WithPhysicalAddress(Address address)
    {
        _baseTestingPerson.PhysicalAddress = address;
        return this;
    }

    public FakeTestingPersonBuilder WithId(Guid newGuid)
    {
        _baseTestingPerson.Id = newGuid;
        return this;
    }

    public FakeTestingPersonBuilder WithRating(decimal rating)
    {
        _baseTestingPerson.Rating = rating;
        return this;
    }

    public FakeTestingPersonBuilder WithSpecificDate(DateTimeOffset? dateTime)
    {
        _baseTestingPerson.SpecificDate = dateTime;
        return this;
    }

    public FakeTestingPersonBuilder WithDate(DateOnly? dateTime)
    {
        _baseTestingPerson.Date = dateTime;
        return this;
    }

    public TestingPerson Build() => _baseTestingPerson;
}