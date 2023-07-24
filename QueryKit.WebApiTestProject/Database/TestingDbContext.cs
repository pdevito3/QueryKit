namespace QueryKit.WebApiTestProject.Database;

using Entities.Ingredients;
using Entities.Recipes;
using Microsoft.EntityFrameworkCore;
using QueryKit.WebApiTestProject.Entities;

public class TestingDbContext : DbContext
{
    public TestingDbContext(DbContextOptions<TestingDbContext> options)
        : base(options)
    {
    }
    
    [DbFunction (Name = "SOUNDEX", IsBuiltIn = true)]
    public static string SoundsLike(string query) => throw new NotImplementedException();

    public DbSet<TestingPerson> People { get; set; }
    public DbSet<Recipe> Recipes { get; set; }
    public DbSet<IngredientPreparation> IngredientPreparations { get; set; }
    public DbSet<Ingredient> Ingredients { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasPostgresExtension("fuzzystrmatch");
        
        modelBuilder.ApplyConfiguration(new PersonConfiguration());
        modelBuilder.ApplyConfiguration(new RecipeConfiguration());
    }
}
