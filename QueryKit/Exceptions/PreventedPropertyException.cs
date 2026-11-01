namespace QueryKit.Exceptions;

/// <summary>
/// A filter or a sort used a property that has PreventFilter or PreventSort, and the configuration has
/// <see cref="Configuration.IgnoredClauseBehavior.Throw"/>.
/// </summary>
public sealed class PreventedPropertyException : QueryKitException
{
    private PreventedPropertyException(string propertyName, string message)
        : base(message)
    {
        PropertyName = propertyName;
    }

    /// <summary>The prevented property, as the filter or the sort named it.</summary>
    public string PropertyName { get; }

    internal static PreventedPropertyException ForFilter(string propertyName)
        => new(propertyName, $"The property '{propertyName}' cannot be used in a filter.");

    internal static PreventedPropertyException ForSort(string propertyName)
        => new(propertyName, $"The property '{propertyName}' cannot be used in a sort.");
}
