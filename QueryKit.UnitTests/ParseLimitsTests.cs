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

    private sealed class InterfaceOnlyConfigurationWithLimits : FilterBehaviorInterfaceTests.InterfaceOnlyConfiguration, IQueryKitParseLimits
    {
        public int MaxNestingDepth { get; set; }
        public int MaxInputLength { get; set; }
    }
}
