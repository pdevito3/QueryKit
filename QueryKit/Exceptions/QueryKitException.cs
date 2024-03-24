namespace QueryKit.Exceptions;

/// <summary>
/// Base QueryKit exception, all exceptions thrown by QueryKit inherit this.
/// </summary>
public class QueryKitException : Exception
{
    public QueryKitException(string message, Exception exception) : base(message, exception) {}
    public QueryKitException(string message) : base(message) {}
}
