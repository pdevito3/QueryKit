namespace QueryKit.WebApiTestProject.Database;

using Microsoft.EntityFrameworkCore;
using QueryKit.WebApiTestProject.Entities;

public class TestingDbContext : DbContext
{
    public TestingDbContext(DbContextOptions<TestingDbContext> options)
        : base(options)
    {
    }

    public DbSet<TestingPerson> People { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new PersonConfiguration());
    }
}
