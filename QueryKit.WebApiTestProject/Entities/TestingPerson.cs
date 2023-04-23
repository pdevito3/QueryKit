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

public class EmailAddress : ValueObject
{
    public EmailAddress(string? value)
    {
        Value = value;
    }
    
    public string? Value { get; private set; }
}

public class Address : ValueObject
{
    public string Line1 { get; }   
    public string Line2 { get; }
    public string City { get; }
    public string State { get; }
    public PostalCode PostalCode { get; }
    public string Country { get; }
    public Address(string line1, string line2, string city, string state, string postalCode, string country)
        : this(line1, line2, city, state, PostalCode.Of(postalCode), country)
    {
    }

    public Address(string line1, string line2, string city, string state, PostalCode postalCode, string country)
    {
        Line1 = line1;
        Line2 = line2;
        City = city;
        State = state;
        PostalCode = postalCode;
        Country = country;
    }
}

public class PostalCode : ValueObject
{
    public string Value { get; }
    public PostalCode(string value)
    {
        Value = value;
    }

    public static PostalCode Of(string postalCode) => new PostalCode(postalCode);
    public static implicit operator string(PostalCode postalCode) => postalCode.Value;
}