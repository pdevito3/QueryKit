namespace QueryKit.UnitTests;

using FluentAssertions;
using QueryKit.Operators;

public class ArithmeticOperatorTests
{
    [Fact]
    public void from_symbol_is_not_part_of_the_public_api()
    {
        typeof(ArithmeticOperator).GetMethod("FromSymbol").Should().BeNull();
    }
}
