namespace QueryKit.UnitTests;

using Configuration;
using Exceptions;
using FluentAssertions;

public class AllowListTests
{
    [Theory]
    [InlineData("""Title == "abc" """, new[] { "abc" })]
    [InlineData("""title == "abc" """, new[] { "abc" })]
    [InlineData("""boss == "ann" """, new[] { "abc" })]
    [InlineData("""Owner.Name == "ann" """, new[] { "abc" })]
    [InlineData("""Parts.Name == "bolt" """, new[] { "xyz" })]
    [InlineData("""shout == "ABC" """, new[] { "abc" })]
    [InlineData("""titleIs == "xyz" """, new[] { "xyz" })]
    [InlineData("""Title == "abc" || Count < 2""", new[] { "abc", "xyz" })]
    public void filter_on_an_allowed_property_filters(string input, string[] expected)
    {
        var result = Rows().ApplyQueryKitFilter(input, AllowListConfig()).ToList();

        result.Select(x => x.Title).Should().BeEquivalentTo(expected);
    }

    [Theory]
    [InlineData("""Secret == "x" """, "Secret")]
    [InlineData("""Hidden == "h" """, "Hidden")]
    [InlineData("""StartsAt.Ticks > 0""", "StartsAt.Ticks")]
    [InlineData("""Title.Length > 2""", "Title.Length")]
    [InlineData("""Owner == null""", "Owner")]
    [InlineData("""Owner.Id > 0""", "Owner.Id")]
    [InlineData("""Title == Owner.Id""", "Owner.Id")]
    [InlineData("""(Count + Secret.Length) > 2""", "Secret.Length")]
    [InlineData("""(Title, Secret) @= "x" """, "Secret")]
    public void filter_on_a_property_outside_the_allow_list_is_unknown(string input, string property)
    {
        var act = () => Rows().ApplyQueryKitFilter(input, AllowListConfig()).ToList();

        act.Should().Throw<UnknownFilterPropertyException>()
            .Which.PropertyName.Should().Be(property);
    }

    [Fact]
    public void one_word_outside_the_allow_list_on_the_right_side_is_text()
    {
        var rows = new[] { new Row { Title = "Secret", Secret = "abc" }, new Row { Title = "abc", Secret = "abc" } }.AsQueryable();

        var result = rows.ApplyQueryKitFilter("Title == Secret", AllowListConfig()).ToList();

        result.Select(x => x.Title).Should().Equal("Secret");
    }

    [Fact]
    public void property_outside_the_allow_list_follows_the_ignored_clause_behavior_when_unknown_properties_are_allowed()
    {
        var ignore = AllowListConfig(config => config.AllowUnknownProperties = true);
        var refuse = AllowListConfig(config =>
        {
            config.AllowUnknownProperties = true;
            config.IgnoredClauseBehavior = IgnoredClauseBehavior.Throw;
        });

        Rows().ApplyQueryKitFilter("""Secret == "x" || Title == "abc" """, ignore).ToList()
            .Select(x => x.Title).Should().BeEquivalentTo("abc", "xyz");
        var act = () => Rows().ApplyQueryKitFilter("""Secret == "x" """, refuse).ToList();
        act.Should().Throw<UnknownFilterPropertyException>().Which.PropertyName.Should().Be("Secret");
    }

    [Theory]
    [InlineData("Title desc", new[] { "xyz", "abc" })]
    [InlineData("boss desc", new[] { "xyz", "abc" })]
    [InlineData("Count desc, Title", new[] { "abc", "xyz" })]
    public void sort_on_an_allowed_property_sorts(string input, string[] expected)
    {
        var result = Rows().ApplyQueryKitSort(input, AllowListConfig()).ToList();

        result.Select(x => x.Title).Should().Equal(expected);
    }

    [Theory]
    [InlineData("Secret desc", "Secret")]
    [InlineData("Hidden", "Hidden")]
    [InlineData("Owner.Id", "Owner.Id")]
    public void sort_on_a_property_outside_the_allow_list_is_unknown(string input, string property)
    {
        var act = () => Rows().ApplyQueryKitSort(input, AllowListConfig()).ToList();

        act.Should().Throw<SortParsingException>()
            .Which.PropertyName.Should().Be(property);
    }

    [Fact]
    public void sort_on_a_custom_operation_named_like_an_unlisted_member_is_unknown()
    {
        var config = AllowListConfig(config => config.CustomOperation<Row>((x, op, value) => x.Count > 0).HasQueryName("Secret"));

        var act = () => Rows().ApplyQueryKitSort("Secret desc", config).ToList();

        act.Should().Throw<SortParsingException>().Which.PropertyName.Should().Be("Secret");
    }

    [Fact]
    public void sort_on_a_member_of_a_listed_collection_is_unknown()
    {
        var act = () => Rows().ApplyQueryKitSort("Parts.Count", AllowListConfig()).ToList();

        act.Should().Throw<SortParsingException>().Which.PropertyName.Should().Be("Parts.Count");
    }

    [Fact]
    public void without_the_allow_list_every_property_is_available()
    {
        var config = new QueryKitConfiguration(config => config.Property<Row>(x => x.Title!));

        var result = Rows().ApplyQueryKitFilter("""Secret == "y" """, config).ToList();

        result.Select(x => x.Title).Should().Equal("xyz");
    }

    private static QueryKitConfiguration AllowListConfig(Action<QueryKitSettings>? configure = null) => new(config =>
    {
        config.UseAllowList = true;
        config.Property<Row>(x => x.Title!);
        config.Property<Row>(x => x.Count).PreventSort();
        config.Property<Row>(x => x.Owner.Name).HasQueryName("boss");
        config.Property<Row>(x => x.Parts.Select(p => p.Name));
        config.DerivedProperty<Row>(x => x.Title!.ToUpper()).HasQueryName("shout");
        config.CustomOperation<Row>((x, op, value) => x.Title == (string)value).HasQueryName("titleIs");
        configure?.Invoke(config);
    });

    private static IQueryable<Row> Rows() => new[]
    {
        new Row { Title = "abc", Secret = "x", Count = 3, Owner = new Owner { Id = 1, Name = "ann" } },
        new Row { Title = "xyz", Secret = "y", Count = 1, Owner = new Owner { Id = 2, Name = "bob" }, Parts = [new Part { Name = "bolt" }] },
    }.AsQueryable();

    public class Row
    {
        public string? Title { get; set; }
        public string? Secret { get; set; }
        public int Count { get; set; }
        public DateTimeOffset StartsAt { get; set; }
        public Owner Owner { get; set; } = new();
        public List<Part> Parts { get; set; } = [];
        private string? Hidden { get; set; } = "h";
    }

    public class Owner
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    public class Part
    {
        public string Name { get; set; } = "";
    }
}
