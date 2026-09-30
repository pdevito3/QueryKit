namespace QueryKit.Configuration;

/// <summary>
/// Controls what the filter parser does with a clause that it ignores: a clause on a property that has
/// PreventFilter, or on an unknown property when AllowUnknownProperties is true.
/// </summary>
public enum IgnoredClauseBehavior
{
    /// <summary>Replaces the clause with (true == true), the same as v1.14.2. Under an OR, the whole OR is then true.</summary>
    ReplaceWithTrue = 0,

    /// <summary>Default. Removes the clause. A logical operator with a removed side keeps only its other side.</summary>
    Remove = 1
}
