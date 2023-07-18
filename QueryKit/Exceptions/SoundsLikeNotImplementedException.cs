namespace QueryKit.Exceptions;

public sealed class SoundsLikeNotImplementedException : Exception
{
    public SoundsLikeNotImplementedException(string dbContextType) 
        : base($"The DbContext type {dbContextType} does not have a SoundsLike method.")
    {
    }
}
