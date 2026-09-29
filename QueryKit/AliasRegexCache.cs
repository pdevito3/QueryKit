namespace QueryKit;

using System.Collections.Concurrent;
using System.Text.RegularExpressions;

// Keeps one Regex for each alias pattern, so a parse does not build a new Regex for each alias.
// The patterns come only from the configuration (query names and operator aliases), never from
// the filter text, so the number of entries stays small.
internal static class AliasRegexCache
{
    private static readonly ConcurrentDictionary<string, Regex> Cache = new();

    public static Regex Get(string pattern)
        => Cache.GetOrAdd(pattern, p => new Regex(p, RegexOptions.IgnoreCase));
}
