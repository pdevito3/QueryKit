namespace QueryKit.WebApiTestProject.Entities;

public class TestingPerson
{
    public string? Title { get; set; }
    public int? Age { get; set; }
    public BirthMonthEnum? BirthMonth { get; set; }
    public decimal? Rating { get; set; }
    public DateOnly? Date { get; set; }
    public bool? Favorite { get; set; }
    public DateTimeOffset? SpecificDate { get; set; }
    public DateTime SpecificDateTime { get; set; }
    public TimeOnly? Time { get; set; }
    public Guid Id { get; set; } = Guid.NewGuid();
    public EmailAddress Email { get; set; }
    public Address PhysicalAddress { get; set; }
}

public enum BirthMonthEnum
{
    January = 1,
    February = 2,
    March = 3,
    April = 4,
    May = 5,
    June = 6,
    July = 7,
    August = 8,
    September = 9,
    October = 10,
    November = 11,
    December = 12
}