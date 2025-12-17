namespace QueryKit.WebApiTestProject.Entities.Ingredients.Models;

public sealed class IngredientForUpdate
{
    public string Name { get; set; } = string.Empty;
    public string Quantity { get; set; } = string.Empty;
    public DateTime? ExpiresOn { get; set; }
    public string Measure { get; set; } = string.Empty;
    public long? QualityLevel { get; set; }
    public Guid RecipeId { get; set; }
}
