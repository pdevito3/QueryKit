namespace QueryKit.Exceptions;

public sealed class UnknownFilterPropertyException : QueryKitException
{
    public UnknownFilterPropertyException(string propertyName)
        : base($"The filter property '{propertyName}' was not recognized.")
    {
        PropertyName = propertyName;
    }

    /// <summary>The filter property that was not recognized, as the filter named it.</summary>
    public string PropertyName { get; }
}
