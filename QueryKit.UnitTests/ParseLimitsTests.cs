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
    public void filter_over_33_nesting_levels_parses_by_default()
    {
        var input = new string('(', 33) + """Title == "salt" """ + new string(')', 33);

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
    public void filter_over_5000_characters_parses_by_default()
    {
        var padding = new string('a', 5000);
        var input = $"""Title == "{padding}" """;

        var filterExpression = FilterParser.ParseFilter<TestingPerson>(input);
        filterExpression.Should().NotBeNull();
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
    public void configuration_that_implements_only_the_interface_has_no_limits()
    {
        var config = new FilterBehaviorInterfaceTests.InterfaceOnlyConfiguration();

        var deep = new string('(', 33) + """Title == "salt" """ + new string(')', 33);
        FilterParser.ParseFilter<TestingPerson>(deep, config).Should().NotBeNull();

        var longInput = $"""Title == "{new string('a', 5000)}" """;
        FilterParser.ParseFilter<TestingPerson>(longInput, config).Should().NotBeNull();
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

    private sealed class InterfaceOnlyConfigurationWithLimits : FilterBehaviorInterfaceTests.InterfaceOnlyConfiguration, IQueryKitParseLimits
    {
        public int MaxNestingDepth { get; set; }
        public int MaxInputLength { get; set; }
    }
}
