namespace QueryKit.UnitTests;

using Configuration;
using Exceptions;
using FluentAssertions;

// A collection has no single sort order, so a sort on a collection property is a parsing error.
public class CollectionSortTests
{
    [Theory]
    [InlineData(nameof(Row.List))]
    [InlineData(nameof(Row.ReadOnlyList))]
    [InlineData(nameof(Row.Set))]
    [InlineData(nameof(Row.Array))]
    [InlineData(nameof(Row.Numbers))]
    [InlineData(nameof(Row.Children))]
    [InlineData("Parent.Tags")]
    public void sort_on_a_collection_property_throws_sort_parsing_exception(string property)
    {
        var act = () => Rows().AsQueryable().ApplyQueryKitSort(property).ToList();

        act.Should().Throw<SortParsingException>()
            .WithMessage($"Parsing failed during sorting. '{property}' is a collection, and a collection has no single sort order.");
    }

    [Theory]
    [InlineData("List desc")]
    [InlineData("-List")]
    [InlineData("Name, List")]
    public void sort_on_a_collection_property_throws_for_every_sort_form(string input)
    {
        var act = () => Rows().ApplyQueryKitSort(input).ToList();

        act.Should().Throw<SortParsingException>();
    }

    [Fact]
    public void sort_on_a_collection_property_throws_through_apply_querykit()
    {
        var act = () => Rows().AsQueryable().ApplyQueryKit(new QueryKitData { SortOrder = "List" }).ToList();

        act.Should().Throw<SortParsingException>();
    }

    [Fact]
    public void sort_on_a_collection_query_name_names_the_query_name()
    {
        var config = new QueryKitConfiguration(settings =>
        {
            settings.Property<Row>(x => x.List).HasQueryName("tags");
        });

        var act = () => Rows().AsQueryable().ApplyQueryKitSort("tags", config).ToList();

        act.Should().Throw<SortParsingException>()
            .WithMessage("Parsing failed during sorting. 'tags' is a collection, and a collection has no single sort order.");
    }

    [Fact]
    public void prevented_sort_on_a_collection_property_is_ignored()
    {
        var config = new QueryKitConfiguration(settings =>
        {
            settings.Property<Row>(x => x.List).PreventSort();
        });

        var result = Rows().AsQueryable().ApplyQueryKitSort("List, Name desc", config).ToList();

        result.Select(x => x.Name).Should().Equal("b", "a");
    }

    [Fact]
    public void sort_on_a_string_still_sorts()
    {
        var result = Rows().ApplyQueryKitSort("Name desc").ToList();

        result.Select(x => x.Name).Should().Equal("b", "a");
    }

    // A database sorts a byte array as one binary value, for example a SQL Server rowversion. In memory, byte[] is not IComparable.
    [Fact]
    public void sort_on_a_byte_array_parses()
    {
        var sorts = SortParser.ParseSort<Row>("Version desc");

        sorts.Should().ContainSingle().Which.Expression!.Body.ToString().Should().Be("Convert(x.Version, Object)");
    }

    private static Row[] Rows() =>
    [
        new("a", ["x"], [1], [1]),
        new("b", ["y"], [2], [2]),
    ];

    public class Row(string name, string[] tags, int[] numbers, byte[] version)
    {
        public string Name { get; } = name;
        public List<string> List { get; } = [..tags];
        public IReadOnlyList<string> ReadOnlyList { get; } = tags;
        public HashSet<string> Set { get; } = [..tags];
        public string[] Array { get; } = tags;
        public List<int> Numbers { get; } = [..numbers];
        public List<Row> Children { get; } = [];
        public Parent Parent { get; } = new(tags);
        public byte[] Version { get; } = version;
    }

    public class Parent(string[] tags)
    {
        public List<string> Tags { get; } = [..tags];
    }
}
