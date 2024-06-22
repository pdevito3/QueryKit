namespace SharedTestingHelper.Fakes.IngredientPreparations;

using AutoBogus;
using QueryKit.WebApiTestProject.Entities.Ingredients;
using QueryKit.WebApiTestProject.Entities.Ingredients.Models;

public sealed class FakeIngredientPreparation : AutoFaker<IngredientPreparation>
{
    public FakeIngredientPreparation()
    {
    }
}