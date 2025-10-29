namespace QueryKit.UnitTests;

using Bogus;
using Configuration;
using Exceptions;
using FluentAssertions;
using Operators;
using WebApiTestProject.Entities;
using WebApiTestProject.Entities.Recipes;

public class CustomFilterPropertyTests
{
    [Fact]
    public void can_have_child_prop_name_ownsone()
    {
        var faker = new Faker();
        var value = faker.Lorem.Word();
        var input = $"""PhysicalAddress.State == "{value}" """;
        var filterExpression = FilterParser.ParseFilter<TestingPerson>(input);
        filterExpression.ToString().Should().Be($"""x => (x.PhysicalAddress.State == "{value}")""");
    }

    [Fact]
    public void can_have_custom_child_prop_name_ownsone()
    {
        var faker = new Faker();
        var value = faker.Lorem.Word();
        var input = $"""state == "{value}" """;
    
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<TestingPerson>(x => x.PhysicalAddress.State).HasQueryName("state");
        });
        var filterExpression = FilterParser.ParseFilter<TestingPerson>(input, config);
        filterExpression.ToString().Should().Be($"""x => (x.PhysicalAddress.State == "{value}")""");
    }
    
    [Fact(Skip = "Will need something like this if i want to support HasConversion in efcore.")]
    public void can_have_child_prop_name_for_efcore_HasConversion()
    {
        var faker = new Faker();
        var value = faker.Lorem.Word();
        var input = $"""Email.Value == "{value}" """;
        var filterExpression = FilterParser.ParseFilter<TestingPerson>(input);
        filterExpression.ToString().Should().Be($"""x => (x.Email == "{value}")""");
    }
    
    [Fact(Skip = "Will need something like this if i want to support HasConversion in efcore.")]
    public void can_have_custom_child_prop_name_for_efcore_HasConversion()
    {
        var faker = new Faker();
        var value = faker.Lorem.Word();
        var input = $"""email == "{value}" """;
    
        var config = new QueryKitConfiguration(config =>
        {
            config.Property<TestingPerson>(x => x.Email).HasQueryName("email");
        });
        var filterExpression = FilterParser.ParseFilter<TestingPerson>(input, config);
        filterExpression.ToString().Should().Be($"""x => (x.Email == "{value}")""");
    }
    
    [Fact]
    public void can_have_custom_prop_name_for_string()
    {
        var faker = new Faker();
        var value = faker.Lorem.Word();
        var input = $"""special_title == "{value}" """;

        var config = new QueryKitConfiguration(config =>
        {
            config.Property<TestingPerson>(x => x.Title).HasQueryName("special_title");
        });
        var filterExpression = FilterParser.ParseFilter<TestingPerson>(input, config);
        filterExpression.ToString().Should().Be($"""x => (x.Title == "{value}")""");
    }
    
    [Fact]
    public void can_handle_alias_in_value()
    {
        var faker = new Faker();
        var value = faker.Lorem.Word();
        var input = $"""special_title == "{value} with special_value" """;

        var config = new QueryKitConfiguration(config =>
        {
            config.Property<TestingPerson>(x => x.Title).HasQueryName("special_title");
        });
        var filterExpression = FilterParser.ParseFilter<TestingPerson>(input, config);
        filterExpression.ToString().Should().Be($"""x => (x.Title == "{value} with special_value")""");
    }
    
    [Fact]
    public void can_handle_alias_in_value_with_operator_after_it()
    {
        var faker = new Faker();
        var value = faker.Lorem.Word();
        var input = $"""special_title == "{value} with special_value @=* a thing" """;

        var config = new QueryKitConfiguration(config =>
        {
            config.Property<TestingPerson>(x => x.Title).HasQueryName("special_title");
        });
        var filterExpression = FilterParser.ParseFilter<TestingPerson>(input, config);
        filterExpression.ToString().Should().Be($"""x => (x.Title == "{value} with special_value @=* a thing")""");
    }
    
    [Fact]
    public void can_have_custom_prop_name_for_multiple_props()
    {
        var faker = new Faker();
        var stringValue = faker.Lorem.Word();
        var guidValue = Guid.NewGuid();
        var input = $"""special_title == "{stringValue}" || identifier == "{guidValue}" """;

        var config = new QueryKitConfiguration(config =>
        {
            config.Property<TestingPerson>(x => x.Title).HasQueryName("special_title");
            config.Property<TestingPerson>(x => x.Id).HasQueryName("identifier");
        });
        var filterExpression = FilterParser.ParseFilter<TestingPerson>(input, config);
        filterExpression.ToString().Should().Be($"""x => ((x.Title == "{stringValue}") OrElse (x.Id == {guidValue}))""");
    }

    [Fact]
    public void can_have_custom_prop_name_for_some_props()
    {
        var faker = new Faker();
        var stringValue = faker.Lorem.Word();
        var guidValue = Guid.NewGuid();
        var input = $"""special_title == "{stringValue}" || Id == "{guidValue}" """;

        var config = new QueryKitConfiguration(config =>
        {
            config.Property<TestingPerson>(x => x.Title).HasQueryName("special_title");
        });
        var filterExpression = FilterParser.ParseFilter<TestingPerson>(input, config);
        filterExpression.ToString().Should().Be($"""x => ((x.Title == "{stringValue}") OrElse (x.Id == {guidValue}))""");
    }
    
    [Fact]
    public void can_handle_case_insensitive_custom_props()
    {
        var faker = new Faker();
        var value = faker.Lorem.Word();
        var input = $"""SPECIALtitle == "{value}" """;

        var config = new QueryKitConfiguration(config =>
        {
            config.Property<TestingPerson>(x => x.Title).HasQueryName("specialtitle");
        });
        var filterExpression = FilterParser.ParseFilter<TestingPerson>(input, config);
        filterExpression.ToString().Should().Be($"""x => (x.Title == "{value}")""");
    }
    
    [Fact]
    public void can_have_custom_prop_excluded_from_filter()
    {
        var faker = new Faker();
        var stringValue = faker.Lorem.Word();
        var guidValue = Guid.NewGuid();
        var input = $"""special_title == "{stringValue}" || Id == "{guidValue}" """;

        var config = new QueryKitConfiguration(config =>
        {
            config.Property<TestingPerson>(x => x.Title).HasQueryName("special_title");
            config.Property<TestingPerson>(x => x.Id).PreventFilter();
        });
        var filterExpression = FilterParser.ParseFilter<TestingPerson>(input, config);
        filterExpression.ToString().Should().Be($"""x => ((x.Title == "{stringValue}") OrElse (True == True))""");
    }
    
    [Fact]
    public void can_have_custom_prop_excluded_from_filter_with_custom_propname()
    {
        var faker = new Faker();
        var stringValue = faker.Lorem.Word();
        var guidValue = Guid.NewGuid();
        var input = $"""special_title == "{stringValue}" || identifier == "{guidValue}" """;

        var config = new QueryKitConfiguration(config =>
        {
            config.Property<TestingPerson>(x => x.Title).HasQueryName("special_title");
            config.Property<TestingPerson>(x => x.Id).HasQueryName("identifier").PreventFilter();
        });
        var filterExpression = FilterParser.ParseFilter<TestingPerson>(input, config);
        filterExpression.ToString().Should().Be($"""x => ((x.Title == "{stringValue}") OrElse (True == True))""");
    }
    
    [Fact]
    public void can_have_custom_prop_work_with_collection_filters()
    {
        var faker = new Faker();
        var stringValue = faker.Lorem.Word();
        var input = $"""special_title == "{stringValue}" && Ingredients.Name == "flour" """;

        var config = new QueryKitConfiguration(config =>
        {
            config.Property<Recipe>(x => x.Title).HasQueryName("special_title");
        });
        var filterExpression = FilterParser.ParseFilter<Recipe>(input, config);
        filterExpression.ToString().Should().Be(
            $"""x => ((x.Title == "{stringValue}") AndAlso x.Ingredients.Select(y => y.Name).Any(z => (z == "flour")))""");
    }
    
    [Fact]
    public void can_have_derived_prop_work_with_collection_filters()
    {
        var faker = new Faker();
        var stringValue = faker.Lorem.Word();
        var input = $"""special_title_directions == "{stringValue}" && Ingredients.Name == "flour" """;

        var config = new QueryKitConfiguration(config =>
        {
            config.DerivedProperty<Recipe>(x => x.Title + x.Directions).HasQueryName("special_title_directions");
        });
        var filterExpression = FilterParser.ParseFilter<Recipe>(input, config);
        filterExpression.ToString().Should().Be(
            $"""x => (((x.Title + x.Directions) == "{stringValue}") AndAlso x.Ingredients.Select(y => y.Name).Any(z => (z == "flour")))""");
    }
    
    [Fact]
    public void filter_prevented_props_always_have_true_equals_true_regardless_of_comparison()
    {
        var faker = new Faker();
        var filterOperator = faker.PickRandom(ComparisonOperator.List.Where(x => x != ComparisonOperator.EqualsOperator()).ToList());
        var guidValue = Guid.NewGuid();
        var input = $"""Id {filterOperator} "{guidValue}" """;

        var config = new QueryKitConfiguration(config =>
        {
            config.Property<TestingPerson>(x => x.Id).PreventFilter();
        });
        var filterExpression = FilterParser.ParseFilter<TestingPerson>(input, config);
        filterExpression.ToString().Should().Be($"""x => (True == True)""");
    }
    
    [Fact]
    public void can_throw_error_when_property_has_space()
    {
        var faker = new Faker();
        var propertyName = faker.Lorem.Sentence();
        var firstWord = propertyName.Split(' ').First();
        var input = $"""{propertyName} == 25""";

        var config = new QueryKitConfiguration(config =>
        {
            config.Property<TestingPerson>(x => x.Id).PreventFilter();
        });
        var act = () => FilterParser.ParseFilter<TestingPerson>(input, config);
        act.Should().Throw<UnknownFilterPropertyException>()
            .WithMessage($"The filter property '{firstWord}' was not recognized.");
    }
    
    [Fact]
    public void can_handle_nonexistent_property()
    {
        var faker = new Faker();
        var input = $"""{faker.Lorem.Word()} == 25""";
        
        var config = new QueryKitConfiguration(config =>
        {
            config.AllowUnknownProperties = true;
        });
        var filterExpression = FilterParser.ParseFilter<TestingPerson>(input, config);
        filterExpression.ToString().Should().Be("x => (True == True)");
    }
}