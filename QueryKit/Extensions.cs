namespace QueryKit;

internal static class Extensions
{
    public static bool IsNullable(this Type type)
    {
        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
    }

    // A collection is a type that implements IEnumerable<T>, for example List<T>, IReadOnlyList<T>, HashSet<T>, or T[].
    // A string is not a collection.
    public static bool IsCollection(this Type type)
    {
        return type.GetCollectionElementType() != null;
    }

    // The T of IEnumerable<T>. A type such as Dictionary<TKey, TValue> or a class that inherits from List<T> has other generic arguments, so read the interface.
    public static Type? GetCollectionElementType(this Type type)
    {
        if (type == typeof(string))
        {
            return null;
        }

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
        {
            return type.GetGenericArguments()[0];
        }

        return type.GetInterfaces()
            .FirstOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            ?.GetGenericArguments()[0];
    }
}
