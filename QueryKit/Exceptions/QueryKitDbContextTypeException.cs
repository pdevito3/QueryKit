namespace QueryKit.Exceptions;

public sealed class QueryKitDbContextTypeException : QueryKitException
{
    public QueryKitDbContextTypeException(string specificMessage)
        : base($"There no DbContext type provided in your QueryKit config, but one was needed for this operation. {specificMessage}")
    {
    }
}
