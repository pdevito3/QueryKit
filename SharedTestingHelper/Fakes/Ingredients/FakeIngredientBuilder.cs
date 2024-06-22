namespace SharedTestingHelper.Fakes.Ingredients;

using QueryKit.WebApiTestProject.Entities.Ingredients;
using QueryKit.WebApiTestProject.Entities.Ingredients.Models;

public class FakeIngredientBuilder
{
    private IngredientForCreation _creationData = new FakeIngredientForCreation().Generate();
    private List<IngredientPreparation> _preparations = new();

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
    
    public FakeIngredientBuilder WithPreparation(IngredientPreparation preparation)
    {
        _preparations.Add(preparation);
        return this;
    }
    
    public Ingredient Build()
    {
        var result = Ingredient.Create(_creationData);
        result.Preparations = _preparations;
        return result;
    }
}