namespace QueryKit.UnitTests;

using FluentAssertions;
using QueryKit.Operators;

public class ArithmeticOperatorTests
{
    [Theory]
    [InlineData("+", "+")]
    [InlineData("-", "-")]
    [InlineData("*", "*")]
    [InlineData("/", "/")]
    [InlineData("%", "%")]
    public void from_symbol_returns_the_operator_for_the_symbol(string symbol, string expectedSymbol)
    {
        var op = ArithmeticOperator.FromSymbol(symbol);

        op.Should().NotBeNull();
        op!.Symbol.Should().Be(expectedSymbol);
    }

    [Fact]
    public void from_symbol_returns_null_for_an_unknown_symbol()
    {
        var op = ArithmeticOperator.FromSymbol("^");

        op.Should().BeNull();
    }
}
