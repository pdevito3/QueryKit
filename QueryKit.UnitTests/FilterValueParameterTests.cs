namespace QueryKit.UnitTests;

using System.Linq.Expressions;
using FluentAssertions;
using WebApiTestProject.Entities;

public class FilterValueParameterTests
{
    [Theory]
    [InlineData("""Title == "lamb" """)]
    [InlineData("""Title @=* "waffle" """)]
    [InlineData("Age > 30")]
    [InlineData("Rating > 3.5")]
    [InlineData("BirthMonth == 1")]
    [InlineData("BirthMonth == \"January\"")]
    [InlineData("Favorite == true")]
    [InlineData("SpecificDate == 2022-07-01T00:00:03Z")]
    [InlineData("SpecificDateTime > 2022-07-01T00:00:03")]
    [InlineData("Date == 2022-07-01")]
    [InlineData("""Time == "00:00:03.123456" """)]
    [InlineData("""Id == "aa648248-cb69-4217-ac95-d7484795afb2" """)]
    [InlineData("""Title ^^ ["lamb", "chicken"]""")]
    [InlineData("""Title ^^* ["lamb", "chicken"]""")]
    [InlineData("""Title !^^ ["lamb", "chicken"]""")]
    [InlineData("""Title !^^* ["lamb", "chicken"]""")]
    [InlineData("Age ^^ [18, 30]")]
    [InlineData("(Age + 5) > 30")]
    public void filter_values_are_field_reads_so_ef_core_sends_them_as_parameters(string input)
    {
        var filterExpression = FilterParser.ParseFilter<TestingPerson>(input);

        var constants = new ConstantCollector();
        constants.Visit(filterExpression);

        constants.Values.Should().NotBeEmpty();
        constants.Values.Should().AllSatisfy(value =>
            value.GetType().GetGenericTypeDefinition().Should().Be(typeof(FilterValue<>)));
    }

    [Fact]
    public void filter_values_keep_their_value_and_type()
    {
        var filterExpression = FilterParser.ParseFilter<TestingPerson>("""Time == "00:00:03.123456" """);

        var comparison = (BinaryExpression)filterExpression.Body;
        var read = (MemberExpression)comparison.Right;
        read.Type.Should().Be(typeof(TimeOnly?));
        Expression.Lambda<Func<TimeOnly?>>(read).Compile()().Should()
            .Be(new TimeOnly(0, 0, 3, 123, 456));
    }

    [Fact]
    public void null_stays_a_constant_so_the_query_uses_is_null()
    {
        var filterExpression = FilterParser.ParseFilter<TestingPerson>("Title == null");

        var comparison = (BinaryExpression)filterExpression.Body;
        comparison.Right.Should().BeAssignableTo<ConstantExpression>()
            .Which.Value.Should().BeNull();
    }

    private sealed class ConstantCollector : ExpressionVisitor
    {
        public List<object> Values { get; } = new();

        protected override Expression VisitConstant(ConstantExpression node)
        {
            if (node.Value != null)
                Values.Add(node.Value);
            return node;
        }
    }
}
