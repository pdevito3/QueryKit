namespace QueryKit;

internal sealed class FilterParseScope : IDisposable
{
    // Parsing is synchronous, so the thread that parses owns the scope. An async parse would need AsyncLocal.
    [ThreadStatic] private static FilterParseScope? _current;

    private readonly FilterParseScope? _previous;

    private FilterParseScope(bool parameterize, int maxNestingDepth)
    {
        Parameterize = parameterize;
        MaxNestingDepth = maxNestingDepth;
        _previous = _current;
        _current = this;
    }

    internal static FilterParseScope? Current => _current;

    internal static FilterParseScope Begin(bool parameterize, int maxNestingDepth) => new(parameterize, maxNestingDepth);

    internal bool Parameterize { get; }

    internal int MaxNestingDepth { get; }

    internal int NestingDepth { get; set; }

    internal bool QueryNameHidUnknownIdentifier { get; set; }
    internal bool RetryingForLegacyException { get; set; }

    public void Dispose() => _current = _previous;
}
