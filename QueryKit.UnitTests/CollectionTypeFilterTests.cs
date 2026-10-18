namespace QueryKit.UnitTests;

using System.Collections.ObjectModel;
using FluentAssertions;

// A filter on a collection property gives the same rows for every type that implements IEnumerable<T>.
public class CollectionTypeFilterTests
{
    public static readonly string[] CollectionProperties =
    [
        nameof(Tagged.List), nameof(Tagged.ReadOnlyList), nameof(Tagged.ReadOnlyCollection), nameof(Tagged.Collection),
        nameof(Tagged.Set), nameof(Tagged.Array), nameof(Tagged.Enumerable), nameof(Tagged.Custom),
    ];

    private static readonly (string Operation, string[] Expected)[] TextCases =
    [
        ("""^$ "dinner" """, ["stew"]),
        ("""!^$ "dinner" """, ["pancakes", "bread", "water"]),
        ("""^$* "WINNER" """, ["bread"]),
        ("""== "dinner" """, ["stew"]),
        ("""==* "DINNER" """, ["stew"]),
        ("""!= "dinner" """, ["pancakes", "bread"]),
        ("""!=* "DINNER" """, ["pancakes", "bread"]),
        ("""%!= "dinner" """, ["pancakes", "bread", "water"]),
        ("""%== "dinner" """, ["stew", "water"]),
        ("""@= "ee" """, ["pancakes"]),
        ("""_= "br" """, ["pancakes", "bread"]),
        ("""_-= "er" """, ["stew", "bread"]),
        ("#== 2", ["pancakes", "bread"]),
        ("#> 0", ["pancakes", "stew", "bread"]),
    ];

    public static TheoryData<string, string, string[]> TextFilters()
    {
        var data = new TheoryData<string, string, string[]>();
        foreach (var property in CollectionProperties)
        foreach (var (operation, expected) in TextCases)
            data.Add(property, operation, expected);
        return data;
    }

    [Theory]
    [MemberData(nameof(TextFilters))]
    public void text_collection_filter_matches_for_every_collection_type(string property, string operation, string[] expected)
    {
        var result = TaggedRows().AsQueryable().ApplyQueryKitFilter($"{property} {operation}").ToList();

        result.Select(x => x.Name).Should().BeEquivalentTo(expected);
    }

    [Theory]
    [InlineData(nameof(Scored.Array), "> 6", new[] { "a" })]
    [InlineData(nameof(Scored.ReadOnlyList), "> 6", new[] { "a" })]
    [InlineData(nameof(Scored.Array), "== 5", new[] { "b" })]
    [InlineData(nameof(Scored.ReadOnlyList), "== 5", new[] { "b" })]
    [InlineData(nameof(Scored.Array), "^$ 5", new[] { "b" })]
    [InlineData(nameof(Scored.ReadOnlyList), "^$ 5", new[] { "b" })]
    [InlineData(nameof(Scored.Array), "#== 0", new[] { "c" })]
    [InlineData(nameof(Scored.ReadOnlyList), "#== 0", new[] { "c" })]
    public void number_collection_filter_matches_for_every_collection_type(string property, string operation, string[] expected)
    {
        var rows = new[]
        {
            new Scored("a", [1, 9]),
            new Scored("b", [5]),
            new Scored("c", []),
        };

        var result = rows.AsQueryable().ApplyQueryKitFilter($"{property} {operation}").ToList();

        result.Select(x => x.Name).Should().BeEquivalentTo(expected);
    }

    [Theory]
    [InlineData("Tags == null", new[] { "none" })]
    [InlineData("Tags != null", new[] { "empty", "one" })]
    [InlineData("Labels == null", new[] { "none" })]
    [InlineData("Labels != null", new[] { "empty", "one" })]
    [InlineData("ReadOnlyTags == null", new[] { "none" })]
    [InlineData("ReadOnlyTags != null", new[] { "empty", "one" })]
    public void null_comparison_on_a_collection_checks_the_collection_for_null(string input, string[] expected)
    {
        var rows = new[]
        {
            new NullableTagged { Name = "none" },
            new NullableTagged { Name = "empty", Tags = [], Labels = [], ReadOnlyTags = [] },
            new NullableTagged { Name = "one", Tags = ["x"], Labels = ["x"], ReadOnlyTags = ["x"] },
        };

        var result = rows.AsQueryable().ApplyQueryKitFilter(input).ToList();

        result.Select(x => x.Name).Should().BeEquivalentTo(expected);
    }

    [Theory]
    [InlineData("ChildArray.Name == null", new[] { "second" })]
    [InlineData("ChildReadOnlyList.Name == null", new[] { "second" })]
    [InlineData("ChildReadOnlyList.Name != null", new[] { "first", "second" })]
    public void null_comparison_on_a_nested_path_compares_each_element(string input, string[] expected)
    {
        var rows = new[]
        {
            new Parent("first", [new Child("a")]),
            new Parent("second", [new Child("a"), new Child(null)]),
        };

        var result = rows.AsQueryable().ApplyQueryKitFilter(input).ToList();

        result.Select(x => x.Name).Should().BeEquivalentTo(expected);
    }

    [Theory]
    [InlineData("""ChildArray.Name == "b" """)]
    [InlineData("""ChildReadOnlyList.Name == "b" """)]
    [InlineData("""ChildCustom.Name == "b" """)]
    public void nested_path_through_a_collection_matches_for_every_collection_type(string input)
    {
        var rows = new[]
        {
            new Parent("first", [new Child("a")]),
            new Parent("second", [new Child("a"), new Child("b")]),
        };

        var result = rows.AsQueryable().ApplyQueryKitFilter(input).ToList();

        result.Select(x => x.Name).Should().Equal("second");
    }

    private static Tagged[] TaggedRows() =>
    [
        new("pancakes", ["breakfast", "sweet"]),
        new("stew", ["dinner"]),
        new("bread", ["bread", "Winner"]),
        new("water", []),
    ];

    public class TagList : List<string>
    {
        public TagList(IEnumerable<string> tags) : base(tags)
        {
        }
    }

    public class Tagged(string name, string[] tags)
    {
        public string Name { get; } = name;
        public List<string> List { get; } = [..tags];
        public IReadOnlyList<string> ReadOnlyList { get; } = tags.ToList();
        public IReadOnlyCollection<string> ReadOnlyCollection { get; } = new ReadOnlyCollection<string>(tags);
        public ICollection<string> Collection { get; } = tags.ToList();
        public HashSet<string> Set { get; } = [..tags];
        public string[] Array { get; } = tags;
        public IEnumerable<string> Enumerable { get; } = tags;
        public TagList Custom { get; } = new(tags);
    }

    public class Scored(string name, int[] scores)
    {
        public string Name { get; } = name;
        public int[] Array { get; } = scores;
        public IReadOnlyList<int> ReadOnlyList { get; } = scores.ToList();
    }

    public class NullableTagged
    {
        public string Name { get; init; } = null!;
        public List<string>? Tags { get; init; }
        public string[]? Labels { get; init; }
        public IReadOnlyList<string>? ReadOnlyTags { get; init; }
    }

    public class ChildList : List<Child>
    {
        public ChildList(IEnumerable<Child> children) : base(children)
        {
        }
    }

    public class Parent(string name, Child[] children)
    {
        public string Name { get; } = name;
        public Child[] ChildArray { get; } = children;
        public IReadOnlyList<Child> ChildReadOnlyList { get; } = children;
        public ChildList ChildCustom { get; } = new(children);
    }

    public class Child(string? name)
    {
        public string? Name { get; } = name;
    }
}
