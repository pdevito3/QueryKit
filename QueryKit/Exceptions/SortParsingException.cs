namespace QueryKit.Exceptions;

public sealed class SortParsingException : Exception
{
    public SortParsingException(string propertyName) 
        : base($"Parsing failed during sorting. '{propertyName}' was not recognized.")
    {
    }
}
