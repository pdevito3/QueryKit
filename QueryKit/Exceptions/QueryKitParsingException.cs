namespace QueryKit.Exceptions;

public sealed class QueryKitParsingException : QueryKitException
{
    public QueryKitParsingException(string message)
        : this(message, QueryKitParsingReason.Other, null)
    {
    }

    internal QueryKitParsingException(string message, QueryKitParsingReason reason, string? @operator)
        : base(message)
    {
        Reason = reason;
        Operator = @operator;
    }

    /// <summary>Why the parse failed.</summary>
    public QueryKitParsingReason Reason { get; }

    /// <summary>The operator that the failure is about, as the filter wrote it. Null when the failure has no operator.</summary>
    public string? Operator { get; }
}
