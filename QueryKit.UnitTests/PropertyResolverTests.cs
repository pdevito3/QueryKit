namespace QueryKit.UnitTests;

using Configuration;
using Exceptions;
using FluentAssertions;
using WebApiTestProject.Entities;
using WebApiTestProject.Entities.Ingredients;
using WebApiTestProject.Entities.Recipes;

public class PropertyResolverTests
{
    [Fact]
    public void unknown_property_clause_is_true_equals_true_by_default()
    {
        var input = """Nope == "x" || Age > 100""";
        var config = new QueryKitConfiguration(config =>
        {
            config.AllowUnknownProperties = true;
        });

        var filterExpression = FilterParser.ParseFilter<TestingPerson>(input, config);

        filterExpression.ToDisplayString().Should().Be("x => ((True == True) OrElse (x.Age > 100))");
    }

    [Fact]
    public void prevented_property_clause_is_true_equals_true_by_default()
    {
        var input = """FirstName == "Ann" || Title == "s" """;
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<TestingPerson>(x => x.Title!).PreventFilter();
        });

        var filterExpression = FilterParser.ParseFilter<TestingPerson>(input, config);

        filterExpression.ToDisplayString().Should().Be("""x => ((x.FirstName == "Ann") OrElse (True == True))""");
    }

    [Fact]
    public void prevented_property_is_found_by_its_path_when_another_query_name_matches_that_path()
    {
        var input = """nick == "Ann" """;
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<TestingPerson>(x => x.Title!).HasQueryName("firstname");
            config.Property<TestingPerson>(x => x.FirstName!).HasQueryName("nick").PreventFilter();
        });

        var filterExpression = FilterParser.ParseFilter<TestingPerson>(input, config);

        filterExpression.ToDisplayString().Should().Be("x => (True == True)");
    }

    [Fact]
    public void property_is_not_prevented_by_another_property_whose_query_name_matches_its_path()
    {
        var input = """nick == "Ann" || Age > 100""";
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<TestingPerson>(x => x.Title!).HasQueryName("firstname").PreventFilter();
            config.Property<TestingPerson>(x => x.FirstName!).HasQueryName("nick");
        });

        var filterExpression = FilterParser.ParseFilter<TestingPerson>(input, config);

        filterExpression.ToDisplayString().Should().Be("""x => ((x.FirstName == "Ann") OrElse (x.Age > 100))""");
    }

    [Fact]
    public void property_list_with_only_prevented_properties_is_true_by_default()
    {
        var input = """(Title, FirstName) == "x" || Age > 100""";
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<TestingPerson>(x => x.Title!).PreventFilter();
            config.Property<TestingPerson>(x => x.FirstName!).PreventFilter();
        });

        var filterExpression = FilterParser.ParseFilter<TestingPerson>(input, config);

        filterExpression.ToDisplayString().Should().Be("x => (True OrElse (x.Age > 100))");
    }

    [Fact]
    public void unknown_property_clause_is_true_equals_true_under_and()
    {
        var input = """Age > 100 && Nope == "x" """;
        var config = new QueryKitConfiguration(config =>
        {
            config.IgnoredClauseBehavior = IgnoredClauseBehavior.ReplaceWithTrue;
            config.AllowUnknownProperties = true;
        });

        var filterExpression = FilterParser.ParseFilter<TestingPerson>(input, config);

        filterExpression.ToDisplayString().Should().Be("x => ((x.Age > 100) AndAlso (True == True))");
    }

    [Fact]
    public void ignored_clause_in_a_group_is_true_equals_true_in_the_group()
    {
        var input = """Title == "a" && (Nope == "x" || Age > 100)""";
        var config = new QueryKitConfiguration(config =>
        {
            config.IgnoredClauseBehavior = IgnoredClauseBehavior.ReplaceWithTrue;
            config.AllowUnknownProperties = true;
        });

        var filterExpression = FilterParser.ParseFilter<TestingPerson>(input, config);

        filterExpression.ToDisplayString().Should().Be(
            """x => ((x.Title == "a") AndAlso ((True == True) OrElse (x.Age > 100)))""");
    }

    [Fact]
    public void unknown_property_clause_is_removed_under_or()
    {
        var input = """Nope == "x" || Age > 100""";
        var config = new QueryKitConfiguration(config =>
        {
            config.IgnoredClauseBehavior = IgnoredClauseBehavior.Remove;
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
            config.IgnoredClauseBehavior = IgnoredClauseBehavior.Remove;
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
            config.IgnoredClauseBehavior = IgnoredClauseBehavior.Remove;
            config.Property<TestingPerson>(x => x.Rating!).PreventFilter();
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
            config.IgnoredClauseBehavior = IgnoredClauseBehavior.Remove;
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
            config.IgnoredClauseBehavior = IgnoredClauseBehavior.Remove;
            config.Property<TestingPerson>(x => x.Title!).PreventFilter();
            config.Property<TestingPerson>(x => x.FirstName!).PreventFilter();
        });

        var filterExpression = FilterParser.ParseFilter<TestingPerson>(input, config);

        filterExpression.ToDisplayString().Should().Be("x => (x.Age > 100)");
    }

    [Fact]
    public void prevented_property_in_arithmetic_is_still_filtered()
    {
        var input = """(Age + 0) > 10 || Title == "a" """;
        var config = new QueryKitConfiguration(config =>
        {
            config.IgnoredClauseBehavior = IgnoredClauseBehavior.Remove;
            config.Property<TestingPerson>(x => x.Age!).PreventFilter();
        });

        var filterExpression = FilterParser.ParseFilter<TestingPerson>(input, config);

        filterExpression.ToDisplayString().Should().Be("""x => (((x.Age + Convert(0, Nullable`1)) > Convert(10, Nullable`1)) OrElse (x.Title == "a"))""");
    }

    [Fact]
    public void prevented_property_on_the_right_side_of_arithmetic_is_still_filtered()
    {
        var input = """(Age + 0) > (Rating * 2)""";
        var config = new QueryKitConfiguration(config =>
        {
            config.IgnoredClauseBehavior = IgnoredClauseBehavior.Remove;
            config.Property<TestingPerson>(x => x.Rating!).PreventFilter();
        });

        var filterExpression = FilterParser.ParseFilter<TestingPerson>(input, config);

        filterExpression.ToDisplayString().Should().Be("x => (Convert((x.Age + Convert(0, Nullable`1)), Nullable`1) > (x.Rating * Convert(2, Nullable`1)))");
    }

    [Fact]
    public void arithmetic_property_skips_max_property_depth()
    {
        var input = """(Recipe.Rating + 0) > 1""";
        var config = new QueryKitConfiguration(config =>
        {
            config.MaxPropertyDepth = 0;
        });

        var act = () => FilterParser.ParseFilter<Ingredient>(input, config);

        act.Should().NotThrow();
    }

    [Fact]
    public void prevented_property_on_the_right_side_is_still_compared()
    {
        var input = """FirstName == Title || Age > 100""";
        var config = new QueryKitConfiguration(config =>
        {
            config.IgnoredClauseBehavior = IgnoredClauseBehavior.Remove;
            config.Property<TestingPerson>(x => x.Title!).PreventFilter();
        });

        var filterExpression = FilterParser.ParseFilter<TestingPerson>(input, config);

        filterExpression.ToDisplayString().Should().Be("x => ((x.FirstName == x.Title) OrElse (x.Age > 100))");
    }

    [Fact]
    public void prevented_property_in_a_list_in_another_case_is_still_filtered()
    {
        var input = """(title, FirstName) == "x" """;
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<TestingPerson>(x => x.Title!).PreventFilter();
        });

        var filterExpression = FilterParser.ParseFilter<TestingPerson>(input, config);

        filterExpression.ToDisplayString().Should().Be("""x => ((x.Title == "x") OrElse (x.FirstName == "x"))""");
    }

    [Fact]
    public void prevented_property_removes_the_clause_in_any_case()
    {
        var input = """title == "x" || Age > 100""";
        var config = new QueryKitConfiguration(config =>
        {
            config.IgnoredClauseBehavior = IgnoredClauseBehavior.Remove;
            config.Property<TestingPerson>(x => x.Title!).PreventFilter();
        });

        var filterExpression = FilterParser.ParseFilter<TestingPerson>(input, config);

        filterExpression.ToDisplayString().Should().Be("x => (x.Age > 100)");
    }

    [Fact]
    public void prevented_property_with_a_query_name_is_still_filtered_by_its_member_name_in_another_case()
    {
        var input = """title == "x" || Age > 100""";
        var config = new QueryKitConfiguration(config =>
        {
            config.IgnoredClauseBehavior = IgnoredClauseBehavior.Remove;
            config.Property<TestingPerson>(x => x.Title!).HasQueryName("t").PreventFilter();
        });

        var filterExpression = FilterParser.ParseFilter<TestingPerson>(input, config);

        filterExpression.ToDisplayString().Should().Be("""x => ((x.Title == "x") OrElse (x.Age > 100))""");
    }

    [Fact]
    public void property_in_a_list_uses_its_case_insensitive_mode_in_any_case()
    {
        var input = """(title) @=* "x" """;
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<TestingPerson>(x => x.Title!).HasCaseInsensitiveMode(CaseInsensitiveMode.Upper);
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
            config.Property<TestingPerson>(x => x.Title!).PreventSort();
        });

        var sortExpressions = SortParser.ParseSort<TestingPerson>(input, config);

        sortExpressions.Should().ContainSingle();
        sortExpressions[0].Expression!.ToString().Should().Be("x => Convert(x.Age, Object)");
    }

    [Fact]
    public void prevented_sort_property_with_a_query_name_still_sorts_by_its_member_name_in_another_case()
    {
        var input = "title desc";
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<TestingPerson>(x => x.Title!).HasQueryName("t").PreventSort();
        });

        var sortExpressions = SortParser.ParseSort<TestingPerson>(input, config);

        sortExpressions.Should().ContainSingle();
        sortExpressions[0].Expression!.ToString().Should().Be("x => Convert(x.Title, Object)");
    }

    [Fact]
    public void prevented_derived_property_is_still_filtered()
    {
        var input = """full == "x" || Age > 100""";
        var config = new QueryKitConfiguration(config =>
        {
            config.IgnoredClauseBehavior = IgnoredClauseBehavior.Remove;
            config.DerivedProperty<TestingPerson>(x => x.FirstName + " " + x.LastName).HasQueryName("full").PreventFilter();
        });

        var filterExpression = FilterParser.ParseFilter<TestingPerson>(input, config);

        filterExpression.ToDisplayString().Should().Be("""x => ((((x.FirstName + " ") + x.LastName) == "x") OrElse (x.Age > 100))""");
    }

    [Fact]
    public void prevented_derived_property_in_a_list_is_still_filtered()
    {
        var input = """(full, FirstName) == "x" """;
        var config = new QueryKitConfiguration(config =>
        {
            config.DerivedProperty<TestingPerson>(x => x.FirstName + " " + x.LastName).HasQueryName("full").PreventFilter();
        });

        var filterExpression = FilterParser.ParseFilter<TestingPerson>(input, config);

        filterExpression.ToDisplayString().Should().Be("""x => ((((x.FirstName + " ") + x.LastName) == "x") OrElse (x.FirstName == "x"))""");
    }

    [Fact]
    public void prevented_custom_operation_is_still_applied()
    {
        var input = """adult == true || Age > 100""";
        var config = new QueryKitConfiguration(config =>
        {
            config.IgnoredClauseBehavior = IgnoredClauseBehavior.Remove;
            config.CustomOperation<TestingPerson>((x, op, value) => x.Age > 17).HasQueryName("adult").PreventFilter();
        });

        var filterExpression = FilterParser.ParseFilter<TestingPerson>(input, config);

        filterExpression.ToDisplayString().Should().Be("x => (Invoke((entity, op, value) => (Convert(entity, TestingPerson).Age > Convert(17, Nullable`1)), Convert(x, Object), ==, True) OrElse (x.Age > 100))");
    }

    [Fact]
    public void prevented_derived_sort_property_still_sorts()
    {
        var input = "full desc, Age";
        var config = new QueryKitConfiguration(config =>
        {
            config.DerivedProperty<TestingPerson>(x => x.FirstName + " " + x.LastName).HasQueryName("full").PreventSort();
        });

        var sortExpressions = SortParser.ParseSort<TestingPerson>(input, config);

        sortExpressions.Should().HaveCount(2);
    }

    [Fact]
    public void query_name_in_a_property_list_throws()
    {
        var input = """(name, FirstName) == "x" """;
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<TestingPerson>(x => x.Title!).HasQueryName("name");
        });

        var act = () => FilterParser.ParseFilter<TestingPerson>(input, config);

        act.Should().ThrowExactly<UnknownFilterPropertyException>().WithMessage("*'name'*");
    }

    [Fact]
    public void query_name_of_a_prevented_property_in_a_property_list_throws()
    {
        var input = """(hidden, Title) == "x" """;
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<TestingPerson>(x => x.FirstName!).HasQueryName("hidden").PreventFilter().PreventSort();
        });

        var act = () => FilterParser.ParseFilter<TestingPerson>(input, config);

        act.Should().ThrowExactly<UnknownFilterPropertyException>().WithMessage("*'hidden'*");
    }

    [Fact]
    public void query_name_in_arithmetic_is_not_recognized()
    {
        var input = """(stars + 0) > 3""";
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<TestingPerson>(x => x.Rating!).HasQueryName("stars");
        });

        var act = () => FilterParser.ParseFilter<TestingPerson>(input, config);

        act.Should().Throw<UnknownFilterPropertyException>()
            .WithMessage("The filter property 'stars' was not recognized.");
    }

    [Theory]
    [InlineData("first-name")]
    [InlineData("_first")]
    [InlineData("first name")]
    [InlineData("person.first")]
    [InlineData("first_name")]
    public void query_name_that_is_not_a_plain_identifier_resolves_to_its_property(string queryName)
    {
        var input = $"""{queryName} == "Ann" """;
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<TestingPerson>(x => x.FirstName!).HasQueryName(queryName);
        });

        var filterExpression = FilterParser.ParseFilter<TestingPerson>(input, config);

        filterExpression.ToDisplayString().Should().Be("""x => (x.FirstName == "Ann")""");
    }

    [Fact]
    public void query_name_with_a_hyphen_resolves_in_every_case()
    {
        var input = """FIRST-NAME == "Ann" """;
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<TestingPerson>(x => x.FirstName!).HasQueryName("first-name");
        });

        var filterExpression = FilterParser.ParseFilter<TestingPerson>(input, config);

        filterExpression.ToDisplayString().Should().Be("""x => (x.FirstName == "Ann")""");
    }

    [Fact]
    public void query_name_with_a_hyphen_in_a_value_is_replaced()
    {
        var input = """Title == "first-name == x" """;
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<TestingPerson>(x => x.FirstName!).HasQueryName("first-name");
        });

        var filterExpression = FilterParser.ParseFilter<TestingPerson>(input, config);

        filterExpression.ToDisplayString().Should().Be("""x => (x.Title == "FirstName == x")""");
    }

    [Fact]
    public void query_name_with_a_hyphen_before_an_operator_alias_filters_by_its_property()
    {
        var input = """first-name eq "Ann" """;
        var config = new QueryKitConfiguration(config =>
        {
            config.EqualsOperator = "eq";
            config.Property<TestingPerson>(x => x.FirstName!).HasQueryName("first-name");
        });

        var filterExpression = FilterParser.ParseFilter<TestingPerson>(input, config);

        filterExpression.ToDisplayString().Should().Be("""x => (x.FirstName == "Ann")""");
    }

    [Fact]
    public void query_name_on_the_right_side_is_a_value()
    {
        var input = """Title == first""";
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<TestingPerson>(x => x.FirstName!).HasQueryName("first");
        });

        var filterExpression = FilterParser.ParseFilter<TestingPerson>(input, config);

        filterExpression.ToDisplayString().Should().Be("""x => (x.Title == "first")""");
    }

    [Fact]
    public void query_name_with_a_hyphen_sorts_by_its_property()
    {
        var input = "first-name desc";
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<TestingPerson>(x => x.FirstName!).HasQueryName("first-name");
        });

        var sortExpressions = SortParser.ParseSort<TestingPerson>(input, config);

        sortExpressions.Should().ContainSingle();
        sortExpressions[0].Expression!.ToString().Should().Be("x => Convert(x.FirstName, Object)");
        sortExpressions[0].IsAscending.Should().BeFalse();
    }

    [Fact]
    public void longer_query_name_wins_over_a_query_name_it_starts_with()
    {
        var input = """first name == "Ann" && first == "Lee" """;
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<TestingPerson>(x => x.FirstName!).HasQueryName("first name");
            config.Property<TestingPerson>(x => x.LastName!).HasQueryName("first");
        });

        var filterExpression = FilterParser.ParseFilter<TestingPerson>(input, config);

        filterExpression.ToDisplayString().Should().Be("""x => ((x.FirstName == "Ann") AndAlso (x.LastName == "Lee"))""");
    }

    [Fact]
    public void query_name_does_not_match_the_start_of_a_longer_property_name()
    {
        var input = """FirstName == "Ann" """;
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<TestingPerson>(x => x.Title!).HasQueryName("first");
        });

        var filterExpression = FilterParser.ParseFilter<TestingPerson>(input, config);

        filterExpression.ToDisplayString().Should().Be("""x => (x.FirstName == "Ann")""");
    }

    [Fact]
    public void query_name_with_a_hyphen_in_a_property_list_throws()
    {
        var input = """(first-name, Title) == "x" """;
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<TestingPerson>(x => x.FirstName!).HasQueryName("first-name");
        });

        var act = () => FilterParser.ParseFilter<TestingPerson>(input, config);

        act.Should().ThrowExactly<UnknownFilterPropertyException>().WithMessage("*'first'*");
    }

    [Fact]
    public void derived_property_query_name_with_a_hyphen_resolves_to_its_expression()
    {
        var input = """full-name == "Ann Lee" """;
        var config = new QueryKitConfiguration(config =>
        {
            config.DerivedProperty<TestingPerson>(x => x.FirstName + " " + x.LastName).HasQueryName("full-name");
        });

        var filterExpression = FilterParser.ParseFilter<TestingPerson>(input, config);

        filterExpression.ToDisplayString().Should().Be("""x => (((x.FirstName + " ") + x.LastName) == "Ann Lee")""");
    }

    [Fact]
    public void custom_operation_query_name_with_a_space_resolves_to_its_operation()
    {
        var input = """is adult == true""";
        var config = new QueryKitConfiguration(config =>
        {
            config.CustomOperation<TestingPerson>((x, op, value) => x.Age > 17).HasQueryName("is adult");
        });

        var filterExpression = FilterParser.ParseFilter<TestingPerson>(input, config);

        filterExpression.ToDisplayString().Should().Be("x => Invoke((entity, op, value) => (Convert(entity, TestingPerson).Age > Convert(17, Nullable`1)), Convert(x, Object), ==, True)");
    }

    [Fact]
    public void derived_property_query_name_does_not_match_the_start_of_a_longer_name()
    {
        var input = """FirstName == "Ann" """;
        var config = new QueryKitConfiguration(config =>
        {
            config.DerivedProperty<TestingPerson>(x => x.FirstName + " " + x.LastName).HasQueryName("first");
        });

        var filterExpression = FilterParser.ParseFilter<TestingPerson>(input, config);

        filterExpression.ToDisplayString().Should().Be("""x => (x.FirstName == "Ann")""");
    }

    [Fact]
    public void query_name_in_a_value_is_replaced()
    {
        var input = """FirstName == "name == x" """;
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<TestingPerson>(x => x.Title!).HasQueryName("name");
        });

        var filterExpression = FilterParser.ParseFilter<TestingPerson>(input, config);

        filterExpression.ToDisplayString().Should().Be("""x => (x.FirstName == "Title == x")""");
    }

    [Fact]
    public void property_prevented_for_filter_and_sort_throws_by_its_query_name()
    {
        var input = """name == "x" || Age > 100""";
        var config = new QueryKitConfiguration(config =>
        {
            config.IgnoredClauseBehavior = IgnoredClauseBehavior.Remove;
            config.Property<TestingPerson>(x => x.Title!).HasQueryName("name").PreventFilter().PreventSort();
        });

        var act = () => FilterParser.ParseFilter<TestingPerson>(input, config);

        act.Should().ThrowExactly<InvalidOperationException>()
            .WithMessage("'Title' is not allowed for filtering or sorting.");
    }

    [Fact]
    public void property_prevented_for_filter_and_sort_throws_by_its_query_name_before_an_operator_alias()
    {
        var input = """name eq "x" """;
        var config = new QueryKitConfiguration(config =>
        {
            config.EqualsOperator = "eq";
            config.Property<TestingPerson>(x => x.Title!).HasQueryName("name").PreventFilter().PreventSort();
        });

        var act = () => FilterParser.ParseFilter<TestingPerson>(input, config);

        act.Should().ThrowExactly<InvalidOperationException>()
            .WithMessage("'Title' is not allowed for filtering or sorting.");
    }

    [Fact]
    public void property_prevented_for_filter_and_sort_is_removed_by_its_member_name()
    {
        var input = """Title == "x" || Age > 100""";
        var config = new QueryKitConfiguration(config =>
        {
            config.IgnoredClauseBehavior = IgnoredClauseBehavior.Remove;
            config.Property<TestingPerson>(x => x.Title!).HasQueryName("name").PreventFilter().PreventSort();
        });

        var filterExpression = FilterParser.ParseFilter<TestingPerson>(input, config);

        filterExpression.ToDisplayString().Should().Be("x => (x.Age > 100)");
    }

    [Fact]
    public void alias_replacement_replaces_a_query_name_in_a_nested_path()
    {
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<Recipe>(x => x.Title).HasQueryName("name");
        });

        var input = config.PropertyMappings.ReplaceAliasesWithPropertyPaths("""Author.Name == "x" && name == "y" """);

        input.Should().Be("""Author.Title == "x" && Title == "y" """);
    }

    [Fact]
    public void property_path_on_the_right_side_throws()
    {
        var input = """Title == Author.Name""";

        var act = () => FilterParser.ParseFilter<Recipe>(input);

        act.Should().Throw<ParsingException>()
            .WithInnerException<InvalidOperationException>()
            .WithMessage("*Equal is not defined for the types 'System.String' and*Author*");
    }

    [Fact]
    public void unquoted_dotted_word_on_the_right_side_throws()
    {
        var input = """Title == foo.bar""";

        var act = () => FilterParser.ParseFilter<Recipe>(input);

        act.Should().Throw<ParsingException>().WithMessage("*Line 1, Column 13*");
    }

    [Fact]
    public void unknown_property_in_arithmetic_removes_the_clause_when_unknown_properties_are_allowed()
    {
        var input = """(Nope + 1) > 3 || Age > 100""";
        var config = new QueryKitConfiguration(config =>
        {
            config.IgnoredClauseBehavior = IgnoredClauseBehavior.Remove;
            config.AllowUnknownProperties = true;
        });

        var filterExpression = FilterParser.ParseFilter<TestingPerson>(input, config);

        filterExpression.ToDisplayString().Should().Be("x => (x.Age > 100)");
    }

    [Fact]
    public void unknown_property_on_the_right_side_of_arithmetic_removes_the_clause_when_unknown_properties_are_allowed()
    {
        var input = """(Age + 0) > Nope || Title == "a" """;
        var config = new QueryKitConfiguration(config =>
        {
            config.IgnoredClauseBehavior = IgnoredClauseBehavior.Remove;
            config.AllowUnknownProperties = true;
        });

        var filterExpression = FilterParser.ParseFilter<TestingPerson>(input, config);

        filterExpression.ToDisplayString().Should().Be("""x => (x.Title == "a")""");
    }

    [Fact]
    public void unknown_property_in_arithmetic_is_not_recognized()
    {
        var input = """(Nope + 1) > 3""";

        var act = () => FilterParser.ParseFilter<TestingPerson>(input);

        act.Should().Throw<UnknownFilterPropertyException>()
            .WithMessage("The filter property 'Nope' was not recognized.");
    }

    [Theory]
    [InlineData("InternalScore > 30", "x => (x.InternalScore > 30)")]
    [InlineData("internalscore > 30", "x => (x.InternalScore > 30)")]
    [InlineData("""ProtectedNote == "a" """, """x => (x.ProtectedNote == "a")""")]
    [InlineData("secretRank == 7", "x => (x.secretRank == 7)")]
    [InlineData("""Owner.InternalAlias == "Ann" """, """x => (x.Owner.InternalAlias == "Ann")""")]
    [InlineData("(InternalScore, Rating) > 3", "x => ((x.InternalScore > 3) OrElse (x.Rating > 3))")]
    public void non_public_member_filters_like_a_public_member(string input, string expected)
    {
        var filterExpression = FilterParser.ParseFilter<MemberLookupModel>(input);

        filterExpression.ToDisplayString().Should().Be(expected);
    }

    [Fact]
    public void non_public_member_filters_the_rows()
    {
        var models = new List<MemberLookupModel>
        {
            new(internalScore: 50, rank: 7),
            new(internalScore: 20, rank: 3),
        };

        var result = models.ApplyQueryKitFilter("InternalScore > 30 && secretRank == 7").ToList();

        result.Should().ContainSingle().Which.Should().BeSameAs(models[0]);
    }

    [Fact]
    public void non_public_member_filters_when_unknown_properties_are_allowed()
    {
        var input = """secretRank > 100 || Rating == 1""";
        var config = new QueryKitConfiguration(config =>
        {
            config.AllowUnknownProperties = true;
        });

        var filterExpression = FilterParser.ParseFilter<MemberLookupModel>(input, config);

        filterExpression.ToDisplayString().Should().Be("x => ((x.secretRank > 100) OrElse (x.Rating == 1))");
    }

    [Fact]
    public void query_name_on_a_non_public_member_filters_by_that_member()
    {
        var input = """score > 30""";
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<MemberLookupModel>(x => x.InternalScore).HasQueryName("score");
        });

        var filterExpression = FilterParser.ParseFilter<MemberLookupModel>(input, config);

        filterExpression.ToDisplayString().Should().Be("x => (x.InternalScore > 30)");
    }

    [Fact]
    public void public_property_matches_before_a_non_public_field_with_the_same_name()
    {
        var input = """rank == 1""";

        var filterExpression = FilterParser.ParseFilter<MemberLookupModel>(input);

        filterExpression.ToDisplayString().Should().Be("x => (x.Rank == 1)");
    }

    [Theory]
    [InlineData("""Item == "x" """)]
    [InlineData("""item == "x" """)]
    [InlineData("""(Item, Rating) == "x" """)]
    public void indexer_is_an_unknown_property(string input)
    {
        var act = () => FilterParser.ParseFilter<MemberLookupModel>(input);

        act.Should().Throw<UnknownFilterPropertyException>()
            .WithMessage("The filter property 'Item' was not recognized.");
    }

    [Fact]
    public void indexer_clause_is_true_equals_true_when_unknown_properties_are_allowed()
    {
        var input = """Item == "x" """;
        var config = new QueryKitConfiguration(config =>
        {
            config.AllowUnknownProperties = true;
        });

        var filterExpression = FilterParser.ParseFilter<MemberLookupModel>(input, config);

        filterExpression.ToDisplayString().Should().Be("x => (True == True)");
    }

    private class MemberLookupOwner
    {
        internal string InternalAlias { get; set; } = "";
    }

    private class MemberLookupModel
    {
        public MemberLookupModel() { }

        public MemberLookupModel(int internalScore, int rank)
        {
            InternalScore = internalScore;
            secretRank = rank;
        }

        public int Rating { get; set; }
        public int Rank { get; set; }
        public MemberLookupOwner Owner { get; set; } = new();
        internal int InternalScore { get; set; }
        protected string ProtectedNote { get; set; } = "";
        private int secretRank;
#pragma warning disable CS0169 // Never read - the field only tests that the public Rank property matches first
        private int rank;
#pragma warning restore CS0169
        public string this[string key] => key;
    }
}
