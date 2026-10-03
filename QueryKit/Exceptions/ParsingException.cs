namespace QueryKit.Exceptions;

using Sprache;

public sealed class ParsingException : QueryKitException
{
    public ParsingException(Exception exception)
        : base(BuildMessage(exception), exception)
    {
    }

    // A filter value that does not convert to the type of its property. The client sent the value and the property,
    // so the message can name them.
    public ParsingException(string value, string propertyName, Type targetType, Exception exception)
        : base($"The value '{value}' is not a valid {targetType.Name} for the filter property '{propertyName}'.", exception)
    {
    }

    private static string BuildMessage(Exception exception)
    {
        const string baseMessage = "There was a parsing failure, likely due to an invalid comparison or logical operator. You may also be missing double quotes surrounding a string or guid.";

        // Sprache.Position holds only a line and a column, so it is safe to expose to a client.
        // The full exception.Message can name internal parser rules or .NET types, so it stays server-side on InnerException.
        if (exception is ParseException parseException)
        {
            return $"{baseMessage} Failed at {parseException.Position}.";
        }

        return baseMessage;
    }
}
