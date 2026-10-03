using Microsoft.EntityFrameworkCore;

namespace QueryKitVerify.Driver;

public enum Visibility
{
    Public = 1,
    Private = 2,
}

public class Author
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
}

public class Ingredient
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public int Stock { get; set; }
    public Guid RecipeId { get; set; }
}

public class Recipe
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string? Directions { get; set; }
    public int Rating { get; set; }
    public decimal Price { get; set; }
    public bool IsVegetarian { get; set; }
    public Visibility Visibility { get; set; }
    public DateTime CreatedAt { get; set; }

    // The same wall-clock time as CreatedAt, in a column without a time zone.
    [System.ComponentModel.DataAnnotations.Schema.Column(TypeName = "timestamp without time zone")]
    public DateTime LocalCreatedAt { get; set; }
    public DateOnly? DateOfOrigin { get; set; }
    public string Sku { get; set; } = "";
    public string? Serving { get; set; }
    public TimeOnly? ServeTime { get; set; }
    public List<string> Tags { get; set; } = [];
    public Guid AuthorId { get; set; }
    public Author Author { get; set; } = null!;
    public List<Ingredient> Ingredients { get; set; } = [];
}

public class VerifyDbContext(DbContextOptions<VerifyDbContext> options) : DbContext(options)
{
    public DbSet<Recipe> Recipes => Set<Recipe>();
    public DbSet<Author> Authors => Set<Author>();
}

/// <summary>
/// Fixed seed data. The feature map asserts against these exact values, so change both together.
/// </summary>
public static class Seed
{
    public static List<Recipe> Build()
    {
        var julia = new Author { Id = G(101), Name = "Julia Child" };
        var gordon = new Author { Id = G(102), Name = "Gordon Ramsay" };
        var anon = new Author { Id = G(103), Name = "Anonymous" };

        return
        [
            Recipe(1, "Pancakes", julia, rating: 5, price: 4.50m, vegetarian: true, Visibility.Public,
                created: new DateTime(2024, 1, 15, 8, 0, 0, DateTimeKind.Utc), origin: new DateOnly(1900, 1, 1),
                directions: "Whisk and fry", tags: ["breakfast", "sweet"],
                ingredients: [("flour", 10), ("egg", 0)],
                sku: "001", serving: "Warm, with syrup", serveTime: new TimeOnly(8, 30, 0, 500)),
            Recipe(2, "Beef Stew", gordon, rating: 3, price: 12.00m, vegetarian: false, Visibility.Public,
                created: new DateTime(2024, 3, 1, 18, 30, 0, DateTimeKind.Utc), origin: null,
                directions: "Simmer for hours", tags: ["dinner"],
                ingredients: [("beef", 2), ("salt", 50)],
                sku: "002", serving: "Hot", serveTime: new TimeOnly(18, 0, 0)),
            Recipe(3, "Salt Bread", julia, rating: 4, price: 3.25m, vegetarian: true, Visibility.Private,
                created: new DateTime(2024, 6, 10, 12, 0, 0, DateTimeKind.Utc), origin: new DateOnly(1950, 5, 20),
                directions: "Knead and bake", tags: ["bread", "Winner"],
                ingredients: [("salt", 5), ("flour", 3)],
                sku: "003", serving: "Sliced", serveTime: new TimeOnly(12, 15, 30, 250)),
            Recipe(4, "Plain Water", anon, rating: 1, price: 0m, vegetarian: true, Visibility.Private,
                created: new DateTime(2023, 12, 31, 23, 59, 0, DateTimeKind.Utc), origin: null,
                directions: null, tags: [],
                ingredients: [],
                sku: "004", serving: null, serveTime: null),
        ];
    }

    private static Recipe Recipe(int n, string title, Author author, int rating, decimal price, bool vegetarian,
        Visibility visibility, DateTime created, DateOnly? origin, string? directions, List<string> tags,
        (string Name, int Stock)[] ingredients, string sku, string? serving, TimeOnly? serveTime)
    {
        var id = G(n);
        return new Recipe
        {
            Id = id,
            Title = title,
            Directions = directions,
            Rating = rating,
            Price = price,
            IsVegetarian = vegetarian,
            Visibility = visibility,
            CreatedAt = created,
            LocalCreatedAt = DateTime.SpecifyKind(created, DateTimeKind.Unspecified),
            DateOfOrigin = origin,
            Sku = sku,
            Serving = serving,
            ServeTime = serveTime,
            Tags = tags,
            AuthorId = author.Id,
            Author = author,
            Ingredients = ingredients
                .Select((x, i) => new Ingredient { Id = G(n * 100 + i), Name = x.Name, Stock = x.Stock, RecipeId = id })
                .ToList(),
        };
    }

    private static Guid G(int n) => Guid.Parse($"00000000-0000-0000-0000-{n:D12}");
}
