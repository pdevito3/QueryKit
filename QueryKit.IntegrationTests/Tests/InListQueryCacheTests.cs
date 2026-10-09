namespace QueryKit.IntegrationTests.Tests;

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
using QueryKit.Configuration;
using SharedTestingHelper.Fakes;
using WebApiTestProject.Database;

// In the constant mode (ParameterizeFilterValues = false), EF Core finds a compiled query for an in-list
// with Equals and GetHashCode on the list constant. These tests use one DbContextOptions with its own query cache.
public class InListQueryCacheTests() : TestBase
{
    [Theory]
    [InlineData("""Title ^^ ["lamb", "chicken"]""")]
    [InlineData("""Title ^^* ["LAMB", "chicken"]""")]
    [InlineData("""Title !^^ ["lamb", "chicken"]""")]
    [InlineData("""Title !^^* ["LAMB", "chicken"]""")]
    [InlineData("Age ^^ [18, 30]")]
    [InlineData("BirthMonth ^^ [1, 2]")]
    [InlineData("""SpecificDateTime ^^ ["2022-08-27T15:58:50Z"]""")]
    public void equal_in_lists_compile_once(string input)
    {
        var cache = new QueryCache(new TestingServiceScope());

        cache.QueryString(input);
        cache.QueryString(input);

        cache.Compilations.Should().Be(1);
    }

    // A list value without a zone is read as UTC, so it is the same value as a list value with the UTC zone.
    // Both requests filter, and they share one compiled query.
    [Theory]
    [InlineData("^^")]
    [InlineData("^^*")]
    [InlineData("!^^")]
    [InlineData("!^^*")]
    public async Task datetime_in_list_without_a_zone_filters_like_the_utc_zone(string op)
    {
        var testingServiceScope = new TestingServiceScope();
        var when = new DateTime(2001, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(Random.Shared.Next(1, 900_000_000));
        var person = new FakeTestingPersonBuilder().WithSpecificDateTime(when).Build();
        await testingServiceScope.InsertAsync(person);
        var iso = when.ToString("yyyy-MM-ddTHH:mm:ss");
        var cache = new QueryCache(testingServiceScope);

        await RequestFilters(cache, $"""SpecificDateTime {op} ["{iso}"]""", op, person.Id);
        await RequestFilters(cache, $"""SpecificDateTime {op} ["{iso}Z"]""", op, person.Id);

        cache.Compilations.Should().Be(1);
    }

    // The hash of an array covers only its last 8 items. Lists that differ before the last 8 items
    // must still get different hash codes, or each lookup compares them item by item.
    [Theory]
    [InlineData("^^")]
    [InlineData("^^*")]
    [InlineData("!^^")]
    [InlineData("!^^*")]
    public void long_in_lists_with_the_same_last_items_get_different_cache_keys(string op)
    {
        var cache = new QueryCache(new TestingServiceScope());
        var lastItems = Enumerable.Range(1, 8).Select(x => $"\"last {x}\"");

        cache.QueryString($"Title {op} [{string.Join(", ", Enumerable.Range(0, 100).Select(x => $"\"a {x}\"").Concat(lastItems))}]");
        cache.QueryString($"Title {op} [{string.Join(", ", Enumerable.Range(0, 100).Select(x => $"\"b {x}\"").Concat(lastItems))}]");

        cache.Compilations.Should().Be(2);
        cache.KeyHashCodes.Distinct().Should().HaveCount(2);
    }

    private static async Task RequestFilters(QueryCache cache, string input, string op, Guid personId)
    {
        var ids = await cache.Ids(input);

        ids.Contains(personId).Should().Be(!op.StartsWith('!'));
    }

    private sealed class QueryCache
    {
        private static readonly QueryKitConfiguration ConstantMode = new(settings => settings.ParameterizeFilterValues = false);
        private readonly DbContextOptions<TestingDbContext> _options;
        private readonly RecordingMemoryCache _memoryCache = new();
        private int _compilations;

        public QueryCache(TestingServiceScope testingServiceScope)
        {
            // A new memory cache gives EF Core a new internal service provider, so ignore the warning
            // for many service providers.
            _options = new DbContextOptionsBuilder<TestingDbContext>()
                .UseNpgsql(testingServiceScope.DbContext().Database.GetConnectionString())
                .UseSnakeCaseNamingConvention()
                .UseMemoryCache(_memoryCache)
                .ConfigureWarnings(warnings => warnings.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning))
                .LogTo(_ => Interlocked.Increment(ref _compilations), new[] { CoreEventId.QueryCompilationStarting })
                .Options;
        }

        public int Compilations => _compilations;

        public IEnumerable<int> KeyHashCodes => _memoryCache.QueryKeys.Select(key => key.GetHashCode());

        public string QueryString(string input)
        {
            using var context = new TestingDbContext(_options);
            return context.People.ApplyQueryKitFilter(input, ConstantMode).ToQueryString();
        }

        public async Task<List<Guid>> Ids(string input)
        {
            await using var context = new TestingDbContext(_options);
            return await context.People.ApplyQueryKitFilter(input, ConstantMode).Select(x => x.Id).ToListAsync();
        }
    }

    // Records each compiled query key that EF Core looks up.
    private sealed class RecordingMemoryCache : IMemoryCache
    {
        private readonly MemoryCache _inner = new(new MemoryCacheOptions());
        private readonly HashSet<object> _queryKeys = new();

        public IEnumerable<object> QueryKeys => _queryKeys;

        public bool TryGetValue(object key, out object? value)
        {
            if (key.GetType().Name.Contains("CompiledQueryCacheKey"))
            {
                _queryKeys.Add(key);
            }

            return _inner.TryGetValue(key, out value);
        }

        public ICacheEntry CreateEntry(object key) => _inner.CreateEntry(key);

        public void Remove(object key) => _inner.Remove(key);

        public void Dispose() => _inner.Dispose();
    }
}
