namespace QueryKit.UnitTests;

public class UnitTest1
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
    
    [Fact]
    public void Test1()
    {
        var input =
            "((Title @=* waffle && Age > 30) || Title == lamb) && (Age < 18 || (BirthMonth == January && Title _= ally)) || Rating > 3.5 || SpecificDate == 2022-07-01T00:00:03Z && (Date == 2022-07-01 || Time == 00:00:03)";

// TODO support bool without `==` and with a not???

// input = "Title @=* waffle";
// input = "(Title @=* waffle || Age > 30) || Age < 18";
// input = "Title @=* waffle || Age > 30";
        var filterExpression = FilterParser.ParseFilter<Person>(input);
        Console.WriteLine(filterExpression);
    }
}