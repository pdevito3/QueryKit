namespace QueryKit.Exceptions;

public sealed class QueryKitDbContextTypeException : Exception
{
    public QueryKitDbContextTypeException(string specificMessage) 
        : base($"There no DbContext type provided in your QueryKit config, but one was needed for this operation. {specificMessage}")
    {
    }
}
