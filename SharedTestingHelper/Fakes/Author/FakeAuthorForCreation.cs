namespace SharedTestingHelper.Fakes.Author;

using AutoBogus;
using QueryKit.WebApiTestProject.Entities.Authors.Models;

public sealed class FakeAuthorForCreation : AutoFaker<AuthorForCreation>
{
    public FakeAuthorForCreation()
    {
    }
}