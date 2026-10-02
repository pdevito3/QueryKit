namespace QueryKit.IntegrationTests.Tests;

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using QueryKit.Configuration;
using SharedTestingHelper.Fakes;
using WebApiTestProject.Entities;

public class FilterParameterTests() : TestBase
{
    private static readonly QueryKitConfiguration LiteralConfig =
        new(settings => settings.ParameterizeFilterValues = false);

    [Theory]
    [InlineData("""Title == "lamb" """, """Title == "chicken" """)]
    [InlineData("""Title @=* "lamb" """, """Title @=* "chicken" """)]
    [InlineData("Age > 30", "Age > 18")]
    [InlineData("Rating > 3.5", "Rating > 4.5")]
    [InlineData("BirthMonth == 1", "BirthMonth == 2")]
    [InlineData("Favorite == true", "Favorite == false")]
    [InlineData("SpecificDate > 2022-07-01T00:00:03Z", "SpecificDate > 2023-01-01T00:00:00Z")]
    [InlineData("SpecificDateTime > 2022-07-01T00:00:03Z", "SpecificDateTime > 2023-01-01T00:00:00Z")]
    [InlineData("Date == 2022-07-01", "Date == 2023-01-01")]
    [InlineData("Time == 00:00:03", "Time == 12:30:00")]
    [InlineData("""Id == "aa648248-cb69-4217-ac95-d7484795afb2" """, """Id == "bb648248-cb69-4217-ac95-d7484795afb2" """)]
    [InlineData("""Title ^^ ["lamb", "chicken"]""", """Title ^^ ["beef", "pork", "tofu"]""")]
    [InlineData("""Title ^^* ["lamb", "chicken"]""", """Title ^^* ["beef", "pork", "tofu"]""")]
    [InlineData("""Title !^^ ["lamb", "chicken"]""", """Title !^^ ["beef", "pork", "tofu"]""")]
    [InlineData("(Age + 5) > 30", "(Age + 7) > 18")]
    public void filters_that_differ_only_in_values_share_one_parameterized_query(string first, string second)
    {
        var testingServiceScope = new TestingServiceScope();

        var firstSql = SqlWithoutParameterValues(testingServiceScope, first);
        var secondSql = SqlWithoutParameterValues(testingServiceScope, second);

        firstSql.Should().Contain("@");
        firstSql.Should().Be(secondSql);
    }

    [Fact]
    public async Task in_list_is_one_array_parameter_and_still_filters()
    {
        var testingServiceScope = new TestingServiceScope();
        var lamb = new FakeTestingPersonBuilder().WithTitle($"lamb {Guid.NewGuid()}").Build();
        var chicken = new FakeTestingPersonBuilder().WithTitle($"chicken {Guid.NewGuid()}").Build();
        var beef = new FakeTestingPersonBuilder().WithTitle($"beef {Guid.NewGuid()}").Build();
        await testingServiceScope.InsertAsync(lamb, chicken, beef);

        var input = $"""Title ^^* ["{lamb.Title!.ToUpper()}", "{chicken.Title}"]""";
        var query = testingServiceScope.DbContext().People.ApplyQueryKitFilter(input);
        var people = await query.ToListAsync();

        SqlWithoutParameterValues(query).Should().Contain("= ANY (@");
        people.Select(x => x.Id).Should().BeEquivalentTo(new[] { lamb.Id, chicken.Id });
    }

    [Theory]
    [InlineData("""Title == "lamb" """, "'lamb'")]
    [InlineData("Age > 30", "> 30")]
    [InlineData("Date == 2022-07-01", "DATE '2022-07-01'")]
    [InlineData("(Age + 5) > 30", "+ 5")]
    public void filter_values_are_sql_literals_when_parameters_are_off(string input, string expectedLiteral)
    {
        var testingServiceScope = new TestingServiceScope();

        var sql = testingServiceScope.DbContext().People.ApplyQueryKitFilter(input, LiteralConfig).ToQueryString();

        sql.Should().NotContain("@");
        sql.Should().Contain(expectedLiteral);
    }

    [Fact]
    public async Task in_list_is_a_literal_list_when_parameters_are_off_and_still_filters()
    {
        var testingServiceScope = new TestingServiceScope();
        var lamb = new FakeTestingPersonBuilder().WithTitle($"lamb {Guid.NewGuid()}").Build();
        var chicken = new FakeTestingPersonBuilder().WithTitle($"chicken {Guid.NewGuid()}").Build();
        var beef = new FakeTestingPersonBuilder().WithTitle($"beef {Guid.NewGuid()}").Build();
        await testingServiceScope.InsertAsync(lamb, chicken, beef);

        var input = $"""Title ^^ ["{lamb.Title}", "{chicken.Title}"]""";
        var query = testingServiceScope.DbContext().People.ApplyQueryKitFilter(input, LiteralConfig);
        var people = await query.ToListAsync();

        query.ToQueryString().Should().Contain($"IN ('{lamb.Title}', '{chicken.Title}')");
        people.Select(x => x.Id).Should().BeEquivalentTo(new[] { lamb.Id, chicken.Id });
    }

    private static string SqlWithoutParameterValues(TestingServiceScope testingServiceScope, string input)
        => SqlWithoutParameterValues(testingServiceScope.DbContext().People.ApplyQueryKitFilter(input));

    // ToQueryString() writes each parameter value in a "-- @p='...'" comment line before the SQL.
    private static string SqlWithoutParameterValues(IQueryable<TestingPerson> query)
        => string.Join('\n', query.ToQueryString()
            .Split('\n')
            .Where(line => !line.StartsWith("--")));
}
