namespace SharedTestingHelper.Fakes.Ingredients;

using AutoBogus;
using QueryKit.WebApiTestProject.Entities.Ingredients;
using QueryKit.WebApiTestProject.Entities.Ingredients.Models;

public sealed class FakeIngredientForCreation : AutoFaker<IngredientForCreation>
{
    public FakeIngredientForCreation()
    {
    }
}