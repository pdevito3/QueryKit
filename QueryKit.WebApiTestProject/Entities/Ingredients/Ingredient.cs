namespace QueryKit.WebApiTestProject.Entities.Ingredients;

using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using Models;
using Recipes;

public class IngredientPreparation
{
    public string Text { get; set; }
}

public class Ingredient : BaseEntity
{
    public string Name { get; private set; }

    public string Quantity { get; private set; }

    public DateTime? ExpiresOn { get; private set; }

    public string Measure { get; private set; }

    public int MinimumQuality { get; private set; }

    public List<IngredientPreparation> Preparations { get; set; } = new();

    [JsonIgnore, IgnoreDataMember]
    [ForeignKey("Recipe")]
    public Guid RecipeId { get; private set; }
    public Recipe Recipe { get; private set; }

    public static Ingredient Create(IngredientForCreation ingredientForCreation)
    {
        var newIngredient = new Ingredient();

        newIngredient.Name = ingredientForCreation.Name;
        newIngredient.Quantity = ingredientForCreation.Quantity;
        newIngredient.ExpiresOn = ingredientForCreation.ExpiresOn;
        newIngredient.Measure = ingredientForCreation.Measure;
        newIngredient.RecipeId = ingredientForCreation.RecipeId;
        return newIngredient;
    }

    public Ingredient Update(IngredientForUpdate ingredientForUpdate)
    {
        Name = ingredientForUpdate.Name;
        Quantity = ingredientForUpdate.Quantity;
        ExpiresOn = ingredientForUpdate.ExpiresOn;
        Measure = ingredientForUpdate.Measure;
        RecipeId = ingredientForUpdate.RecipeId;
        return this;
    }
    
    protected Ingredient() { } // For EF + Mocking
}