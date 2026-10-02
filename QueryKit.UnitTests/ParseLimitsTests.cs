namespace QueryKit.UnitTests;

using QueryKit.Configuration;
using QueryKit.Exceptions;
using FluentAssertions;
using WebApiTestProject.Entities;

public class ParseLimitsTests
{
    [Fact]
    public void filter_within_default_nesting_depth_parses()
    {
        var input = new string('(', 5) + """Title == "salt" """ + new string(')', 5);

        var filterExpression = FilterParser.ParseFilter<TestingPerson>(input);
        filterExpression.Should().NotBeNull();
    }

    [Fact]
    public void filter_over_default_nesting_depth_throws()
    {
        var input = new string('(', QueryKitSettings.DefaultMaxNestingDepth + 1)
            + """Title == "salt" """
            + new string(')', QueryKitSettings.DefaultMaxNestingDepth + 1);

        var act = () => FilterParser.ParseFilter<TestingPerson>(input);
        act.Should().Throw<QueryKitNestingDepthExceededException>()
            .WithMessage($"*depth of {QueryKitSettings.DefaultMaxNestingDepth + 1}*maximum allowed depth of {QueryKitSettings.DefaultMaxNestingDepth}*");
    }

    [Fact]
    public void filter_over_configured_nesting_depth_throws()
    {
        var input = new string('(', 3) + """Title == "salt" """ + new string(')', 3);
        var config = new QueryKitConfiguration(settings =>
        {
            settings.MaxNestingDepth = 2;
        });

        var act = () => FilterParser.ParseFilter<TestingPerson>(input, config);
        act.Should().Throw<QueryKitNestingDepthExceededException>()
            .WithMessage("*depth of 3*maximum allowed depth of 2*");
    }

    [Fact]
    public void filter_within_configured_nesting_depth_parses()
    {
        var input = new string('(', 3) + """Title == "salt" """ + new string(')', 3);
        var config = new QueryKitConfiguration(settings =>
        {
            settings.MaxNestingDepth = 3;
        });

        var filterExpression = FilterParser.ParseFilter<TestingPerson>(input, config);
        filterExpression.Should().NotBeNull();
    }

    [Fact]
    public void filter_within_default_input_length_parses()
    {
        var input = """Title == "salt" """;

        var filterExpression = FilterParser.ParseFilter<TestingPerson>(input);
        filterExpression.Should().NotBeNull();
    }

    [Fact]
    public void quoted_value_with_33_parentheses_parses_by_default()
    {
        var input = $"""Title == "{new string('(', 33)}" """;

        var filterExpression = FilterParser.ParseFilter<TestingPerson>(input);
        filterExpression.Should().NotBeNull();
    }

    [Fact]
    public void filter_over_default_input_length_throws()
    {
        var padding = new string('a', QueryKitSettings.DefaultMaxInputLength);
        var input = $"""Title == "{padding}" """;

        var act = () => FilterParser.ParseFilter<TestingPerson>(input);
        act.Should().Throw<QueryKitInputLengthExceededException>()
            .WithMessage($"*length of {input.Length}*maximum allowed length of {QueryKitSettings.DefaultMaxInputLength}*");
    }

    [Fact]
    public void filter_over_configured_input_length_throws()
    {
        var input = """Title == "salt and pepper" """;
        var config = new QueryKitConfiguration(settings =>
        {
            settings.MaxInputLength = 10;
        });

        var act = () => FilterParser.ParseFilter<TestingPerson>(input, config);
        act.Should().Throw<QueryKitInputLengthExceededException>()
            .WithMessage($"*length of {input.Length}*maximum allowed length of 10*");
    }

    [Fact]
    public void filter_within_configured_input_length_parses()
    {
        var input = """Title == "salt" """;
        var config = new QueryKitConfiguration(settings =>
        {
            settings.MaxInputLength = input.Length;
        });

        var filterExpression = FilterParser.ParseFilter<TestingPerson>(input, config);
        filterExpression.Should().NotBeNull();
    }

    [Fact]
    public void configuration_that_implements_only_the_interface_uses_the_default_limits()
    {
        var config = new FilterBehaviorInterfaceTests.InterfaceOnlyConfiguration();

        var filterExpression = FilterParser.ParseFilter<TestingPerson>("""Title == "salt" """, config);
        filterExpression.Should().NotBeNull();

        var tooDeep = new string('(', QueryKitSettings.DefaultMaxNestingDepth + 1)
            + """Title == "salt" """
            + new string(')', QueryKitSettings.DefaultMaxNestingDepth + 1);
        var actDeep = () => FilterParser.ParseFilter<TestingPerson>(tooDeep, config);
        actDeep.Should().Throw<QueryKitNestingDepthExceededException>()
            .WithMessage($"*maximum allowed depth of {QueryKitSettings.DefaultMaxNestingDepth}*");

        var tooLong = $"""Title == "{new string('a', QueryKitSettings.DefaultMaxInputLength)}" """;
        var actLong = () => FilterParser.ParseFilter<TestingPerson>(tooLong, config);
        actLong.Should().Throw<QueryKitInputLengthExceededException>()
            .WithMessage($"*maximum allowed length of {QueryKitSettings.DefaultMaxInputLength}*");
    }

    [Fact]
    public void configuration_that_implements_the_parse_limits_uses_its_own_limits()
    {
        var config = new InterfaceOnlyConfigurationWithLimits { MaxNestingDepth = 2, MaxInputLength = 100 };
        var input = new string('(', 3) + """Title == "salt" """ + new string(')', 3);

        var act = () => FilterParser.ParseFilter<TestingPerson>(input, config);
        act.Should().Throw<QueryKitNestingDepthExceededException>()
            .WithMessage("*depth of 3*maximum allowed depth of 2*");
    }

    [Fact]
    public void quoted_close_parentheses_before_a_group_do_not_lower_the_nesting_depth()
    {
        var input = $"""Title == "{new string(')', 20)}" || """ + new string('(', 20) + """Title == "salt" """ + new string(')', 20);

        var act = () => FilterParser.ParseFilter<TestingPerson>(input, DepthLimit(10));
        act.Should().Throw<QueryKitNestingDepthExceededException>()
            .WithMessage("*depth of 11*maximum allowed depth of 10*");
    }

    [Fact]
    public void quoted_close_parentheses_inside_a_group_do_not_lower_the_nesting_depth()
    {
        var input = new string('(', 8) + $"""Title == "{new string(')', 8)}" && """
            + new string('(', 8) + """Title == "salt" """ + new string(')', 16);

        var act = () => FilterParser.ParseFilter<TestingPerson>(input, DepthLimit(10));
        act.Should().Throw<QueryKitNestingDepthExceededException>()
            .WithMessage("*depth of 11*maximum allowed depth of 10*");
    }

    [Fact]
    public void repeated_quoted_close_parentheses_do_not_lower_the_nesting_depth()
    {
        var segment = """((((Title == "))))" && """;
        var input = string.Concat(Enumerable.Repeat(segment, 5)) + """Title == "salt" """ + new string(')', 20);

        var act = () => FilterParser.ParseFilter<TestingPerson>(input, DepthLimit(10));
        act.Should().Throw<QueryKitNestingDepthExceededException>()
            .WithMessage("*depth of 11*maximum allowed depth of 10*");
    }

    [Fact]
    public void quoted_parentheses_within_the_nesting_depth_keep_their_value()
    {
        var input = """"((Title == ")))" || Title == "(((" || Title == """((("""))"""";

        var filterExpression = FilterParser.ParseFilter<TestingPerson>(input, DepthLimit(2));
        filterExpression.Compile().Invoke(new TestingPerson { Title = ")))" }).Should().BeTrue();
        filterExpression.Compile().Invoke(new TestingPerson { Title = "(((" }).Should().BeTrue();
        filterExpression.Compile().Invoke(new TestingPerson { Title = "salt" }).Should().BeFalse();
    }

    [Fact]
    public void quoted_open_parentheses_do_not_count_toward_the_nesting_depth()
    {
        var parentheses = new string('(', 33);
        var input = $$""""Title == "{{parentheses}}" || Title == """{{parentheses}}""" """";

        var filterExpression = FilterParser.ParseFilter<TestingPerson>(input, DepthLimit(1));
        filterExpression.Compile().Invoke(new TestingPerson { Title = parentheses }).Should().BeTrue();
    }

    [Fact]
    public void arithmetic_groups_count_toward_the_nesting_depth()
    {
        var input = """((Age + (Rating * 2)) > 3)""";

        FilterParser.ParseFilter<TestingPerson>(input, DepthLimit(3)).Should().NotBeNull();
        var act = () => FilterParser.ParseFilter<TestingPerson>(input, DepthLimit(2));
        act.Should().Throw<QueryKitNestingDepthExceededException>()
            .WithMessage("*depth of 3*maximum allowed depth of 2*");
    }

    [Fact]
    public void property_list_groups_count_toward_the_nesting_depth()
    {
        var input = """((Title, FirstName) == "salt")""";

        FilterParser.ParseFilter<TestingPerson>(input, DepthLimit(2)).Should().NotBeNull();
        var act = () => FilterParser.ParseFilter<TestingPerson>(input, DepthLimit(1));
        act.Should().Throw<QueryKitNestingDepthExceededException>()
            .WithMessage("*depth of 2*maximum allowed depth of 1*");
    }

    [Fact]
    public void deep_filter_with_quoted_close_parentheses_throws_instead_of_overflowing_the_stack()
    {
        // Without the grammar count, this filter overflows a 1 MB stack and stops the test process
        const int depth = 20_000;
        var input = $"""Title == "{new string(')', depth)}" || """ + new string('(', depth) + """Title == "salt" """ + new string(')', depth);

        Exception? thrown = null;
        var thread = new Thread(() =>
        {
            try
            {
                FilterParser.ParseFilter<TestingPerson>(input, new QueryKitConfiguration(settings =>
                {
                    settings.MaxNestingDepth = 10;
                    settings.MaxInputLength = int.MaxValue;
                }));
            }
            catch (Exception e)
            {
                thrown = e;
            }
        }, maxStackSize: 1024 * 1024);
        thread.Start();
        thread.Join();

        thrown.Should().BeOfType<QueryKitNestingDepthExceededException>()
            .Which.Message.Should().Contain("depth of 11");
    }

    private static QueryKitConfiguration DepthLimit(int maxNestingDepth)
        => new(settings => settings.MaxNestingDepth = maxNestingDepth);

    private sealed class InterfaceOnlyConfigurationWithLimits : FilterBehaviorInterfaceTests.InterfaceOnlyConfiguration, IQueryKitParseLimits
    {
        public int MaxNestingDepth { get; set; }
        public int MaxInputLength { get; set; }
    }
}
