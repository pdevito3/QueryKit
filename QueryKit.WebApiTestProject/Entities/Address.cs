namespace QueryKit.WebApiTestProject.Entities;

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

    private Address() { }
}