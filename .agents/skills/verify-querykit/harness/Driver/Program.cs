// Verification driver for QueryKit. It calls QueryKit through its public API, the same way a consumer app does,
// against an in-memory IEnumerable and a real Postgres database through EF Core. It prints one JSON document.
//
// Exit codes: 0 = every target succeeded, 2 = QueryKit threw on at least one target, 1 = usage or infrastructure error.

using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using QueryKit;
using QueryKit.Configuration;
using QueryKitVerify.Driver;

string? filter = null, sort = null;
var target = "both";
var configName = "none";
var aggregate = false;

for (var i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--filter": filter = args[++i]; break;
        case "--sort": sort = args[++i]; break;
        case "--target": target = args[++i]; break;
        case "--config": configName = args[++i]; break;
        case "--aggregate": aggregate = true; break;
        case "--list-configs":
            foreach (var (name, preset) in Configs.All) Console.WriteLine($"{name,-18} {preset.Description}");
            return 0;
        default:
            Console.Error.WriteLine($"Unknown argument '{args[i]}'.");
            Console.Error.WriteLine("Usage: qk-driver [--filter <f>] [--sort <s>] [--target memory|postgres|both] [--config <preset>] [--aggregate] | --list-configs");
            return 1;
    }
}

if (!Configs.All.TryGetValue(configName, out var configPreset))
{
    Console.Error.WriteLine($"Unknown config preset '{configName}'. Run with --list-configs.");
    return 1;
}
if (target is not ("memory" or "postgres" or "both"))
{
    Console.Error.WriteLine($"Unknown target '{target}'. Use memory, postgres, or both.");
    return 1;
}
if (aggregate && filter is null && sort is null)
{
    Console.Error.WriteLine("--aggregate needs --filter, --sort, or both.");
    return 1;
}

var api = aggregate ? "ApplyQueryKit(QueryKitData)"
    : string.Join(" + ", new[] { filter is null ? null : "ApplyQueryKitFilter", sort is null ? null : "ApplyQueryKitSort" }.Where(x => x is not null));
if (api == "") api = "none";

var results = new JsonArray();
var anyQueryKitError = false;
var anyOtherError = false;

if (target is "memory" or "both") results.Add(RunMemory());
if (target is "postgres" or "both")
{
    var connectionString = Environment.GetEnvironmentVariable("QK_VERIFY_PG");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        Console.Error.WriteLine("QK_VERIFY_PG is not set. Run the driver through 'qk run' after 'qk up'.");
        return 1;
    }
    results.Add(await RunPostgres(connectionString));
}

var doc = new JsonObject
{
    ["input"] = new JsonObject { ["filter"] = filter, ["sort"] = sort, ["config"] = configName, ["api"] = api },
    ["expression"] = DescribeExpression(),
    ["results"] = results,
};
Console.WriteLine(doc.ToJsonString(new JsonSerializerOptions
{
    WriteIndented = true,
    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
}));
return anyOtherError ? 1 : anyQueryKitError ? 2 : 0;

JsonNode? DescribeExpression()
{
    if (filter is null) return null;
    try { return FilterParser.ParseFilter<Recipe>(filter, configPreset.Build()).ToString(); }
    catch (Exception e) { return $"<{e.GetType().Name}>"; }
}

JsonObject RunMemory()
{
    // The IEnumerable overloads, as in the README section "Using QueryKit on Enumerables".
    var result = new JsonObject { ["target"] = "memory" };
    try
    {
        IEnumerable<Recipe> source = Seed.Build();
        var config = configPreset.Build();
        if (aggregate)
            source = source.ApplyQueryKit(new QueryKitData { Filters = filter, SortOrder = sort, Configuration = config });
        else
        {
            if (filter is not null) source = source.ApplyQueryKitFilter(filter, config);
            if (sort is not null) source = source.ApplyQueryKitSort(sort, config);
        }
        AddRows(result, source.ToList());
    }
    catch (Exception e)
    {
        AddError(result, e);
    }
    return result;
}

async Task<JsonObject> RunPostgres(string connectionString)
{
    var result = new JsonObject { ["target"] = "postgres" };
    var options = new DbContextOptionsBuilder<VerifyDbContext>().UseNpgsql(connectionString).Options;
    await using var db = new VerifyDbContext(options);
    await db.Database.EnsureCreatedAsync();
    if (!await db.Recipes.AnyAsync())
    {
        db.Recipes.AddRange(Seed.Build());
        await db.SaveChangesAsync();
    }

    try
    {
        IQueryable<Recipe> query = db.Recipes;
        var config = configPreset.Build();
        if (aggregate)
            query = query.ApplyQueryKit(new QueryKitData { Filters = filter, SortOrder = sort, Configuration = config });
        else
        {
            if (filter is not null) query = query.ApplyQueryKitFilter(filter, config);
            if (sort is not null) query = query.ApplyQueryKitSort(sort, config);
        }
        query = query.Include(x => x.Author).Include(x => x.Ingredients).AsNoTracking().AsSplitQuery();
        result["sql"] = query.ToQueryString();
        AddRows(result, await query.ToListAsync());
    }
    catch (Exception e)
    {
        AddError(result, e);
    }
    return result;
}

void AddRows(JsonObject result, List<Recipe> recipes)
{
    result["count"] = recipes.Count;
    result["titles"] = new JsonArray(recipes.Select(r => (JsonNode?)r.Title).ToArray());
    result["rows"] = new JsonArray(recipes.Select(r => (JsonNode?)new JsonObject
    {
        ["title"] = r.Title,
        ["author"] = r.Author.Name,
        ["rating"] = r.Rating,
        ["price"] = r.Price,
        ["isVegetarian"] = r.IsVegetarian,
        ["visibility"] = r.Visibility.ToString(),
        ["createdAt"] = r.CreatedAt.ToString("O"),
        ["dateOfOrigin"] = r.DateOfOrigin?.ToString("O"),
        ["directions"] = r.Directions,
        ["tags"] = new JsonArray(r.Tags.Select(t => (JsonNode?)t).ToArray()),
        ["ingredients"] = new JsonArray(r.Ingredients.OrderBy(x => x.Name)
            .Select(x => (JsonNode?)$"{x.Name}:{x.Stock}").ToArray()),
    }).ToArray());
}

void AddError(JsonObject result, Exception e)
{
    // A QueryKit exception is an expected outcome for error-handling proofs. Anything else is reported the same
    // way so the agent sees it, but it is not tagged as a QueryKit error.
    var isQueryKit = e is QueryKit.Exceptions.QueryKitException;
    anyQueryKitError |= isQueryKit;
    anyOtherError |= !isQueryKit;
    result["error"] = new JsonObject
    {
        ["type"] = e.GetType().FullName,
        ["isQueryKitException"] = isQueryKit,
        ["message"] = e.Message,
    };
}
