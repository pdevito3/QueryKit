namespace QueryKit.IntegrationTests;

using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using FluentAssertions;
using FluentAssertions.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Testcontainers.PostgreSql;
using WebApiTestProject;
using WebApiTestProject.Database;
using Xunit;

[CollectionDefinition(nameof(TestFixture))]
public class TestFixtureCollection : ICollectionFixture<TestFixture> {}

public class TestFixture : IAsyncLifetime
{
    public static IServiceScopeFactory BaseScopeFactory;
    private PostgreSqlContainer _dbContainer;

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Consts.Testing.IntegrationTestingEnvName
        });

        _dbContainer = new PostgreSqlBuilder().Build();
        await _dbContainer.StartAsync();
        builder.Configuration.GetSection(ConnectionStringOptions.SectionName)[ConnectionStringOptions.RecipeManagementKey] = _dbContainer.GetConnectionString();
        await RunMigration(_dbContainer.GetConnectionString());

        builder.ConfigureServices();
        var services = builder.Services;

        // add any mock services here
        services.ReplaceServiceWithSingletonMock<IHttpContextAccessor>();

        var provider = services.BuildServiceProvider();
        BaseScopeFactory = provider.GetService<IServiceScopeFactory>();
        SetupDateAssertions();
    }

    private static async Task RunMigration(string connectionString)
    {
        var options = new DbContextOptionsBuilder<TestingDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        var context = new TestingDbContext(options);
        await context?.Database?.MigrateAsync();
    }
    
    public async Task DisposeAsync()
    {
        await _dbContainer.DisposeAsync();
    }

    private static void SetupDateAssertions()
    {
        // close to equivalency required to reconcile precision differences between EF and Postgres
        AssertionOptions.AssertEquivalencyUsing(options =>
        {
            options.Using<DateTime>(ctx => ctx.Subject
                .Should()
                .BeCloseTo(ctx.Expectation, 1.Seconds())).WhenTypeIs<DateTime>();
            options.Using<DateTimeOffset>(ctx => ctx.Subject
                .Should()
                .BeCloseTo(ctx.Expectation, 1.Seconds())).WhenTypeIs<DateTimeOffset>();

            return options;
        });
    }
}

public static class ServiceCollectionServiceExtensions
{
    public static IServiceCollection ReplaceServiceWithSingletonMock<TService>(this IServiceCollection services)
        where TService : class
    {
        services.RemoveAll(typeof(TService));
        services.AddSingleton(_ => Mock.Of<TService>());
        return services;
    }
}
