namespace QueryKit.UnitTests;

using Bogus;
using FluentAssertions;
using Person = FilterParserTests.Person;

public class PreprocessorTests
{
    public class Person
    {
        public string Title { get; set; }
        public int Age { get; set; }
        public string BirthMonth { get; set; }
        public decimal Rating { get; set; }
        public DateOnly Date { get; set; }
        public bool Favorite { get; set; }
        public DateTimeOffset SpecificDate { get; set; }
        public TimeOnly Time { get; set; }
        public Guid Id { get; set; }
        public EmailAddress Email { get; set; }
    }

    public record EmailAddress(string Value)
    {
        public string Value { get; init; }
    }
    
    [Fact]
    public void can_have_child_prop_name()
    {
        var faker = new Faker();
        var value = faker.Lorem.Word();
        var input = $"""Email.Value == "{value}" """;
        var filterExpression = FilterParser.ParseFilter<Person>(input);
        filterExpression.ToString().Should().Be($"""x => (x.Email.Value == "{value}")""");
    }
    
    [Fact]
    public void can_have_custom_child_prop_name()
    {
        var faker = new Faker();
        var value = faker.Lorem.Word();
        var input = $"""email == "{value}" """;
    
        var config = new QueryKitProcessorConfiguration(config =>
        {
            config.Property<Person>(x => x.Email.Value).HasQueryName("email");
        });
        var filterExpression = FilterParser.ParseFilter<Person>(input, config);
        filterExpression.ToString().Should().Be($"""x => (x.Email.Value == "{value}")""");
    }
    
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
    
    [Fact]
    public void can_have_custom_prop_excluded_from_filter()
    {
        var faker = new Faker();
        var stringValue = faker.Lorem.Word();
        var guidValue = Guid.NewGuid();
        var input = $"""special_title == "{stringValue}" || Id == "{guidValue}" """;

        var config = new QueryKitProcessorConfiguration(config =>
        {
            config.Property<Person>(x => x.Title).HasQueryName("special_title");
            config.Property<Person>(x => x.Id).PreventFilter();
        });
        var filterExpression = FilterParser.ParseFilter<Person>(input, config);
        filterExpression.ToString().Should().Be($"""x => ((x.Title == "{stringValue}") OrElse (True == True))""");
    }
    
    [Fact]
    public void can_have_custom_prop_excluded_from_filter_with_custom_propname()
    {
        var faker = new Faker();
        var stringValue = faker.Lorem.Word();
        var guidValue = Guid.NewGuid();
        var input = $"""special_title == "{stringValue}" || identifier == "{guidValue}" """;

        var config = new QueryKitProcessorConfiguration(config =>
        {
            config.Property<Person>(x => x.Title).HasQueryName("special_title");
            config.Property<Person>(x => x.Id).PreventFilter().HasQueryName("identifier");
        });
        var filterExpression = FilterParser.ParseFilter<Person>(input, config);
        filterExpression.ToString().Should().Be($"""x => ((x.Title == "{stringValue}") OrElse (True == True))""");
    }
    
    [Fact]
    public void filter_prevented_props_always_have_true_equals_true_regardless_of_comparison()
    {
        var faker = new Faker();
        var filterOperator = faker.PickRandom(ComparisonOperator.List.Where(x => x != ComparisonOperator.EqualsOperator()).ToList());
        var guidValue = Guid.NewGuid();
        var input = $"""Id {filterOperator} "{guidValue}" """;

        var config = new QueryKitProcessorConfiguration(config =>
        {
            config.Property<Person>(x => x.Id).PreventFilter();
        });
        var filterExpression = FilterParser.ParseFilter<Person>(input, config);
        filterExpression.ToString().Should().Be($"""x => (True == True)""");
    }
}