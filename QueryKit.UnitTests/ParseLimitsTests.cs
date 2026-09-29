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
}
