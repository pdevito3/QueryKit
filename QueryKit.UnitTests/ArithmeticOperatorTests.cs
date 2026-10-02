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
#pragma warning disable CS0618 // FromSymbol stays for v1.14.2 compatibility
        var op = ArithmeticOperator.FromSymbol(symbol);
#pragma warning restore CS0618

        op.Should().NotBeNull();
        op!.Symbol.Should().Be(expectedSymbol);
    }

    [Fact]
    public void from_symbol_returns_null_for_an_unknown_symbol()
    {
#pragma warning disable CS0618 // FromSymbol stays for v1.14.2 compatibility
        var op = ArithmeticOperator.FromSymbol("^");
#pragma warning restore CS0618

        op.Should().BeNull();
    }
}
