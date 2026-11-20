namespace QueryKit.UnitTests;

using System.Linq.Expressions;
using Exceptions;
using FluentAssertions;
using QueryKit.Configuration;
using WebApiTestProject.Entities;

public class NestedParseTests
{
    [Fact]
    public void a_parse_inside_a_parse_keeps_the_settings_of_the_outer_parse()
    {
        var config = new QueryKitConfiguration(settings =>
        {
            settings.ParameterizeFilterValues = true;
            settings.MaxNestingDepth = 1;
            settings.Property<Row>(x => x.Id).HasConversion<string>();
        });

        IdThatParsesOnConstruction.NestedParses = 0;
        var filter = FilterParser.ParseFilter<Row>("""Id ^^ ["1"] && (Name == "one")""", config);

        IdThatParsesOnConstruction.NestedParses.Should().BeGreaterThan(0);
        var holders = new MemberCollector();
        holders.Visit(filter);
        holders.HolderTypes.Should().Contain(typeof(FilterValue<string>));

        var tooDeep = () => FilterParser.ParseFilter<Row>("""Id ^^ ["1"] && ((Name == "one"))""", config);
        tooDeep.Should().Throw<QueryKitNestingDepthExceededException>();
    }

    private class Row
    {
        public IdThatParsesOnConstruction Id { get; set; } = null!;
        public string Name { get; set; } = null!;
    }

    private class IdThatParsesOnConstruction
    {
        public IdThatParsesOnConstruction(string value)
        {
            Value = value;
            FilterParser.ParseFilter<TestingPerson>("((Age > 1))");
            NestedParses++;
        }

        public static int NestedParses { get; set; }

        public string Value { get; }
    }

    private sealed class MemberCollector : ExpressionVisitor
    {
        public List<Type> HolderTypes { get; } = new();

        protected override Expression VisitMember(MemberExpression node)
        {
            if (node.Expression is ConstantExpression constant)
                HolderTypes.Add(constant.Type);
            return base.VisitMember(node);
        }
    }
}
