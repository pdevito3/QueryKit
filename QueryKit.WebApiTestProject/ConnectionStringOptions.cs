namespace QueryKit.WebApiTestProject;

public class ConnectionStringOptions
{
    public const string SectionName = "ConnectionStrings";
    public const string RecipeManagementKey = "TestingDb";

    public string TestingDb { get; set; } = String.Empty;
}

public static class ConnectionStringOptionsExtensions
{
    public static ConnectionStringOptions GetConnectionStringOptions(this IConfiguration configuration)
        => configuration.GetSection(ConnectionStringOptions.SectionName).Get<ConnectionStringOptions>() ?? new ConnectionStringOptions();
}