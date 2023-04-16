namespace QueryKit.IntegrationTests.Fakes;

using AutoBogus;
using WebApiTestProject.Entities;

public class FakePersonBuilder
{
    private readonly Person _basePerson = new AutoFaker<Person>().Generate();
    
    public FakePersonBuilder WithTitle(string title)
    {
        _basePerson.Title = title;
        return this;
    }

    public Person Build() => _basePerson;
}