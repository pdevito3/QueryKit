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

    [Fact]
    public void prevented_property_in_a_list_is_skipped_in_any_case()
    {
        var input = """(title, FirstName) == "x" """;
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<TestingPerson>(x => x.Title).PreventFilter();
        });

        var filterExpression = FilterParser.ParseFilter<TestingPerson>(input, config);

        filterExpression.ToDisplayString().Should().Be("""x => (x.FirstName == "x")""");
    }

    [Fact]
    public void prevented_property_removes_the_clause_in_any_case()
    {
        var input = """title == "x" || Age > 100""";
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<TestingPerson>(x => x.Title).PreventFilter();
        });

        var filterExpression = FilterParser.ParseFilter<TestingPerson>(input, config);

        filterExpression.ToDisplayString().Should().Be("x => (x.Age > 100)");
    }

    [Fact]
    public void prevented_property_with_a_query_name_removes_the_clause_when_written_by_its_member_name_in_any_case()
    {
        var input = """title == "x" || Age > 100""";
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<TestingPerson>(x => x.Title).HasQueryName("t").PreventFilter();
        });

        var filterExpression = FilterParser.ParseFilter<TestingPerson>(input, config);

        filterExpression.ToDisplayString().Should().Be("x => (x.Age > 100)");
    }

    [Fact]
    public void property_in_a_list_uses_its_case_insensitive_mode_in_any_case()
    {
        var input = """(title) @=* "x" """;
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<TestingPerson>(x => x.Title).HasCaseInsensitiveMode(CaseInsensitiveMode.Upper);
        });

        var filterExpression = FilterParser.ParseFilter<TestingPerson>(input, config);

        filterExpression.ToDisplayString().Should().Contain("ToUpper()");
    }

    [Fact]
    public void prevented_sort_property_is_skipped_in_any_case()
    {
        var input = "title, Age desc";
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<TestingPerson>(x => x.Title).PreventSort();
        });

        var sortExpressions = SortParser.ParseSort<TestingPerson>(input, config);

        sortExpressions.Should().ContainSingle();
        sortExpressions[0].Expression!.ToString().Should().Be("x => Convert(x.Age, Object)");
    }

    [Fact]
    public void prevented_sort_property_with_a_query_name_is_skipped_when_written_by_its_member_name()
    {
        var input = "title desc";
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<TestingPerson>(x => x.Title).HasQueryName("t").PreventSort();
        });

        var sortExpressions = SortParser.ParseSort<TestingPerson>(input, config);

        sortExpressions.Should().BeEmpty();
    }

    [Fact]
    public void prevented_derived_property_removes_the_clause()
    {
        var input = """full == "x" || Age > 100""";
        var config = new QueryKitConfiguration(config =>
        {
            config.DerivedProperty<TestingPerson>(x => x.FirstName + " " + x.LastName).HasQueryName("full").PreventFilter();
        });

        var filterExpression = FilterParser.ParseFilter<TestingPerson>(input, config);

        filterExpression.ToDisplayString().Should().Be("x => (x.Age > 100)");
    }

    [Fact]
    public void prevented_derived_property_in_a_list_is_skipped()
    {
        var input = """(full, FirstName) == "x" """;
        var config = new QueryKitConfiguration(config =>
        {
            config.DerivedProperty<TestingPerson>(x => x.FirstName + " " + x.LastName).HasQueryName("full").PreventFilter();
        });

        var filterExpression = FilterParser.ParseFilter<TestingPerson>(input, config);

        filterExpression.ToDisplayString().Should().Be("""x => (x.FirstName == "x")""");
    }

    [Fact]
    public void prevented_custom_operation_removes_the_clause()
    {
        var input = """adult == true || Age > 100""";
        var config = new QueryKitConfiguration(config =>
        {
            config.CustomOperation<TestingPerson>((x, op, value) => x.Age > 17).HasQueryName("adult").PreventFilter();
        });

        var filterExpression = FilterParser.ParseFilter<TestingPerson>(input, config);

        filterExpression.ToDisplayString().Should().Be("x => (x.Age > 100)");
    }

    [Fact]
    public void prevented_derived_sort_property_is_skipped()
    {
        var input = "full desc, Age";
        var config = new QueryKitConfiguration(config =>
        {
            config.DerivedProperty<TestingPerson>(x => x.FirstName + " " + x.LastName).HasQueryName("full").PreventSort();
        });

        var sortExpressions = SortParser.ParseSort<TestingPerson>(input, config);

        sortExpressions.Should().ContainSingle();
        sortExpressions[0].Expression!.ToString().Should().Be("x => Convert(x.Age, Object)");
    }
}
