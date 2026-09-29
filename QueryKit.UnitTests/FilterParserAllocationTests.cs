namespace QueryKit.UnitTests;

using Configuration;
using FluentAssertions;
using WebApiTestProject.Entities;

// The parsers and the alias regexes are built once, not on each parse. Each budget is about two
// times the bytes that one parse allocates, and less than the bytes that one parse allocated when
// each parse built the parsers and the regexes again.
public class FilterParserAllocationTests
{
    private const int Iterations = 50;

    [Fact]
    public void a_simple_filter_does_not_build_the_parsers_again()
    {
        BytesForEachParse("Age > 25", null).Should().BeLessThan(100 * 1024);
    }

    [Fact]
    public void property_aliases_do_not_build_a_regex_on_each_parse()
    {
        var config = new QueryKitConfiguration(c =>
        {
            c.Property<TestingPerson>(x => x.Title).HasQueryName("name");
            c.Property<TestingPerson>(x => x.Age).HasQueryName("years");
            c.Property<TestingPerson>(x => x.Rating).HasQueryName("score");
            c.Property<TestingPerson>(x => x.FirstName).HasQueryName("first");
            c.Property<TestingPerson>(x => x.Id).HasQueryName("key");
        });

        BytesForEachParse("""name == "lamb" && years > 25""", config).Should().BeLessThan(250 * 1024);
    }

    [Fact]
    public void operator_aliases_do_not_build_a_regex_on_each_parse()
    {
        var config = new QueryKitConfiguration(c =>
        {
            c.EqualsOperator = "eq";
            c.GreaterThanOperator = "gt";
            c.AndOperator = "and";
        });

        BytesForEachParse("""Title eq "lamb" and Age gt 25""", config).Should().BeLessThan(200 * 1024);
    }

    private static long BytesForEachParse(string filter, IQueryKitConfiguration? config)
    {
        for (var i = 0; i < Iterations; i++)
            FilterParser.ParseFilter<TestingPerson>(filter, config);

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < Iterations; i++)
            FilterParser.ParseFilter<TestingPerson>(filter, config);
        return (GC.GetAllocatedBytesForCurrentThread() - before) / Iterations;
    }
}
