namespace QueryKit;

using System.Collections.Concurrent;
using System.Globalization;
using System.Text.RegularExpressions;

// Keeps one Regex for each alias pattern and culture, so a parse does not build a new Regex for each alias.
// The patterns come only from the configuration (query names and operator aliases), never from
// the filter text, so the number of entries stays small.
// RegexOptions.IgnoreCase uses the current culture when the Regex is built, so the key includes the culture.
// Like v1.14.2, which built a new Regex for each parse, each culture keeps its own case rules.
internal static class AliasRegexCache
{
    private static readonly ConcurrentDictionary<(string Pattern, string Culture), Regex> Cache = new();

    public static Regex Get(string pattern)
        => Cache.GetOrAdd((pattern, CultureInfo.CurrentCulture.Name), key => new Regex(key.Pattern, RegexOptions.IgnoreCase));
}
