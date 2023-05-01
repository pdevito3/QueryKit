namespace SharedTestingHelper.Fakes;

using QueryKit.WebApiTestProject.Entities.Recipes;
using QueryKit.WebApiTestProject.Entities.Recipes.Models;

public class FakeRecipeBuilder
{
    private RecipeForCreation _creationData = new FakeRecipeForCreation().Generate();

    public FakeRecipeBuilder WithModel(RecipeForCreation model)
    {
        _creationData = model;
        return this;
    }
    
    public FakeRecipeBuilder WithTitle(string title)
    {
        _creationData.Title = title;
        return this;
    }
    
    public FakeRecipeBuilder WithVisibility(string visibility)
    {
        _creationData.Visibility = visibility;
        return this;
    }
    
    public FakeRecipeBuilder WithDirections(string directions)
    {
        _creationData.Directions = directions;
        return this;
    }
    
    public FakeRecipeBuilder WithRating(int? rating)
    {
        _creationData.Rating = rating;
        return this;
    }
    
    public FakeRecipeBuilder WithDateOfOrigin(DateOnly? dateOfOrigin)
    {
        _creationData.DateOfOrigin = dateOfOrigin;
        return this;
    }
    
    public FakeRecipeBuilder WithHaveMadeItMyself(bool haveMadeItMyself)
    {
        _creationData.HaveMadeItMyself = haveMadeItMyself;
        return this;
    }
    
    public Recipe Build()
    {
        var result = Recipe.Create(_creationData);
        return result;
    }
}