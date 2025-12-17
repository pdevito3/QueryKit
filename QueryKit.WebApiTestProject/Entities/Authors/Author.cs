namespace QueryKit.WebApiTestProject.Entities.Authors;

using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using Models;
using Recipes;

public class Author : BaseEntity
{
    public string Name { get; private set; } = null!;

    [JsonIgnore, IgnoreDataMember]
    [ForeignKey("Recipe")]
    public Guid RecipeId { get; private set; }
    public Recipe Recipe { get; private set; } = null!;

    public string InternalIdentifier { get; } = null!;


    public static Author Create(AuthorForCreation authorForCreation)
    {
        var newAuthor = new Author();

        newAuthor.Name = authorForCreation.Name;
        newAuthor.RecipeId = authorForCreation.RecipeId;
        
        return newAuthor;
    }

    public Author Update(AuthorForUpdate authorForUpdate)
    {
        Name = authorForUpdate.Name;
        RecipeId = authorForUpdate.RecipeId;
        return this;
    }
    
    protected Author() { } // For EF + Mocking
}