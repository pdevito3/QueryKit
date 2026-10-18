namespace QueryKit.IntegrationTests.Tests;

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SharedTestingHelper.Fakes.Recipes;

// A list endpoint often projects an entity to a DTO that exposes a collection as IReadOnlyList<T>, and then applies the filter.
public class CollectionTypeFilterTests : TestBase
{
    private static readonly (string Operation, string[] Expected)[] Cases =
    [
        ("""^$ "dinner" """, ["stew"]),
        ("""!^$ "dinner" """, ["pancakes", "bread", "water"]),
        ("""== "dinner" """, ["stew"]),
        ("""==* "DINNER" """, ["stew"]),
        ("""!= "dinner" """, ["pancakes", "bread"]),
        ("""!=* "DINNER" """, ["pancakes", "bread"]),
        ("""%!= "dinner" """, ["pancakes", "bread", "water"]),
        ("""%== "dinner" """, ["stew", "water"]),
        ("""@= "ee" """, ["pancakes"]),
        ("""_= "br" """, ["pancakes", "bread"]),
        ("#== 2", ["pancakes", "bread"]),
    ];

    public static TheoryData<string, string, string[]> Filters()
    {
        var data = new TheoryData<string, string, string[]>();
        foreach (var property in new[] { nameof(RecipeDto.List), nameof(RecipeDto.ReadOnlyList), nameof(RecipeDto.ReadOnlyCollection) })
        foreach (var (operation, expected) in Cases)
            data.Add(property, operation, expected);
        return data;
    }

    [Theory]
    [MemberData(nameof(Filters))]
    public async Task collection_filter_on_a_projected_dto_matches_for_every_collection_type(string property, string operation, string[] expectedTitles)
    {
        // Arrange
        var testingServiceScope = new TestingServiceScope();
        var prefix = $"collection type {Guid.NewGuid()} ";
        var pancakes = new FakeRecipeBuilder().WithTitle($"{prefix}pancakes").Build().SetTags(["breakfast", "sweet"]);
        var stew = new FakeRecipeBuilder().WithTitle($"{prefix}stew").Build().SetTags(["dinner"]);
        var bread = new FakeRecipeBuilder().WithTitle($"{prefix}bread").Build().SetTags(["bread", "Winner"]);
        var water = new FakeRecipeBuilder().WithTitle($"{prefix}water").Build().SetTags([]);
        await testingServiceScope.InsertAsync(pancakes, stew, bread, water);

        // Act
        var recipes = await testingServiceScope.DbContext().Recipes
            .Where(x => x.Title.StartsWith(prefix))
            .Select(x => new RecipeDto
            {
                Title = x.Title,
                List = x.Tags,
                ReadOnlyList = x.Tags,
                ReadOnlyCollection = x.Tags,
            })
            .ApplyQueryKitFilter($"{property} {operation}")
            .ToListAsync();

        // Assert
        recipes.Select(x => x.Title[prefix.Length..]).Should().BeEquivalentTo(expectedTitles);
    }

    public class RecipeDto
    {
        public string Title { get; init; } = null!;
        public List<string> List { get; init; } = null!;
        public IReadOnlyList<string> ReadOnlyList { get; init; } = null!;
        public IReadOnlyCollection<string> ReadOnlyCollection { get; init; } = null!;
    }
}
