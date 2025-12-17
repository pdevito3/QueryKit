namespace QueryKit.WebApiTestProject.Entities.Recipes.Models;

public sealed class RecipeForCreation
{
    public string Title { get; set; } = string.Empty;
    public string Visibility { get; set; } = string.Empty;
    public string Directions { get; set; } = string.Empty;
    public int? Rating { get; set; }
    public DateOnly? DateOfOrigin { get; set; }
    public bool HaveMadeItMyself { get; set; }
    public string CollectionEmail { get; set; } = string.Empty;
    public Guid? SecondaryId { get; set; }
}
