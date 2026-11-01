namespace QueryKit.UnitTests;

using Configuration;
using Exceptions;
using FluentAssertions;

public class IgnoredClauseThrowTests
{
    [Theory]
    [InlineData("""Secret == "x" """, "Secret")]
    [InlineData("""Secret == "x" || Title == "abc" """, "Secret")]
    [InlineData("""Title == "abc" && Secret == "x" """, "Secret")]
    [InlineData("""hidden == "x" """, "hidden")]
    [InlineData("""Title == Secret""", "Secret")]
    [InlineData("""(Count + 1) > 2""", "Count")]
    [InlineData("""(Title, Secret) @= "x" """, "Secret")]
    [InlineData("""shout == "ABC" """, "shout")]
    [InlineData("""titleIs == "abc" """, "titleIs")]
    public void clause_on_a_prevented_property_throws_and_names_the_property(string input, string property)
    {
        var act = () => Rows().ApplyQueryKitFilter(input, ThrowConfig()).ToList();

        act.Should().Throw<PreventedPropertyException>()
            .Which.PropertyName.Should().Be(property);
    }

    [Theory]
    [InlineData("""Nope == "x" """, "Nope")]
    [InlineData("""(Nope + 1) > 2""", "Nope")]
    [InlineData("""(Title, Nope) @= "x" """, "Nope")]
    public void clause_on_an_unknown_property_throws_even_when_unknown_properties_are_allowed(string input, string property)
    {
        var act = () => Rows().ApplyQueryKitFilter(input, ThrowConfig()).ToList();

        act.Should().Throw<UnknownFilterPropertyException>()
            .Which.PropertyName.Should().Be(property);
    }

    [Fact]
    public void clause_on_a_prevented_property_throws_when_unknown_properties_are_not_allowed()
    {
        var config = new QueryKitConfiguration(config =>
        {
            config.IgnoredClauseBehavior = IgnoredClauseBehavior.Throw;
            config.Property<Row>(x => x.Secret!).PreventFilter();
        });

        var act = () => Rows().ApplyQueryKitFilter("""Title == "abc" || Secret == "x" """, config).ToList();

        act.Should().Throw<PreventedPropertyException>().Which.PropertyName.Should().Be("Secret");
    }

    [Fact]
    public void clause_on_an_allowed_property_filters()
    {
        var result = Rows().ApplyQueryKitFilter("""Title == "abc" || Title @= "y" """, ThrowConfig()).ToList();

        result.Select(x => x.Title).Should().BeEquivalentTo("abc", "xyz");
    }

    [Theory]
    [InlineData("Secret desc", "Secret")]
    [InlineData("Title, hidden", "hidden")]
    public void sort_on_a_prevented_property_throws_and_names_the_property(string input, string property)
    {
        var act = () => Rows().ApplyQueryKitSort(input, ThrowConfig()).ToList();

        act.Should().Throw<PreventedPropertyException>()
            .Which.PropertyName.Should().Be(property);
    }

    [Fact]
    public void sort_on_a_property_that_only_prevents_filter_sorts()
    {
        var result = Rows().ApplyQueryKitSort("Count desc", ThrowConfig()).ToList();

        result.Select(x => x.Title).Should().Equal("xyz", "abc");
    }

    [Fact]
    public void sort_on_a_prevented_property_is_skipped_without_throw()
    {
        var config = new QueryKitConfiguration(config => config.Property<Row>(x => x.Secret!).PreventSort());

        var result = Rows().ApplyQueryKitSort("Secret desc, Title desc", config).ToList();

        result.Select(x => x.Title).Should().Equal("xyz", "abc");
    }

    private static QueryKitConfiguration ThrowConfig() => new(config =>
    {
        config.IgnoredClauseBehavior = IgnoredClauseBehavior.Throw;
        config.AllowUnknownProperties = true;
        config.Property<Row>(x => x.Secret!).PreventFilter().PreventSort();
        config.Property<Row>(x => x.Hidden!).HasQueryName("hidden").PreventFilter().PreventSort();
        config.Property<Row>(x => x.Count).PreventFilter();
        config.DerivedProperty<Row>(x => x.Title!.ToUpper()).HasQueryName("shout").PreventFilter();
        config.CustomOperation<Row>((x, op, value) => x.Title == (string)value).HasQueryName("titleIs").PreventFilter();
    });

    private static IQueryable<Row> Rows() => new[]
    {
        new Row { Title = "abc", Secret = "x", Hidden = "h", Count = 1 },
        new Row { Title = "xyz", Secret = "y", Hidden = "i", Count = 3 },
    }.AsQueryable();

    public class Row
    {
        public string? Title { get; set; }
        public string? Secret { get; set; }
        public string? Hidden { get; set; }
        public int Count { get; set; }
    }
}
