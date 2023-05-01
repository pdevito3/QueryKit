namespace SharedTestingHelper.Fakes.Author;

using QueryKit.WebApiTestProject.Entities.Authors;
using QueryKit.WebApiTestProject.Entities.Authors.Models;

public class FakeAuthorBuilder
{
    private AuthorForCreation _creationData = new FakeAuthorForCreation().Generate();

    public FakeAuthorBuilder WithModel(AuthorForCreation model)
    {
        _creationData = model;
        return this;
    }
    
    public FakeAuthorBuilder WithName(string name)
    {
        _creationData.Name = name;
        return this;
    }
    
    public FakeAuthorBuilder WithRecipeId(Guid recipeId)
    {
        _creationData.RecipeId = recipeId;
        return this;
    }
    
    public Author Build()
    {
        var result = Author.Create(_creationData);
        return result;
    }
}