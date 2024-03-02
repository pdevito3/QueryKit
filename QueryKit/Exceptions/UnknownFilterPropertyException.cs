namespace QueryKit.Exceptions;

public sealed class UnknownFilterPropertyException : QueryKitException
{
    public UnknownFilterPropertyException(string propertyName)
        : base($"The filter property '{propertyName}' was not recognized.")
    {
    }
}
