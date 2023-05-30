namespace QueryKit.UnitTests;

using Configuration;
using FluentAssertions;
using WebApiTestProject.Entities.Recipes;

public class OperatorAliasTests
{
    [Fact]
    public void can_handle_alias_text_with_space()
    {
        var input = """Title ti "titilating" """;
        
        var config = new QueryKitConfiguration(config =>
        {
            config.EqualsOperator = "ti";
        });
        var filterExpression = FilterParser.ParseFilter<Recipe>(input, config);
        filterExpression.ToString().Should().Be($"""x => (x.Title == "titilating")""");
    }

    [Fact]
    public void can_handle_alias_text_with_casing_and_space()
    {
        var input = """Title ti "titilating ties a ti" || Title Ti "titilater" """;

        var config = new QueryKitConfiguration(config =>
        {
            config.EqualsOperator = "ti";
        });
        var filterExpression = FilterParser.ParseFilter<Recipe>(input, config);
        filterExpression.ToString().Should().Be($"""x => ((x.Title == "titilating ties a ti") OrElse (x.Title == "titilater"))""");
    }

    [Fact]
    public void can_do_symbol_alias()
    {
        var input = """Title @ "titilating" """;

        var config = new QueryKitConfiguration(config =>
        {
            config.EqualsOperator = "@";
        });
        var filterExpression = FilterParser.ParseFilter<Recipe>(input, config);
        filterExpression.ToString().Should().Be($"""x => (x.Title == "titilating")""");
    }

    [Fact]
    public void can_alias_logical_operator_or()
    {
        var input = """Title eq "titilating" or Rating > 3""";

        var config = new QueryKitConfiguration(config =>
        {
            config.EqualsOperator = "eq";
            config.OrOperator = "or";
        });
        var filterExpression = FilterParser.ParseFilter<Recipe>(input, config);
        filterExpression.ToString().Should().Be($"""x => ((x.Title == "titilating") OrElse (x.Rating > 3))""");
    }

    [Fact]
    public void can_alias_logical_operator_and()
    {
        var input = """Title eq "titilating" and Rating > 3""";

        var config = new QueryKitConfiguration(config =>
        {
            config.EqualsOperator = "eq";
            config.AndOperator = "and";
        });
        var filterExpression = FilterParser.ParseFilter<Recipe>(input, config);
        filterExpression.ToString().Should().Be($"""x => ((x.Title == "titilating") AndAlso (x.Rating > 3))""");
    }

    [Fact]
    public void can_alias_logical_operator_with_conflict()
    {
        var input = """Title eq "titilating or" or Rating > 3""";

        var config = new QueryKitConfiguration(config =>
        {
            config.EqualsOperator = "eq";
            config.OrOperator = "or";
        });
        var filterExpression = FilterParser.ParseFilter<Recipe>(input, config);
        filterExpression.ToString().Should().Be($"""x => ((x.Title == "titilating or") OrElse (x.Rating > 3))""");
    }

    [Fact]
    public void can_do_symbols_alias()
    {
        var input = """Title @@$ "titilating" """;
        
        var config = new QueryKitConfiguration(config =>
        {
            config.EqualsOperator = "@@$";
        });
        var filterExpression = FilterParser.ParseFilter<Recipe>(input, config);
        filterExpression.ToString().Should().Be($"""x => (x.Title == "titilating")""");
    }

    [Fact]
    public void can_do_symbols_alias_with_default_case_insensitive()
    {
        var input = """Title @@$* "titilating" """;
        
        var config = new QueryKitConfiguration(config =>
        {
            config.EqualsOperator = "@@$";
        });
        var filterExpression = FilterParser.ParseFilter<Recipe>(input, config);
        filterExpression.ToString().Should().Be($"""x => (x.Title.ToLower() == "titilating".ToLower())""");
    }

    [Fact]
    public void can_do_symbols_alias_case_insensitive()
    {
        var input = """Title @@$~ "titilating" """;
        
        var config = new QueryKitConfiguration(config =>
        {
            config.EqualsOperator = "@@$";
            config.CaseInsensitiveAppendix = "~";
        });
        var filterExpression = FilterParser.ParseFilter<Recipe>(input, config);
        filterExpression.ToString().Should().Be($"""x => (x.Title.ToLower() == "titilating".ToLower())""");
    }

    [Fact]
    public void can_do_symbols_alias_with_case_insensitive_conflicting_chars()
    {
        var input = """Title @@$ "titilating" """;
        
        var config = new QueryKitConfiguration(config =>
        {
            config.EqualsOperator = "@@$";
            config.CaseInsensitiveAppendix = "$";
        });
        var filterExpression = FilterParser.ParseFilter<Recipe>(input, config);
        filterExpression.ToString().Should().Be($"""x => (x.Title == "titilating")""");
    }

    [Fact]
    public void can_use_contains_not_case_sensitive()
    {
        var input = """Title @@$ "titilating" """;
        
    
        var config = new QueryKitConfiguration(config =>
        {
            config.ContainsOperator = "@@$";
            config.CaseInsensitiveAppendix = "$";
        });
        var filterExpression = FilterParser.ParseFilter<Recipe>(input, config);
        filterExpression.ToString().Should().Be($"""x => x.Title.Contains("titilating")""");
    }

    [Fact]
    public void can_do_symbols_alias_case_insensitive_with_conflicting_chars()
    {
        var input = """Title @@$$ "titilating" """;
        
        var config = new QueryKitConfiguration(config =>
        {
            config.EqualsOperator = "@@$";
            config.CaseInsensitiveAppendix = "$";
        });
        var filterExpression = FilterParser.ParseFilter<Recipe>(input, config);
        filterExpression.ToString().Should().Be($"""x => (x.Title.ToLower() == "titilating".ToLower())""");
    }

    [Fact]
    public void can_do_symbols_alias_case_insensitive_with_no_symbols()
    {
        var input = """Title eqt "titilating" """;
        
        var config = new QueryKitConfiguration(config =>
        {
            config.EqualsOperator = "eq";
            config.CaseInsensitiveAppendix = "t";
        });
        var filterExpression = FilterParser.ParseFilter<Recipe>(input, config);
        filterExpression.ToString().Should().Be($"""x => (x.Title.ToLower() == "titilating".ToLower())""");
    }

    [Fact]
    public void can_have_custom_config_object()
    {
        var input = """Title eq "titilating" """;

        var config = new CustomQueryKitConfiguration();
        var filterExpression = FilterParser.ParseFilter<Recipe>(input, config);
        filterExpression.ToString().Should().Be($"""x => (x.Title == "titilating")""");
    }

    [Fact]
    public void can_use_ints()
    {
        var input = """Rating gt 10""";

        var config = new CustomQueryKitConfiguration();
        var filterExpression = FilterParser.ParseFilter<Recipe>(input, config);
        filterExpression.ToString().Should().Be($"""x => (x.Rating > 10)""");
    }

    public class CustomQueryKitConfiguration : QueryKitConfiguration
    {
        public CustomQueryKitConfiguration(Action<QueryKitSettings>? configureSettings = null)
            : base(settings => 
            {
                settings.EqualsOperator = "eq";
                settings.NotEqualsOperator = "neq";
                settings.GreaterThanOperator = "gt";
                settings.GreaterThanOrEqualOperator = "gte";
                settings.LessThanOperator = "lt";
                settings.LessThanOrEqualOperator = "lte";
                settings.ContainsOperator = "ct";
                settings.StartsWithOperator = "sw";
                settings.EndsWithOperator = "ew";
                settings.NotContainsOperator = "nct";
                settings.NotStartsWithOperator = "nsw";
                settings.NotEndsWithOperator = "new";
                settings.AndOperator = "and";
                settings.OrOperator = "or";
                settings.CaseInsensitiveAppendix = "i";

                configureSettings?.Invoke(settings);
            })
        {
        }
    }
}