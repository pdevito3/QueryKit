namespace QueryKit.Exceptions;

public sealed class UnknownFilterPropertyException : Exception
{
    public UnknownFilterPropertyException(string propertyName) 
        : base($"The filter property '{propertyName}' was not recognized.")
    {
    }
}
