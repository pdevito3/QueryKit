namespace QueryKit.Exceptions;

public sealed class SortParsingException : QueryKitException
{
    public SortParsingException(string propertyName)
        : base($"Parsing failed during sorting. '{propertyName}' was not recognized.")
    {
    }

    private SortParsingException(string message, bool isMessage)
        : base(message)
    {
    }

    internal static SortParsingException ForCollection(string propertyName)
        => new($"Parsing failed during sorting. '{propertyName}' is a collection, and a collection has no single sort order.", isMessage: true);
}
