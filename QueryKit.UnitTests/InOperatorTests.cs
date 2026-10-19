namespace QueryKit.UnitTests;

using Exceptions;
using FluentAssertions;

public class InOperatorTests
{
    [Theory]
    [InlineData("""Name ^^ "a" """, "^^")]
    [InlineData("""Name !^^ "a" """, "!^^")]
    [InlineData("""Name ^^* "a" """, "^^*")]
    [InlineData("""Name !^^* "a" """, "!^^*")]
    [InlineData("Score ^^ 5", "^^")]
    [InlineData("""Size ^^ "Big" """, "^^")]
    [InlineData("""Id ^^ "8b1f0a62-4a5e-4c52-9f39-2a8d8a1c9e11" """, "^^")]
    [InlineData("""List ^^ "dinner" """, "^^")]
    public void in_operator_with_one_value_throws_querykit_parsing_exception(string input, string op)
    {
        var act = () => Rows().AsQueryable().ApplyQueryKitFilter(input).ToList();

        act.Should().Throw<QueryKitParsingException>()
            .WithMessage($"""The {op} operator needs a list value, for example ["a", "b"].""");
    }

    [Theory]
    [InlineData("""Name ^^ ["pancakes", "stew"] """, new[] { "pancakes", "stew" })]
    [InlineData("""Name !^^ ["pancakes", "stew"] """, new[] { "bread", "water" })]
    [InlineData("""Name ^^* ["PANCAKES"] """, new[] { "pancakes" })]
    [InlineData("Score ^^ [1, 3]", new[] { "pancakes", "bread" })]
    public void in_operator_with_a_list_on_a_scalar_property_matches(string input, string[] expected)
    {
        var result = Rows().AsQueryable().ApplyQueryKitFilter(input).ToList();

        result.Select(x => x.Name).Should().BeEquivalentTo(expected);
    }

    private static readonly (string Operation, string[] Expected)[] CollectionCases =
    [
        ("""^^ ["dinner", "sweet"] """, ["pancakes", "stew"]),
        ("""!^^ ["dinner", "sweet"] """, ["bread", "water"]),
        ("""%^^ ["dinner", "breakfast", "sweet"] """, ["pancakes", "stew", "water"]),
        ("""^^* ["WINNER"] """, ["bread"]),
        ("""!^^* ["WINNER"] """, ["pancakes", "stew", "water"]),
    ];

    public static TheoryData<string, string, string[]> CollectionFilters()
    {
        var data = new TheoryData<string, string, string[]>();
        foreach (var property in new[] { nameof(Row.List), nameof(Row.ReadOnlyList), nameof(Row.Set), nameof(Row.Array), "Children.Name" })
        foreach (var (operation, expected) in CollectionCases)
            data.Add(property, operation, expected);
        return data;
    }

    [Theory]
    [MemberData(nameof(CollectionFilters))]
    public void in_operator_on_a_collection_matches_when_any_element_is_in_the_list(string property, string operation, string[] expected)
    {
        var result = Rows().AsQueryable().ApplyQueryKitFilter($"{property} {operation}").ToList();

        result.Select(x => x.Name).Should().BeEquivalentTo(expected);
    }

    [Theory]
    [InlineData("Numbers ^^ [2, 9]", new[] { "pancakes", "stew" })]
    [InlineData("Numbers !^^ [2, 9]", new[] { "bread", "water" })]
    public void in_operator_on_a_number_collection_matches(string input, string[] expected)
    {
        var result = Rows().AsQueryable().ApplyQueryKitFilter(input).ToList();

        result.Select(x => x.Name).Should().BeEquivalentTo(expected);
    }

    private static Row[] Rows() =>
    [
        new("pancakes", 1, ["breakfast", "sweet"], [1, 2]),
        new("stew", 2, ["dinner"], [9]),
        new("bread", 3, ["bread", "Winner"], [3]),
        new("water", 4, [], []),
    ];

    public enum Size { Big, Small }

    public class Row(string name, int score, string[] tags, int[] numbers)
    {
        public Guid Id { get; } = Guid.NewGuid();
        public string Name { get; } = name;
        public int Score { get; } = score;
        public Size Size { get; } = Size.Big;
        public List<string> List { get; } = [..tags];
        public IReadOnlyList<string> ReadOnlyList { get; } = tags;
        public HashSet<string> Set { get; } = [..tags];
        public string[] Array { get; } = tags;
        public List<int> Numbers { get; } = [..numbers];
        public List<Child> Children { get; } = tags.Select(x => new Child(x)).ToList();
    }

    public class Child(string name)
    {
        public string Name { get; } = name;
    }
}
