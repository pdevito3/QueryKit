namespace QueryKit.WebApiTestProject.Entities.Recipes.Models;

public sealed class RecipeForUpdate
{
    public string Title { get; set; }
    public string Visibility { get; set; }
    public string Directions { get; set; }
    public int? Rating { get; set; }
    public DateOnly? DateOfOrigin { get; set; }
    public bool HaveMadeItMyself { get; set; }

}
