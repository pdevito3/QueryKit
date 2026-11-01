namespace QueryKit.Exceptions;

/// <summary>Why a <see cref="QueryKitParsingException"/> was thrown.</summary>
public enum QueryKitParsingReason
{
    /// <summary>A failure with no specific reason.</summary>
    Other = 0,

    /// <summary>The filter or sort used an operator or a direction that QueryKit does not support.</summary>
    UnsupportedOperator = 1,

    /// <summary>The operator needs a list value, for example ["a", "b"].</summary>
    NeedsList = 2,

    /// <summary>The operator does not accept a null value.</summary>
    RefusesNull = 3,

    /// <summary>The operator works only on a collection property.</summary>
    NeedsCollection = 4
}
