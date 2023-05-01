namespace QueryKit.WebApiTestProject.Entities;

public class TestingPerson
{
    public string? Title { get; set; }
    public int? Age { get; set; }
    public string? BirthMonth { get; set; }
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