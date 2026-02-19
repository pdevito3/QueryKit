namespace QueryKit.Configuration;

/// <summary>
/// Controls which SQL case transformation is generated for case-insensitive string operators (e.g. @=*, _=*).
/// </summary>
public enum CaseInsensitiveMode
{
    /// <summary>Default. Generates ToLower() / LOWER() comparisons.</summary>
    Lower = 0,

    /// <summary>Generates ToUpper() / UPPER() comparisons. Useful when data is normalized to uppercase.</summary>
    Upper = 1
}
