namespace QueryKit.Exceptions;

using Sprache;

public sealed class ParsingException : QueryKitException
{
    public ParsingException(Exception exception)
        : base(BuildMessage(exception), exception)
    {
        if (exception is ParseException parseException)
        {
            Line = parseException.Position.Line;
            Column = parseException.Position.Column;
        }
    }

    // A filter value that does not convert to the type of its property. The client sent the value and the property,
    // so the message can name them.
    public ParsingException(string value, string propertyName, Type targetType, Exception exception)
        : base($"The value '{value}' is not a valid {targetType.Name} for the filter property '{propertyName}'.", exception)
    {
        Value = value;
        PropertyName = propertyName;
        TargetType = targetType;
    }

    /// <summary>The filter property of a value that does not convert. Null for a syntax failure.</summary>
    public string? PropertyName { get; }

    /// <summary>The type that the value did not convert to. Null for a syntax failure.</summary>
    public Type? TargetType { get; }

    /// <summary>
    /// The value that did not convert, as the client wrote it. Null for a syntax failure.
    /// This is user input: do not write it to a log or a trace without the same care as the rest of the request.
    /// </summary>
    public string? Value { get; }

    /// <summary>The line of a syntax failure, from 1. Null when the failure is not a syntax failure.</summary>
    public int? Line { get; }

    /// <summary>The column of a syntax failure, from 1. Null when the failure is not a syntax failure.</summary>
    public int? Column { get; }

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
