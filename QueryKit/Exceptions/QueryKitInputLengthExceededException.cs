namespace QueryKit.Exceptions;

public sealed class QueryKitInputLengthExceededException : QueryKitException
{
    public QueryKitInputLengthExceededException(int length, int maxLength)
        : base($"The filter has a length of {length}, which exceeds the maximum allowed length of {maxLength}.")
    {
    }
}
