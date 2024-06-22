namespace SharedTestingHelper.Fakes;

using AutoBogus;
using QueryKit.WebApiTestProject.Entities;

public class FakeTestingPersonBuilder
{
    private readonly TestingPerson _baseTestingPerson = new AutoFaker<TestingPerson>()
        .RuleFor(x => x.FirstName, faker => faker.Name.FirstName())
        .RuleFor(x => x.LastName, faker => faker.Name.LastName())
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

    public FakeTestingPersonBuilder WithFavorite(bool favorite)
    {
        _baseTestingPerson.Favorite = favorite;
        return this;
    }

    public FakeTestingPersonBuilder WithEmail(string email)
    {
        _baseTestingPerson.Email = new EmailAddress(email);
        return this;
    }

    public FakeTestingPersonBuilder WithBirthMonth(BirthMonthEnum? value)
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
    
    public FakeTestingPersonBuilder WithSpecificDateTime(DateTime dateTime)
    {
        _baseTestingPerson.SpecificDateTime = dateTime;
        return this;
    }

    public FakeTestingPersonBuilder WithDate(DateOnly? date)
    {
        _baseTestingPerson.Date = date;
        return this;
    }
    
    public FakeTestingPersonBuilder WithTime(TimeOnly? time)
    {
        _baseTestingPerson.Time = time;
        return this;
    }

    public FakeTestingPersonBuilder WithFirstName(string firstName)
    {
        _baseTestingPerson.FirstName = firstName;
        return this;
    }
    
    public FakeTestingPersonBuilder WithLastName(string lastName)
    {
        _baseTestingPerson.LastName = lastName;
        return this;
    }

    public TestingPerson Build() => _baseTestingPerson;
}