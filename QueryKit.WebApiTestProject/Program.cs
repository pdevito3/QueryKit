using System.Reflection;
using Microsoft.EntityFrameworkCore;
using QueryKit.WebApiTestProject;
using QueryKit.WebApiTestProject.Database;

var builder = WebApplication.CreateBuilder(args);
builder.ConfigureServices();

var app = builder.Build();


app.Run();



public static class WebAppServiceConfiguration
{
    public static void ConfigureServices(this WebApplicationBuilder builder)
    {
        var configuration = builder.Configuration;
        var env = builder.Environment;
        var services = builder.Services;
        
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        var connectionString = configuration.GetConnectionStringOptions().TestingDb;
        if(string.IsNullOrWhiteSpace(connectionString))
        {
            // this makes local migrations easier to manage. feel free to refactor if desired.
            connectionString = env.IsDevelopment() 
                ? "Host=localhost;Port=50033;Database=testingdb;Username=postgres;Password=postgres"
                : throw new Exception("The database connection string is not set.");
        }

        services.AddDbContext<TestingDbContext>(options =>
            options.UseNpgsql(connectionString,
                npgSql => npgSql.MigrationsAssembly(typeof(TestingDbContext).Assembly.FullName))
                .UseSnakeCaseNamingConvention());
    }
}
