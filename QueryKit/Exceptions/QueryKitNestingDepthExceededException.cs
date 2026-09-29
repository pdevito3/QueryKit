namespace QueryKit.Exceptions;

public sealed class QueryKitNestingDepthExceededException : QueryKitException
{
    public QueryKitNestingDepthExceededException(int depth, int maxDepth)
        : base($"The filter has a nesting depth of {depth}, which exceeds the maximum allowed depth of {maxDepth}.")
    {
    }
}
