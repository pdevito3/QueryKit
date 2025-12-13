namespace QueryKit.Exceptions;

public sealed class QueryKitPropertyDepthExceededException : QueryKitException
{
    public QueryKitPropertyDepthExceededException(string propertyPath, int depth, int maxDepth)
        : base($"The property path '{propertyPath}' has a depth of {depth}, which exceeds the maximum allowed depth of {maxDepth}.")
    {
    }
}
