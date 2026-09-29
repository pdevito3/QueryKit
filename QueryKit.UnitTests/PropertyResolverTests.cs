namespace QueryKit.UnitTests;

using Configuration;
using Exceptions;
using FluentAssertions;
using WebApiTestProject.Entities;
using WebApiTestProject.Entities.Ingredients;

public class PropertyResolverTests
{
    [Fact]
    public void unknown_property_clause_is_removed_under_or()
    {
        var input = """Nope == "x" || Age > 100""";
        var config = new QueryKitConfiguration(config =>
        {
            config.AllowUnknownProperties = true;
        });

        var filterExpression = FilterParser.ParseFilter<TestingPerson>(input, config);

        filterExpression.ToDisplayString().Should().Be("x => (x.Age > 100)");
    }

    [Fact]
    public void unknown_property_clause_is_removed_under_and()
    {
        var input = """Age > 100 && Nope == "x" """;
        var config = new QueryKitConfiguration(config =>
        {
            config.AllowUnknownProperties = true;
        });

        var filterExpression = FilterParser.ParseFilter<TestingPerson>(input, config);

        filterExpression.ToDisplayString().Should().Be("x => (x.Age > 100)");
    }

    [Fact]
    public void prevented_property_clause_is_removed_under_or()
    {
        var input = """Rating == 1 || Age > 100""";
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<TestingPerson>(x => x.Rating).PreventFilter();
        });

        var filterExpression = FilterParser.ParseFilter<TestingPerson>(input, config);

        filterExpression.ToDisplayString().Should().Be("x => (x.Age > 100)");
    }

    [Fact]
    public void removed_clause_in_a_group_is_removed_from_the_group()
    {
        var input = """Title == "a" && (Nope == "x" || Age > 100)""";
        var config = new QueryKitConfiguration(config =>
        {
            config.AllowUnknownProperties = true;
        });

        var filterExpression = FilterParser.ParseFilter<TestingPerson>(input, config);

        filterExpression.ToDisplayString().Should().Be("""x => ((x.Title == "a") AndAlso (x.Age > 100))""");
    }

    [Fact]
    public void property_list_with_only_prevented_properties_is_removed()
    {
        var input = """(Title, FirstName) == "x" || Age > 100""";
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<TestingPerson>(x => x.Title).PreventFilter();
            config.Property<TestingPerson>(x => x.FirstName).PreventFilter();
        });

        var filterExpression = FilterParser.ParseFilter<TestingPerson>(input, config);

        filterExpression.ToDisplayString().Should().Be("x => (x.Age > 100)");
    }

    [Fact]
    public void prevented_property_in_arithmetic_removes_the_clause()
    {
        var input = """(Age + 0) > 10 || Title == "a" """;
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<TestingPerson>(x => x.Age).PreventFilter();
        });

        var filterExpression = FilterParser.ParseFilter<TestingPerson>(input, config);

        filterExpression.ToDisplayString().Should().Be("""x => (x.Title == "a")""");
    }

    [Fact]
    public void prevented_property_on_the_right_side_of_arithmetic_removes_the_clause()
    {
        var input = """(Age + 0) > (Rating * 2)""";
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<TestingPerson>(x => x.Rating).PreventFilter();
        });

        var filterExpression = FilterParser.ParseFilter<TestingPerson>(input, config);

        filterExpression.ToDisplayString().Should().Be("x => True");
    }

    [Fact]
    public void arithmetic_property_obeys_max_property_depth()
    {
        var input = """(Recipe.Rating + 0) > 1""";
        var config = new QueryKitConfiguration(config =>
        {
            config.MaxPropertyDepth = 0;
        });

        var act = () => FilterParser.ParseFilter<Ingredient>(input, config);

        act.Should().Throw<QueryKitPropertyDepthExceededException>();
    }

    [Fact]
    public void prevented_property_on_the_right_side_removes_the_clause()
    {
        var input = """FirstName == Title || Age > 100""";
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<TestingPerson>(x => x.Title).PreventFilter();
        });

        var filterExpression = FilterParser.ParseFilter<TestingPerson>(input, config);

        filterExpression.ToDisplayString().Should().Be("x => (x.Age > 100)");
    }

    [Fact]
    public void prevented_property_on_the_right_side_removes_the_clause_in_any_case()
    {
        var input = """FirstName == title || Age > 100""";
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<TestingPerson>(x => x.Title).PreventFilter();
        });

        var filterExpression = FilterParser.ParseFilter<TestingPerson>(input, config);

        filterExpression.ToDisplayString().Should().Be("x => (x.Age > 100)");
    }
}
