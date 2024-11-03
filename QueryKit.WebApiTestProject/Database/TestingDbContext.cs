namespace QueryKit.WebApiTestProject.Database;

using Entities.Authors;
using Entities.Ingredients;
using Entities.Recipes;
using Microsoft.EntityFrameworkCore;
using QueryKit.WebApiTestProject.Entities;

public class TestingDbContext(DbContextOptions<TestingDbContext> options) : DbContext(options)
{
    [DbFunction (Name = "SOUNDEX", IsBuiltIn = true)]
    public static string SoundsLike(string query) => throw new NotImplementedException();

    public DbSet<TestingPerson> People { get; set; }
    public DbSet<Recipe> Recipes { get; set; }
    public DbSet<IngredientPreparation> IngredientPreparations { get; set; }
    public DbSet<Ingredient> Ingredients { get; set; }
    public DbSet<Author> Authors { get; set; } 
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.HasSequence<int>(Consts.DatabaseSequences.AuthorPrefix)
            .StartsAt(100045702) // people don't like a nice round starting number
            .IncrementsBy(1);
        
        modelBuilder.HasPostgresExtension("fuzzystrmatch");
        
        modelBuilder.ApplyConfiguration(new PersonConfiguration());
        modelBuilder.ApplyConfiguration(new RecipeConfiguration());
        modelBuilder.ApplyConfiguration(new AuthorConfiguration());
    }
}
