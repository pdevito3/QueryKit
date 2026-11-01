namespace QueryKit.UnitTests;

using Configuration;
using Exceptions;
using FluentAssertions;

public class QuotedListValueTests
{
    [Theory]
    [InlineData("""Title == "[a]" """, new[] { "[a]" })]
    [InlineData(""""Title == """[a]""" """", new[] { "[a]" })]
    [InlineData("""Title == "[a, b]" """, new[] { "[a, b]" })]
    [InlineData("""Title != "[a]" """, new[] { "abc", "[a, b]" })]
    [InlineData("""Title ==* "[A]" """, new[] { "[a]" })]
    [InlineData("""Title @=* "[A" """, new[] { "[a]", "[a, b]" })]
    [InlineData("""Title @=* "[a]" """, new[] { "[a]" })]
    [InlineData("""Title _= "[a" """, new[] { "[a]", "[a, b]" })]
    [InlineData("""Tags == "[a]" """, new[] { "[a]" })]
    public void quoted_value_in_list_form_is_a_string(string input, string[] expected)
    {
        var result = Rows().AsQueryable().ApplyQueryKitFilter(input).ToList();

        result.Select(x => x.Title).Should().BeEquivalentTo(expected);
    }

    [Theory]
    [InlineData("""Title ^^ "[abc]" """, new[] { "abc" })]
    [InlineData("""Title !^^ "[abc]" """, new[] { "[a]", "[a, b]" })]
    [InlineData("""Title ^^* "[ABC]" """, new[] { "abc" })]
    [InlineData(""""Title ^^ """["abc", "[a]"]""" """", new[] { "abc", "[a]" })]
    [InlineData("""Score ^^ "[1, 3]" """, new[] { "abc", "[a, b]" })]
    [InlineData("""Size ^^ "[Small]" """, new[] { "[a]" })]
    public void in_operator_reads_a_quoted_list_as_a_list(string input, string[] expected)
    {
        var result = Rows().AsQueryable().ApplyQueryKitFilter(input).ToList();

        result.Select(x => x.Title).Should().BeEquivalentTo(expected);
    }

    [Theory]
    [InlineData("""Score == "[1]" """, "[1]", "Int32", "Score")]
    [InlineData("""Size == "[Small]" """, "[Small]", "Size", "Size")]
    public void quoted_value_in_list_form_on_a_non_string_property_is_an_invalid_value(string input, string value, string type, string property)
    {
        var act = () => Rows().AsQueryable().ApplyQueryKitFilter(input).ToList();

        act.Should().Throw<ParsingException>()
            .WithMessage($"The value '{value}' is not a valid {type} for the filter property '{property}'.");
    }

    [Fact]
    public void custom_operation_gets_a_quoted_value_in_list_form_as_written()
    {
        var config = new QueryKitConfiguration(config =>
        {
            config.CustomOperation<Row>((x, op, value) => x.Title == (string)value).HasQueryName("titleIs");
        });
        var rows = new[] { new Row(@"[a\,b]", 1, Size.Big), new Row("[a,b]", 2, Size.Big) };

        var result = rows.AsQueryable().ApplyQueryKitFilter("""titleIs == "[a\,b]" """, config).ToList();

        result.Select(x => x.Title).Should().Equal(@"[a\,b]");
    }

    private static Row[] Rows() =>
    [
        new("abc", 1, Size.Big),
        new("[a]", 2, Size.Small),
        new("[a, b]", 3, Size.Big),
    ];

    public enum Size { Big, Small }

    public class Row(string title, int score, Size size)
    {
        public string Title { get; } = title;
        public int Score { get; } = score;
        public Size Size { get; } = size;
        public List<string> Tags { get; } = [title];
    }
}
