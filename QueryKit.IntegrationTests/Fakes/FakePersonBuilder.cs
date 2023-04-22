namespace QueryKit.IntegrationTests.Fakes;

using AutoBogus;
using WebApiTestProject.Entities;

public class FakePersonBuilder
{
    private readonly TestingPerson _baseTestingPerson = new AutoFaker<TestingPerson>().Generate();
    
    public FakePersonBuilder WithTitle(string title)
    {
        _baseTestingPerson.Title = title;
        return this;
    }

    public TestingPerson Build() => _baseTestingPerson;
}