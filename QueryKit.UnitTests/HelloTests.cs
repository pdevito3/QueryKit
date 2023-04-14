namespace QueryKit.UnitTests;

using FluentAssertions;

public class HelloTests
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
    }
    
    // var input = """""((Title @=* "waffle & chicken" && Age > 30) || Title == """lamb is great on a "gee-ro" not a "gyro" sandwich""") && (Age < 18 || (BirthMonth == "January" && Title _= "ally")) || Rating > 3.5 || SpecificDate == 2022-07-01T00:00:03Z && (Date == 2022-07-01 || Time == 00:00:03)""""";

    
    
    [Fact]
    public void escaped_double_quote()
    {
        var input = """""Title == """lamb is great on a "gee-ro" not a "gy-ro" sandwich""" """"";

        var filterExpression = FilterParser.ParseFilter<Person>(input);
        var asString = filterExpression.ToString();
        asString.Should()
            .Be(""""x => (x.Title == "lamb is great on a \"gee-ro\" not a \"gy-ro\" sandwich")"""");
    }
    
    [Fact]
    public void complex_with_lots_of_types()
    {
        var input =
            """""((Title @=* "waffle & chicken" && Age > 30) || Title == "lamb") && (Age < 18 || (BirthMonth == "January" && Title _= "ally")) || Rating > 3.5 || SpecificDate == 2022-07-01T00:00:03Z && (Date == 2022-07-01 || Time == 00:00:03)""""";

        var filterExpression = FilterParser.ParseFilter<Person>(input);
        filterExpression.ToString().Should()
            .Be(""""x => (((((x.Title.ToLower().Contains("waffle & chicken".ToLower()) AndAlso (x.Age > 30)) OrElse (x.Title == "lamb")) AndAlso ((x.Age < 18) OrElse ((x.BirthMonth == "January") AndAlso x.Title.StartsWith("ally")))) OrElse (x.Rating > 3.5)) OrElse ((x.SpecificDate == 7/1/2022 12:00:03 AM +00:00) AndAlso ((x.Date == 7/1/2022) OrElse (x.Time == 12:00 AM))))"""");
    }
    
    [Fact]
    public void order_of_ops_quote_on_string()
    {
        var input = """(Title @=* "waffle" || Age > 30) || Age < 18""";
        var filterExpression = FilterParser.ParseFilter<Person>(input);
        filterExpression.ToString().Should()
            .Be(""""x => ((x.Title.ToLower().Contains("waffle".ToLower()) OrElse (x.Age > 30)) OrElse (x.Age < 18))"""");
    }
    
    [Fact]
    public void simple_waffle()
    {
        var input = """"Title @=* "waffle" """";
        var filterExpression = FilterParser.ParseFilter<Person>(input);
        filterExpression.ToString().Should().Be(""""x => x.Title.ToLower().Contains("waffle".ToLower())"""");
    }
}