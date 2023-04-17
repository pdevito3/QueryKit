namespace QueryKit.UnitTests;

using Bogus;
using FluentAssertions;
using Person = FilterParserTests.Person;

public class PreprocessorTests
{
    [Fact]
    public void can_have_custom_prop_name_for_string()
    {
        var faker = new Faker();
        var value = faker.Lorem.Word();
        var input = $"""special_title == "{value}" """;

        var config = new QueryKitProcessorConfiguration(config =>
        {
            config.Property<Person>(x => x.Title).HasQueryName("special_title");
        });
        var filterExpression = FilterParser.ParseFilter<Person>(input, config);
        filterExpression.ToString().Should().Be($"""x => (x.Title == "{value}")""");
    }
    
    [Fact]
    public void can_have_custom_prop_name_for_multiple_props()
    {
        var faker = new Faker();
        var stringValue = faker.Lorem.Word();
        var guidValue = Guid.NewGuid();
        var input = $"""special_title == "{stringValue}" || identifier == "{guidValue}" """;

        var config = new QueryKitProcessorConfiguration(config =>
        {
            config.Property<Person>(x => x.Title).HasQueryName("special_title");
            config.Property<Person>(x => x.Id).HasQueryName("identifier");
        });
        var filterExpression = FilterParser.ParseFilter<Person>(input, config);
        filterExpression.ToString().Should().Be($"""x => ((x.Title == "{stringValue}") OrElse (x.Id == Parse("{guidValue}")))""");
    }
    
    [Fact]
    public void can_have_custom_prop_name_for_some_props()
    {
        var faker = new Faker();
        var stringValue = faker.Lorem.Word();
        var guidValue = Guid.NewGuid();
        var input = $"""special_title == "{stringValue}" || Id == "{guidValue}" """;

        var config = new QueryKitProcessorConfiguration(config =>
        {
            config.Property<Person>(x => x.Title).HasQueryName("special_title");
        });
        var filterExpression = FilterParser.ParseFilter<Person>(input, config);
        filterExpression.ToString().Should().Be($"""x => ((x.Title == "{stringValue}") OrElse (x.Id == Parse("{guidValue}")))""");
    }
    
    [Fact]
    public void can_handle_case_insensitive_custom_props()
    {
        var faker = new Faker();
        var value = faker.Lorem.Word();
        var input = $"""SPECIALtitle == "{value}" """;

        var config = new QueryKitProcessorConfiguration(config =>
        {
            config.Property<Person>(x => x.Title).HasQueryName("specialtitle");
        });
        var filterExpression = FilterParser.ParseFilter<Person>(input, config);
        filterExpression.ToString().Should().Be($"""x => (x.Title == "{value}")""");
    }
}