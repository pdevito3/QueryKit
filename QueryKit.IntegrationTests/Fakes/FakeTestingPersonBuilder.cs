namespace QueryKit.IntegrationTests.Fakes;

using AutoBogus;
using WebApiTestProject.Entities;

public class FakeTestingPersonBuilder
{
    private readonly TestingPerson _baseTestingPerson = new AutoFaker<TestingPerson>().Generate();
    
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

    public TestingPerson Build() => _baseTestingPerson;
}