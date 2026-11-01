namespace QueryKit.Configuration;

/// <summary>
/// Controls what the filter parser does with a clause that it ignores: a clause on a property that has
/// PreventFilter, or on an unknown property when AllowUnknownProperties is true. Throw also applies to a sort
/// on a property that has PreventSort.
/// </summary>
public enum IgnoredClauseBehavior
{
    /// <summary>Default. Replaces the clause with (true == true). Under an OR, the whole OR is then true.</summary>
    ReplaceWithTrue = 0,

    /// <summary>Removes the clause. A logical operator with a removed side keeps only its other side.</summary>
    Remove = 1,

    /// <summary>
    /// Throws instead of ignoring the clause. A property that has PreventFilter or PreventSort throws
    /// PreventedPropertyException. An unknown filter property throws UnknownFilterPropertyException, also when
    /// AllowUnknownProperties is true.
    /// </summary>
    Throw = 2
}
