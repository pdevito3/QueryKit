namespace QueryKit.UnitTests;

using QueryKit.Configuration;
using QueryKit.Exceptions;
using FluentAssertions;
using WebApiTestProject.Entities;

public class PropertyDepthTests
{
    // Filter tests for global MaxPropertyDepth

    [Fact]
    public void filter_with_depth_1_allowed_when_max_depth_is_1()
    {
        var input = """Email.Value == "test@example.com" """;
        var config = new QueryKitConfiguration(settings =>
        {
            settings.MaxPropertyDepth = 1;
        });

        var filterExpression = FilterParser.ParseFilter<TestingPerson>(input, config);
        filterExpression.Should().NotBeNull();
    }

    [Fact]
    public void filter_with_depth_2_throws_when_max_depth_is_1()
    {
        var input = """PhysicalAddress.PostalCode.Value == "12345" """;
        var config = new QueryKitConfiguration(settings =>
        {
            settings.MaxPropertyDepth = 1;
        });

        var act = () => FilterParser.ParseFilter<TestingPerson>(input, config);
        act.Should().Throw<QueryKitPropertyDepthExceededException>()
            .WithMessage("*PhysicalAddress.PostalCode.Value*depth of 2*maximum allowed depth of 1*");
    }

    [Fact]
    public void filter_with_depth_2_allowed_when_max_depth_is_2()
    {
        var input = """PhysicalAddress.PostalCode.Value == "12345" """;
        var config = new QueryKitConfiguration(settings =>
        {
            settings.MaxPropertyDepth = 2;
        });

        var filterExpression = FilterParser.ParseFilter<TestingPerson>(input, config);
        filterExpression.Should().NotBeNull();
    }

    [Fact]
    public void filter_with_depth_0_allowed_when_max_depth_is_1()
    {
        var input = """Title == "Test" """;
        var config = new QueryKitConfiguration(settings =>
        {
            settings.MaxPropertyDepth = 1;
        });

        var filterExpression = FilterParser.ParseFilter<TestingPerson>(input, config);
        filterExpression.Should().NotBeNull();
    }

    [Fact]
    public void filter_with_no_max_depth_allows_any_depth()
    {
        var input = """PhysicalAddress.PostalCode.Value == "12345" """;

        var filterExpression = FilterParser.ParseFilter<TestingPerson>(input);
        filterExpression.Should().NotBeNull();
    }

    // Filter tests for per-property MaxDepth override

    [Fact]
    public void filter_per_property_max_depth_overrides_global()
    {
        var input = """PhysicalAddress.PostalCode.Value == "12345" """;
        var config = new QueryKitConfiguration(settings =>
        {
            settings.MaxPropertyDepth = 1; // Global limit of 1
            settings.Property<TestingPerson>(x => x.PhysicalAddress).HasMaxDepth(2); // Override for PhysicalAddress
        });

        var filterExpression = FilterParser.ParseFilter<TestingPerson>(input, config);
        filterExpression.Should().NotBeNull();
    }

    [Fact]
    public void filter_per_property_max_depth_can_be_more_restrictive()
    {
        var input = """PhysicalAddress.PostalCode.Value == "12345" """;
        var config = new QueryKitConfiguration(settings =>
        {
            settings.MaxPropertyDepth = 5; // Global limit of 5
            settings.Property<TestingPerson>(x => x.PhysicalAddress).HasMaxDepth(1); // Restrict PhysicalAddress to 1
        });

        var act = () => FilterParser.ParseFilter<TestingPerson>(input, config);
        act.Should().Throw<QueryKitPropertyDepthExceededException>()
            .WithMessage("*PhysicalAddress.PostalCode.Value*depth of 2*maximum allowed depth of 1*");
    }

    [Fact]
    public void filter_other_properties_still_use_global_max_depth()
    {
        var input = """Email.Value == "test@example.com" """;
        var config = new QueryKitConfiguration(settings =>
        {
            settings.MaxPropertyDepth = 1;
            settings.Property<TestingPerson>(x => x.PhysicalAddress).HasMaxDepth(2);
        });

        // Email.Value has depth 1, which is within global limit
        var filterExpression = FilterParser.ParseFilter<TestingPerson>(input, config);
        filterExpression.Should().NotBeNull();
    }

    // Sort tests for global MaxPropertyDepth

    [Fact]
    public void sort_with_depth_1_allowed_when_max_depth_is_1()
    {
        var input = "Email.Value";
        var config = new QueryKitConfiguration(settings =>
        {
            settings.MaxPropertyDepth = 1;
        });

        var act = () => SortParser.ParseSort<TestingPerson>(input, config);
        act.Should().NotThrow();
    }

    [Fact]
    public void sort_with_depth_2_throws_when_max_depth_is_1()
    {
        var input = "PhysicalAddress.PostalCode.Value";
        var config = new QueryKitConfiguration(settings =>
        {
            settings.MaxPropertyDepth = 1;
        });

        var act = () => SortParser.ParseSort<TestingPerson>(input, config);
        act.Should().Throw<QueryKitPropertyDepthExceededException>()
            .WithMessage("*PhysicalAddress.PostalCode.Value*depth of 2*maximum allowed depth of 1*");
    }

    [Fact]
    public void sort_with_depth_2_allowed_when_max_depth_is_2()
    {
        var input = "PhysicalAddress.PostalCode.Value";
        var config = new QueryKitConfiguration(settings =>
        {
            settings.MaxPropertyDepth = 2;
        });

        var act = () => SortParser.ParseSort<TestingPerson>(input, config);
        act.Should().NotThrow();
    }

    [Fact]
    public void sort_with_no_max_depth_allows_any_depth()
    {
        var input = "PhysicalAddress.PostalCode.Value";

        var act = () => SortParser.ParseSort<TestingPerson>(input, null);
        act.Should().NotThrow();
    }

    // Sort tests for per-property MaxDepth override

    [Fact]
    public void sort_per_property_max_depth_overrides_global()
    {
        var input = "PhysicalAddress.PostalCode.Value";
        var config = new QueryKitConfiguration(settings =>
        {
            settings.MaxPropertyDepth = 1; // Global limit of 1
            settings.Property<TestingPerson>(x => x.PhysicalAddress).HasMaxDepth(2); // Override for PhysicalAddress
        });

        var act = () => SortParser.ParseSort<TestingPerson>(input, config);
        act.Should().NotThrow();
    }

    [Fact]
    public void sort_per_property_max_depth_can_be_more_restrictive()
    {
        var input = "PhysicalAddress.PostalCode.Value";
        var config = new QueryKitConfiguration(settings =>
        {
            settings.MaxPropertyDepth = 5; // Global limit of 5
            settings.Property<TestingPerson>(x => x.PhysicalAddress).HasMaxDepth(1); // Restrict PhysicalAddress to 1
        });

        var act = () => SortParser.ParseSort<TestingPerson>(input, config);
        act.Should().Throw<QueryKitPropertyDepthExceededException>()
            .WithMessage("*PhysicalAddress.PostalCode.Value*depth of 2*maximum allowed depth of 1*");
    }

    // Property list grouping tests

    [Fact]
    public void filter_property_list_respects_max_depth()
    {
        var input = """(Title, PhysicalAddress.PostalCode.Value) @=* "test" """;
        var config = new QueryKitConfiguration(settings =>
        {
            settings.MaxPropertyDepth = 1;
        });

        var act = () => FilterParser.ParseFilter<TestingPerson>(input, config);
        act.Should().Throw<QueryKitPropertyDepthExceededException>();
    }

    [Fact]
    public void filter_property_list_allows_valid_depth()
    {
        var input = """(Title, Email.Value) @=* "test" """;
        var config = new QueryKitConfiguration(settings =>
        {
            settings.MaxPropertyDepth = 1;
        });

        var filterExpression = FilterParser.ParseFilter<TestingPerson>(input, config);
        filterExpression.Should().NotBeNull();
    }

    // Edge cases

    [Fact]
    public void filter_max_depth_of_0_only_allows_root_properties()
    {
        var input = """Email.Value == "test@example.com" """;
        var config = new QueryKitConfiguration(settings =>
        {
            settings.MaxPropertyDepth = 0;
        });

        var act = () => FilterParser.ParseFilter<TestingPerson>(input, config);
        act.Should().Throw<QueryKitPropertyDepthExceededException>()
            .WithMessage("*Email.Value*depth of 1*maximum allowed depth of 0*");
    }

    [Fact]
    public void filter_root_property_allowed_when_max_depth_is_0()
    {
        var input = """Title == "Test" """;
        var config = new QueryKitConfiguration(settings =>
        {
            settings.MaxPropertyDepth = 0;
        });

        var filterExpression = FilterParser.ParseFilter<TestingPerson>(input, config);
        filterExpression.Should().NotBeNull();
    }
}
