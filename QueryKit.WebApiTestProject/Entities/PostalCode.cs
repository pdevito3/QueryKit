namespace QueryKit.WebApiTestProject.Entities;

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