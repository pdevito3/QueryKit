namespace QueryKit.WebApiTestProject.Entities.Recipes;

using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using Authors;
using Ingredients;
using Models;

public class Recipe : BaseEntity
{
    public string Title { get; private set; }

    private VisibilityEnum _visibility;
    public string Visibility
    {
        get => _visibility.Name;
        private set
        {
            if (!VisibilityEnum.TryFromName(value, true, out var parsed))
                throw new Exception($"Invalid value for {nameof(Visibility)}: {value}");

            _visibility = parsed;
        }
    }

    public string Directions { get; private set; }

    public int? Rating { get; private set; }

    public DateOnly? DateOfOrigin { get; private set; }

    public bool HaveMadeItMyself { get; private set; }

    [JsonIgnore, IgnoreDataMember]
    public Author Author { get; private set; }
    
    private List<Ingredient> _ingredients = new();
    [JsonIgnore, IgnoreDataMember]
    public IReadOnlyCollection<Ingredient> Ingredients => _ingredients.AsReadOnly();

    public void AddIngredient(Ingredient ingredient)
    {
        _ingredients.Add(ingredient);
    }

    public static Recipe Create(RecipeForCreation recipeForCreation)
    {
        var newRecipe = new Recipe();

        newRecipe.Title = recipeForCreation.Title;
        newRecipe.Visibility = recipeForCreation.Visibility;
        newRecipe.Directions = recipeForCreation.Directions;
        newRecipe.Rating = recipeForCreation.Rating;
        newRecipe.DateOfOrigin = recipeForCreation.DateOfOrigin;
        newRecipe.HaveMadeItMyself = recipeForCreation.HaveMadeItMyself;
        return newRecipe;
    }

    public Recipe Update(RecipeForUpdate recipeForUpdate)
    {
        Title = recipeForUpdate.Title;
        Visibility = recipeForUpdate.Visibility;
        Directions = recipeForUpdate.Directions;
        Rating = recipeForUpdate.Rating;
        DateOfOrigin = recipeForUpdate.DateOfOrigin;
        HaveMadeItMyself = recipeForUpdate.HaveMadeItMyself;
        return this;
    }
    
    public Recipe SetAuthor(Author author)
    {
        Author = author;
        return this;
    }
    
    public Recipe SetIngredients(List<Ingredient> ingredients)
    {
        _ingredients = ingredients;
        return this;
    }

    protected Recipe() { } // For EF + Mocking
}