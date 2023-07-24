namespace QueryKit.Exceptions;

public sealed class QueryKitParsingException : Exception
{
    public QueryKitParsingException(string message) 
        : base(message)
    {
    }
}
