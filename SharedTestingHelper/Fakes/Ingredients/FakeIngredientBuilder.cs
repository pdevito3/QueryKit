namespace SharedTestingHelper.Fakes.Ingredients;

using QueryKit.WebApiTestProject.Entities.Ingredients;
using QueryKit.WebApiTestProject.Entities.Ingredients.Models;

public class FakeIngredientBuilder
{
    private IngredientForCreation _creationData = new FakeIngredientForCreation().Generate();

    public FakeIngredientBuilder WithModel(IngredientForCreation model)
    {
        _creationData = model;
        return this;
    }
    
    public FakeIngredientBuilder WithName(string name)
    {
        _creationData.Name = name;
        return this;
    }
    
    public Ingredient Build()
    {
        var result = Ingredient.Create(_creationData);
        return result;
    }
}