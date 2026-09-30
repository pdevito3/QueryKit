namespace QueryKit.UnitTests;

using System.Linq.Expressions;
using Configuration;
using FluentAssertions;
using WebApiTestProject.Entities;

public class FilterBehaviorInterfaceTests
{
    [Fact]
    public void configuration_that_implements_only_the_interface_parameterizes_filter_values()
    {
        var config = new InterfaceOnlyConfiguration { ParameterizeFilterValues = true };

        var filterExpression = FilterParser.ParseFilter<TestingPerson>("""Title == "lamb" """, config);

        var comparison = (BinaryExpression)filterExpression.Body;
        comparison.Right.Should().BeAssignableTo<MemberExpression>();
    }

    [Fact]
    public void configuration_that_implements_only_the_interface_removes_ignored_clauses()
    {
        var config = new InterfaceOnlyConfiguration
        {
            AllowUnknownProperties = true,
            IgnoredClauseBehavior = IgnoredClauseBehavior.Remove
        };
        var input = """Nope == "x" || Age > 100""";

        var filterExpression = FilterParser.ParseFilter<TestingPerson>(input, config);

        filterExpression.ToDisplayString().Should().Be("x => (x.Age > 100)");
    }

    // Implements only the IQueryKitConfiguration members of v1.14.2 plus IQueryKitFilterBehavior.
    // This class does not compile when either interface gets a new member.
    private sealed class InterfaceOnlyConfiguration : IQueryKitConfiguration, IQueryKitFilterBehavior
    {
        public QueryKitPropertyMappings PropertyMappings { get; } = new();
        public string EqualsOperator { get; set; } = "==";
        public string NotEqualsOperator { get; set; } = "!=";
        public string GreaterThanOperator { get; set; } = ">";
        public string LessThanOperator { get; set; } = "<";
        public string GreaterThanOrEqualOperator { get; set; } = ">=";
        public string LessThanOrEqualOperator { get; set; } = "<=";
        public string ContainsOperator { get; set; } = "@=";
        public string StartsWithOperator { get; set; } = "_=";
        public string EndsWithOperator { get; set; } = "_-=";
        public string NotContainsOperator { get; set; } = "!@=";
        public string NotStartsWithOperator { get; set; } = "!_=";
        public string NotEndsWithOperator { get; set; } = "!_-=";
        public string InOperator { get; set; } = "^^";
        public string NotInOperator { get; set; } = "!^^";
        public string SoundsLikeOperator { get; set; } = "~~";
        public string DoesNotSoundLikeOperator { get; set; } = "!~";
        public string CaseInsensitiveAppendix { get; set; } = "*";
        public string AndOperator { get; set; } = "&&";
        public string OrOperator { get; set; } = "||";
        public bool AllowUnknownProperties { get; set; }
        public Type? DbContextType { get; set; }
        public string HasCountEqualToOperator { get; set; } = "#==";
        public string HasCountNotEqualToOperator { get; set; } = "#!=";
        public string HasCountGreaterThanOperator { get; set; } = "#>";
        public string HasCountLessThanOperator { get; set; } = "#<";
        public string HasCountGreaterThanOrEqualOperator { get; set; } = "#>=";
        public string HasCountLessThanOrEqualOperator { get; set; } = "#<=";
        public string HasOperator { get; set; } = "^$";
        public string DoesNotHaveOperator { get; set; } = "!^$";
        public int? MaxPropertyDepth { get; set; }
        public CaseInsensitiveMode CaseInsensitiveComparison { get; set; } = CaseInsensitiveMode.Lower;
        public bool ParameterizeFilterValues { get; set; }
        public IgnoredClauseBehavior IgnoredClauseBehavior { get; set; }
    }
}
