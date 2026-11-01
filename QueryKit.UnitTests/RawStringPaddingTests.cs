namespace QueryKit.UnitTests;

using FluentAssertions;

public class RawStringPaddingTests
{
    [Theory]
    [InlineData(""""Title == """ "hi" """ """", new[] { "\"hi\"" })]
    [InlineData(""""Title _= """ "hi" now """ """", new[] { "\"hi\" now" })]
    [InlineData(""""Title _-= """ say "hi" """ """", new[] { "say \"hi\"" })]
    [InlineData(""""Title @= """ "hi" """ """", new[] { "\"hi\"", "\"hi\" now", "say \"hi\"" })]
    [InlineData(""""Title != """ "hi" """ """", new[] { "hi", "\"hi\" now", "say \"hi\"", " x ", "x", " x", "   ", "\tx\t", "\"a\"", "b" })]
    [InlineData(""""Title == """ x """ """", new[] { "x" })]
    [InlineData(""""Title == """  x  """ """", new[] { " x " })]
    [InlineData(""""Title == """ x""" """", new[] { " x" })]
    [InlineData(""""Title == """   """ """", new[] { "   " })]
    [InlineData("Title == \"\"\"\tx\t\"\"\"", new[] { "\tx\t" })]
    [InlineData("""""Title == """"hi"""" """"", new[] { "hi" })]
    [InlineData(""""Title ^^ [""" "a" """, "b"]"""", new[] { "\"a\"", "b" })]
    public void raw_string_drops_one_space_of_padding_from_each_end(string input, string[] expected)
    {
        var result = Rows().AsQueryable().ApplyQueryKitFilter(input).ToList();

        result.Select(x => x.Title).Should().BeEquivalentTo(expected);
    }

    private static Row[] Rows() =>
    [
        new("\"hi\""),
        new("hi"),
        new("\"hi\" now"),
        new("say \"hi\""),
        new(" x "),
        new("x"),
        new(" x"),
        new("   "),
        new("\tx\t"),
        new("\"a\""),
        new("b"),
    ];

    public class Row(string title)
    {
        public string Title { get; } = title;
    }
}
