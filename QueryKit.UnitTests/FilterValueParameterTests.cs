namespace QueryKit.UnitTests;

using System.Linq.Expressions;
using FluentAssertions;
using QueryKit.Configuration;
using WebApiTestProject.Entities;

public class FilterValueParameterTests
{
    private static readonly QueryKitConfiguration LiteralConfig =
        new(settings => settings.ParameterizeFilterValues = false);

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

    [Theory]
    [InlineData("""Title == "lamb" """, """x => (x.Title == "lamb")""")]
    [InlineData("Age > 30", "x => (x.Age > 30)")]
    [InlineData("BirthMonth == \"January\"", "x => (x.BirthMonth == new Nullable`1(January))")]
    [InlineData("SpecificDate == 2022-07-01T00:00:03Z",
        "x => (x.SpecificDate == new Nullable`1(new DateTimeOffset(637922304030000000, 00:00:00)))")]
    [InlineData("Date == 2022-07-01", "x => (x.Date == new Nullable`1(new DateOnly(2022, 7, 1)))")]
    [InlineData("""Time == "00:00:03.123456" """, "x => (x.Time == new Nullable`1(new TimeOnly(0, 0, 3, 123, 456)))")]
    [InlineData("(Age + 5) > 30", "x => ((x.Age + Convert(5, Nullable`1)) > Convert(30, Nullable`1))")]
    public void filter_values_are_constants_when_parameters_are_off(string input, string expected)
    {
        var filterExpression = FilterParser.ParseFilter<TestingPerson>(input, LiteralConfig);

        filterExpression.ToString().Should().Be(expected);
    }

    [Theory]
    [InlineData("""Title == "lamb" """)]
    [InlineData("SpecificDateTime > 2022-07-01T00:00:03")]
    [InlineData("""Title ^^ ["lamb", "chicken"]""")]
    [InlineData("""Title ^^* ["lamb", "chicken"]""")]
    [InlineData("Age ^^ [18, 30]")]
    [InlineData("(Age + 5) > 30")]
    public void filter_values_do_not_use_the_holder_when_parameters_are_off(string input)
    {
        var filterExpression = FilterParser.ParseFilter<TestingPerson>(input, LiteralConfig);

        var constants = new ConstantCollector();
        constants.Visit(filterExpression);

        constants.Values.Should().NotBeEmpty();
        constants.Values.Should().NotContain(value =>
            value.GetType().IsGenericType && value.GetType().GetGenericTypeDefinition() == typeof(FilterValue<>));
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
