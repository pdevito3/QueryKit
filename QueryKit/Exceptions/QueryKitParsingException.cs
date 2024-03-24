namespace QueryKit.Exceptions;

public sealed class QueryKitParsingException : QueryKitException
{
    public QueryKitParsingException(string message)
        : base(message)
    {
    }
}
