namespace QueryKit.Exceptions;

public sealed class SortParsingException : QueryKitException
{
    public SortParsingException(string propertyName)
        : base($"Parsing failed during sorting. '{propertyName}' was not recognized.")
    {
        PropertyName = propertyName;
    }

    private SortParsingException(string propertyName, string message)
        : base(message)
    {
        PropertyName = propertyName;
    }

    /// <summary>The sort property that failed, as the sort named it.</summary>
    public string PropertyName { get; }

    internal static SortParsingException ForCollection(string propertyName)
        => new(propertyName, $"Parsing failed during sorting. '{propertyName}' is a collection, and a collection has no single sort order.");
}
