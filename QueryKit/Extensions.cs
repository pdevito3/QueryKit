namespace QueryKit;

using System.Text;
using Sprache;

public static class Extensions
{
    public static string PreprocessInput<T>(this string input, IQueryKitPropertyConfiguration configuration)
    {
        var mappings = new QueryKitPropertyMappings();
        configuration.Configure(mappings);

        var identifiers = FilterParser.Identifier.Many().Parse(input).ToList();
        var transformedIdentifiers = identifiers.Select(id => GetTransformedIdentifier(id, mappings)).ToList();

        var processedInput = ReplaceIdentifiers(input, identifiers, transformedIdentifiers);
        return processedInput;
    }

    private static string GetTransformedIdentifier(string identifier, QueryKitPropertyMappings mappings)
    {
        var propertyInfo = mappings.GetPropertyInfo(identifier);
        return propertyInfo != null && propertyInfo.CanFilter && propertyInfo.QueryName != null
            ? propertyInfo.QueryName 
            : identifier;
    }

    private static string ReplaceIdentifiers(string input, List<string> identifiers, List<string> transformedIdentifiers)
    {
        var sb = new StringBuilder(input);

        for (var i = 0; i < identifiers.Count; i++)
        {
            sb.Replace(identifiers[i], transformedIdentifiers[i]);
        }

        return sb.ToString();
    }
}