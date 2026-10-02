namespace QueryKit.UnitTests;

using Configuration;
using Exceptions;
using FluentAssertions;
using WebApiTestProject.Entities;

// v1.14.2 read only the first word of a query name with a space or a hyphen, and threw
// UnknownFilterPropertyException for that word. A filter with such a query name that fails
// still throws the v1.14.2 exception. A filter that v1.14.2 rejected can now succeed.
public class QueryNameOverUnknownTests
{
    private static readonly QueryKitConfiguration Config = new(config =>
    {
        config.DerivedProperty<TestingPerson>(x => (x.Age * 2)!).HasQueryName("double age");
        config.DerivedProperty<TestingPerson>(x => x.FirstName + " " + x.LastName).HasQueryName("full-name");
        config.CustomOperation<TestingPerson>((x, op, value) => x.Age > 17).HasQueryName("is adult");
    });

    [Theory]
    [InlineData("double age > 6", "double")]
    [InlineData("DOUBLE AGE > 6", "DOUBLE")]
    [InlineData("""full-name == "Ann Lee" && Title ==""", "full")]
    [InlineData("is adult", "is")]
    [InlineData("is adult ==", "is")]
    [InlineData("""is adult == true && Title ==""", "is")]
    [InlineData("""is adult == true && Nope == "x" """, "is")]
    [InlineData("""is adult == true && Age > "x" """, "is")]
    [InlineData("""((is adult == true)) || (Title == "a" && is adult ==)""", "is")]
    public void failed_filter_with_a_query_name_throws_unknown_property_for_the_first_word(string input, string unknownProperty)
    {
        var act = () => FilterParser.ParseFilter<TestingPerson>(input, Config);

        act.Should().ThrowExactly<UnknownFilterPropertyException>().WithMessage($"*'{unknownProperty}'*");
    }

    [Fact]
    public void failure_before_the_query_name_throws_parsing_exception()
    {
        var act = () => FilterParser.ParseFilter<TestingPerson>("""Title == && is adult == true""", Config);

        act.Should().ThrowExactly<ParsingException>();
    }

    [Fact]
    public void failure_before_the_query_name_throws_its_own_format_exception()
    {
        var act = () => FilterParser.ParseFilter<TestingPerson>("""Age > "x" && is adult == true""", Config);

        act.Should().ThrowExactly<FormatException>();
    }

    [Fact]
    public void failure_before_the_query_name_throws_its_own_unknown_property()
    {
        var act = () => FilterParser.ParseFilter<TestingPerson>("""Nope == "x" && is adult == true""", Config);

        act.Should().ThrowExactly<UnknownFilterPropertyException>().WithMessage("*'Nope'*");
    }

    [Fact]
    public void filter_with_a_query_name_still_succeeds()
    {
        var filterExpression = FilterParser.ParseFilter<TestingPerson>("""is adult == true && Title == "x" """, Config);

        filterExpression.ToDisplayString().Should().Be("""x => (Invoke((entity, op, value) => (Convert(entity, TestingPerson).Age > Convert(17, Nullable`1)), Convert(x, Object), ==, True) AndAlso (x.Title == "x"))""");
    }
}
