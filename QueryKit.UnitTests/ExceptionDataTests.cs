namespace QueryKit.UnitTests;

using Exceptions;
using FluentAssertions;
using Operators;

public class ExceptionDataTests
{
    [Fact]
    public void unknown_filter_property_names_the_property()
    {
        var act = () => Rows().ApplyQueryKitFilter("""Nope == "a" """).ToList();

        act.Should().Throw<UnknownFilterPropertyException>()
            .Which.PropertyName.Should().Be("Nope");
    }

    [Fact]
    public void value_that_does_not_convert_names_the_property_type_and_value()
    {
        var act = () => Rows().ApplyQueryKitFilter("""Count == "x" """).ToList();

        var exception = act.Should().Throw<ParsingException>().Which;
        exception.PropertyName.Should().Be("Count");
        exception.TargetType.Should().Be(typeof(int));
        exception.Value.Should().Be("x");
        exception.Line.Should().BeNull();
        exception.Column.Should().BeNull();
    }

    [Fact]
    public void syntax_failure_gives_the_line_and_column()
    {
        var act = () => Rows().ApplyQueryKitFilter("Title ==").ToList();

        var exception = act.Should().Throw<ParsingException>().Which;
        exception.Line.Should().Be(1);
        exception.Column.Should().Be(9);
        exception.Message.Should().EndWith("Failed at Line 1, Column 9.");
        exception.PropertyName.Should().BeNull();
        exception.TargetType.Should().BeNull();
        exception.Value.Should().BeNull();
    }

    [Theory]
    [InlineData("""Title ^^ "a" """, "^^", QueryKitParsingReason.NeedsList)]
    [InlineData("""Title !^^* "a" """, "!^^*", QueryKitParsingReason.NeedsList)]
    [InlineData("Title @=* null", "@=*", QueryKitParsingReason.RefusesNull)]
    [InlineData("Title ==* null", "==*", QueryKitParsingReason.RefusesNull)]
    [InlineData("""Title ^$ "a" """, "^$", QueryKitParsingReason.NeedsCollection)]
    public void operator_failure_gives_the_operator_and_the_reason(string input, string op, QueryKitParsingReason reason)
    {
        var act = () => Rows().ApplyQueryKitFilter(input).ToList();

        var exception = act.Should().Throw<QueryKitParsingException>().Which;
        exception.Operator.Should().Be(op);
        exception.Reason.Should().Be(reason);
    }

    [Fact]
    public void unsupported_logical_operator_gives_the_operator()
    {
        var act = () => LogicalOperator.GetByOperatorString("xor");

        var exception = act.Should().Throw<QueryKitParsingException>().Which;
        exception.Operator.Should().Be("xor");
        exception.Reason.Should().Be(QueryKitParsingReason.UnsupportedOperator);
    }

    [Fact]
    public void invalid_sort_direction_gives_the_direction()
    {
        var act = () => Rows().ApplyQueryKitSort("Title sideways").ToList();

        var exception = act.Should().Throw<QueryKitParsingException>().Which;
        exception.Operator.Should().Be("sideways");
        exception.Reason.Should().Be(QueryKitParsingReason.UnsupportedOperator);
    }

    [Theory]
    [InlineData("Nope desc", "Nope")]
    [InlineData("Tags", "Tags")]
    public void sort_failure_names_the_property(string input, string property)
    {
        var act = () => Rows().ApplyQueryKitSort(input).ToList();

        act.Should().Throw<SortParsingException>()
            .Which.PropertyName.Should().Be(property);
    }

    [Fact]
    public void message_constructor_has_no_reason_or_operator()
    {
        var exception = new QueryKitParsingException("custom");

        exception.Reason.Should().Be(QueryKitParsingReason.Other);
        exception.Operator.Should().BeNull();
    }

    private static IQueryable<Row> Rows() => new[] { new Row { Title = "a", Count = 1, Tags = ["x"] } }.AsQueryable();

    public class Row
    {
        public string? Title { get; set; }
        public int Count { get; set; }
        public List<string> Tags { get; set; } = [];
    }
}
