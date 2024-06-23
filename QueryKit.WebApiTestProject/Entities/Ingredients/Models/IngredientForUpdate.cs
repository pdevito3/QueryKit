namespace QueryKit.WebApiTestProject.Entities.Ingredients.Models;

public sealed class IngredientForUpdate
{
    public string Name { get; set; }
    public string Quantity { get; set; }
    public DateTime? ExpiresOn { get; set; }
    public string Measure { get; set; }
    public long? QualityLevel { get; set; }
    public Guid RecipeId { get; set; }
}
