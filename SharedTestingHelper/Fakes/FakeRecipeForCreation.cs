namespace SharedTestingHelper.Fakes;

using AutoBogus;
using QueryKit.WebApiTestProject.Entities.Recipes;
using QueryKit.WebApiTestProject.Entities.Recipes.Models;

public sealed class FakeRecipeForCreation : AutoFaker<RecipeForCreation>
{
    public FakeRecipeForCreation()
    {
        RuleFor(r => r.Visibility, f => f.PickRandom<VisibilityEnum>(VisibilityEnum.List).Name);
    }
}