namespace QueryKit.WebApiTestProject.Entities.Authors.Models;

public sealed class AuthorForCreation
{
    public string Name { get; set; } = string.Empty;
    public Guid RecipeId { get; set; }
}
