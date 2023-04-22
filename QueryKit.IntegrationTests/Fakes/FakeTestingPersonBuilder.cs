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

    public TestingPerson Build() => _baseTestingPerson;

    public FakeTestingPersonBuilder WithAge(int age)
    {
        _baseTestingPerson.Age = age;
        return this;
    }

    public FakeTestingPersonBuilder WithBirthMonth(string value)
    {
        _baseTestingPerson.BirthMonth = value;
        return this;
    }
}