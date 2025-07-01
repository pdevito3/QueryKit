namespace QueryKit.UnitTests;

using FluentAssertions;
using QueryKit.WebApiTestProject.Entities.Recipes;
using SharedTestingHelper.Fakes.Recipes;

public class EnumerableFilteringTests()
{
    [Fact]
    public async Task can_filter_enumerable()
    {
        // Arrange
        var recipeOne = new FakeRecipeBuilder().Build();
        var recipeTwo = new FakeRecipeBuilder().Build();
        var listOfRecipes = new List<Recipe> { recipeOne, recipeTwo };
        
        var input = $"""{nameof(Recipe.Title)} == "{recipeOne.Title}" """;
        
        // Act
        var filteredRecipes = listOfRecipes.ApplyQueryKitFilter(input).ToList();

        // Assert
        filteredRecipes.Count.Should().Be(1);
        filteredRecipes[0].Id.Should().Be(recipeOne.Id);
    }
}
