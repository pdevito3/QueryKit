namespace QueryKit.WebApiTestProject.Entities;

public class Address : ValueObject
{
    public string Line1 { get; } = null!;
    public string Line2 { get; } = null!;
    public string City { get; } = null!;
    public string State { get; } = null!;
    public PostalCode PostalCode { get; } = null!;
    public string Country { get; } = null!;
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

    private Address() { }
}