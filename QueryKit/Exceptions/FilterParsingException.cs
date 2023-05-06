namespace QueryKit.Exceptions;

public sealed class ParsingException : Exception
{
    public ParsingException(Exception exception) 
        : base($"There was a parsing failure, likely due to an invalid comparison or logical operator. You may also be missing double quotes surrounding a string or guid.", exception)
    {
    }
}
