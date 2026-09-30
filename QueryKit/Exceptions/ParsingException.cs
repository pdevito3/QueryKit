namespace QueryKit.Exceptions;

using Sprache;

public sealed class ParsingException : QueryKitException
{
    public ParsingException(Exception exception)
        : base(BuildMessage(exception), exception)
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
