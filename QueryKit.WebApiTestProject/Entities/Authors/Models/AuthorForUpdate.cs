namespace QueryKit.WebApiTestProject.Entities.Authors.Models;

public sealed class AuthorForUpdate
{
    public string Name { get; set; } = string.Empty;
    public Guid RecipeId { get; set; }
}
