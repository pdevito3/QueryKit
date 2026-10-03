namespace QueryKit.UnitTests;

using System.Linq.Expressions;
using Configuration;
using FluentAssertions;
using WebApiTestProject.Entities;

// Npgsql needs a Utc DateTime for a timestamptz column and rejects a Utc DateTime for a timestamp without
// time zone column. DateTimeKindForValuesWithoutOffset sets the kind of a value that has no offset.
public class DateTimeKindForValuesWithoutOffsetTests
{
    private static readonly DateTime EightUtc = new(2024, 1, 15, 8, 0, 0, DateTimeKind.Utc);

    public static TheoryData<string> DateTimeInputs => new()
    {
        "SpecificDateTime == 2024-01-15T08:00:00",
        "SpecificDateTime == \"2024-01-15T08:00:00\"",
        "SpecificDateTime ^^ [2024-01-15T08:00:00]",
        "SpecificDateTime ^^ [\"2024-01-15T08:00:00\"]",
        "stamp == 2024-01-15T08:00:00",
    };

    [Theory]
    [MemberData(nameof(DateTimeInputs))]
    public void value_without_offset_is_utc_by_default(string input)
    {
        foreach (var parameterize in new[] { false, true })
        {
            var config = Config(parameterize, kind: null);

            DateTimesIn(input, config).Should().Equal((EightUtc.Ticks, DateTimeKind.Utc));
        }
    }

    [Theory]
    [MemberData(nameof(DateTimeInputs))]
    public void value_without_offset_is_unspecified_when_configured(string input)
    {
        foreach (var parameterize in new[] { false, true })
        {
            var config = Config(parameterize, DateTimeKind.Unspecified);

            DateTimesIn(input, config).Should().Equal((EightUtc.Ticks, DateTimeKind.Unspecified));
        }
    }

    [Theory]
    [MemberData(nameof(DateTimeInputs))]
    public void value_without_offset_is_local_when_configured(string input)
    {
        foreach (var parameterize in new[] { false, true })
        {
            var config = Config(parameterize, DateTimeKind.Local);

            DateTimesIn(input, config).Should().Equal((EightUtc.Ticks, DateTimeKind.Local));
        }
    }

    [Theory]
    [InlineData("SpecificDateTime == 2024-01-15T10:00:00+02:00")]
    [InlineData("SpecificDateTime == 2024-01-15T08:00:00Z")]
    [InlineData("SpecificDateTime ^^ [2024-01-15T10:00:00+02:00]")]
    [InlineData("stamp == 2024-01-15T10:00:00+02:00")]
    public void value_with_offset_is_the_utc_time_when_unspecified_is_configured(string input)
    {
        var config = Config(parameterize: false, DateTimeKind.Unspecified);

        DateTimesIn(input, config).Should().Equal((EightUtc.Ticks, DateTimeKind.Unspecified));
    }

    [Fact]
    public void value_with_offset_is_the_local_time_when_local_is_configured()
    {
        var config = Config(parameterize: false, DateTimeKind.Local);
        var expected = EightUtc.ToLocalTime();

        DateTimesIn("SpecificDateTime == 2024-01-15T08:00:00Z", config).Should().Equal((expected.Ticks, DateTimeKind.Local));
    }

    [Theory]
    [InlineData(null, "2024-01-15T08:00:00+00:00")]
    [InlineData(DateTimeKind.Utc, "2024-01-15T08:00:00+00:00")]
    [InlineData(DateTimeKind.Unspecified, "2024-01-15T08:00:00+00:00")]
    public void date_time_offset_without_offset_is_utc_unless_local_is_configured(DateTimeKind? kind, string expected)
    {
        foreach (var input in new[] { "SpecificDate == 2024-01-15T08:00:00", "SpecificDate ^^ [2024-01-15T08:00:00]" })
        {
            var values = ValuesIn(FilterParser.ParseFilter<TestingPerson>(input, Config(parameterize: false, kind)));

            values.Should().Equal(DateTimeOffset.Parse(expected));
            values.Cast<DateTimeOffset>().Should().OnlyContain(x => x.Offset == TimeSpan.Zero);
        }
    }

    [Fact]
    public void date_time_offset_without_offset_is_local_when_local_is_configured()
    {
        var expected = new DateTimeOffset(DateTime.SpecifyKind(EightUtc, DateTimeKind.Local)).ToUniversalTime();

        foreach (var input in new[] { "SpecificDate == 2024-01-15T08:00:00", "SpecificDate ^^ [2024-01-15T08:00:00]" })
        {
            var values = ValuesIn(FilterParser.ParseFilter<TestingPerson>(input, Config(parameterize: false, DateTimeKind.Local)));

            values.Should().Equal(expected);
            values.Cast<DateTimeOffset>().Should().OnlyContain(x => x.Offset == TimeSpan.Zero);
        }
    }

    [Fact]
    public void setting_applies_only_to_the_filter_that_it_configures()
    {
        DateTimesIn("SpecificDateTime == 2024-01-15T08:00:00", Config(parameterize: false, DateTimeKind.Unspecified));

        DateTimesIn("SpecificDateTime == 2024-01-15T08:00:00", config: null).Should().Equal((EightUtc.Ticks, DateTimeKind.Utc));
    }

    [Fact]
    public void configuration_without_the_setting_uses_utc()
    {
        IQueryKitFilterBehavior behavior = new FilterBehaviorWithoutTheSetting();

        behavior.DateTimeKindForValuesWithoutOffset.Should().Be(DateTimeKind.Utc);
    }

    private static QueryKitConfiguration Config(bool parameterize, DateTimeKind? kind)
        => new(settings =>
        {
            settings.ParameterizeFilterValues = parameterize;
            if (kind is not null)
            {
                settings.DateTimeKindForValuesWithoutOffset = kind.Value;
            }
            settings.CustomOperation<TestingPerson>((x, op, value) => x.SpecificDateTime == (DateTime)value)
                .HasQueryName("stamp");
        });

    private static List<(long Ticks, DateTimeKind Kind)> DateTimesIn(string input, QueryKitConfiguration? config)
        => ValuesIn(FilterParser.ParseFilter<TestingPerson>(input, config))
            .Cast<DateTime>()
            .Select(x => (x.Ticks, x.Kind))
            .ToList();

    private static List<object> ValuesIn(Expression expression)
    {
        var collector = new DateValueCollector();
        collector.Visit(expression);
        return collector.Values;
    }

    // Collects the date values of a filter, both from a literal and from a parameter.
    private sealed class DateValueCollector : ExpressionVisitor
    {
        public List<object> Values { get; } = [];

        protected override Expression VisitNew(NewExpression node)
        {
            if (node.Type != typeof(DateTime) && node.Type != typeof(DateTimeOffset))
            {
                return base.VisitNew(node);
            }

            Add(Expression.Lambda(node).Compile().DynamicInvoke());
            return node;
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            Add(node.Value);
            return node;
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            if (node.Expression is not ConstantExpression)
            {
                return base.VisitMember(node);
            }

            Add(Expression.Lambda(node).Compile().DynamicInvoke());
            return node;
        }

        private void Add(object? value)
        {
            switch (value)
            {
                case DateTime or DateTimeOffset:
                    Values.Add(value);
                    break;
                case System.Collections.IEnumerable list and not string:
                    Values.AddRange(list.Cast<object>().Where(x => x is DateTime or DateTimeOffset));
                    break;
            }
        }
    }

    private sealed class FilterBehaviorWithoutTheSetting : IQueryKitFilterBehavior
    {
        public bool ParameterizeFilterValues => false;
        public IgnoredClauseBehavior IgnoredClauseBehavior => IgnoredClauseBehavior.Remove;
    }
}
