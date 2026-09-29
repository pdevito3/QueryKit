namespace QueryKit;

using System.Reflection;
using Configuration;

internal enum PropertyReferenceKind
{
    Member,
    DerivedProperty,
    CustomOperation,
    Unknown
}

/// <summary>
/// A property reference from a filter or sort string, resolved against the entity type and the configuration.
/// </summary>
internal sealed class PropertyReference
{
    private PropertyReference(PropertyReferenceKind kind, string text, string path, QueryKitPropertyInfo? mapping, string? unknownSegment)
    {
        Kind = kind;
        Text = text;
        Path = path;
        Mapping = mapping;
        UnknownSegment = unknownSegment;
    }

    public PropertyReferenceKind Kind { get; }

    /// <summary>The reference as the caller wrote it.</summary>
    public string Text { get; }

    /// <summary>For a member, the real member names joined with '.'. For other kinds, the reference text.</summary>
    public string Path { get; }

    /// <summary>The configuration of the member, the derived property, or the custom operation, if there is one.</summary>
    public QueryKitPropertyInfo? Mapping { get; }

    public bool CanFilter => Mapping?.CanFilter ?? true;

    public bool CanSort => Mapping?.CanSort ?? true;

    /// <summary>When the reference is not a member, the first path segment that did not resolve to a member.</summary>
    public string? UnknownSegment { get; }

    internal static PropertyReference Member(string text, string path, QueryKitPropertyInfo? mapping)
        => new(PropertyReferenceKind.Member, text, path, mapping, null);

    internal static PropertyReference NotMember(PropertyReferenceKind kind, string text, QueryKitPropertyInfo? mapping, string unknownSegment)
        => new(kind, text, text, mapping, unknownSegment);
}

/// <summary>
/// Resolves every property reference in a filter or sort string the same way.
/// </summary>
internal static class PropertyResolver
{
    internal static PropertyReference Resolve(Type rootType, string reference, IQueryKitConfiguration? config)
    {
        config?.ValidatePropertyDepth(reference);

        var memberPath = ResolveMemberPath(rootType, reference, out var unknownSegment);
        if (memberPath != null)
        {
            return PropertyReference.Member(reference, memberPath, config?.PropertyMappings?.GetPropertyInfo(memberPath));
        }

        var customOperationInfo = config?.PropertyMappings?.GetCustomOperationInfoByQueryName(reference);
        if (customOperationInfo?.CustomOperation != null)
        {
            return PropertyReference.NotMember(PropertyReferenceKind.CustomOperation, reference, customOperationInfo, unknownSegment!);
        }

        var derivedPropertyInfo = config?.PropertyMappings?.GetDerivedPropertyInfoByQueryName(reference);
        if (derivedPropertyInfo?.DerivedExpression != null)
        {
            return PropertyReference.NotMember(PropertyReferenceKind.DerivedProperty, reference, derivedPropertyInfo, unknownSegment!);
        }

        return PropertyReference.NotMember(PropertyReferenceKind.Unknown, reference, null, unknownSegment!);
    }

    // Matches each segment to a public member, ignoring case. A segment after a collection resolves on the element type.
    private static string? ResolveMemberPath(Type rootType, string path, out string? unknownSegment)
    {
        var memberNames = new List<string>();
        var currentType = rootType;

        foreach (var segment in path.Split('.'))
        {
            while (IsCollection(currentType))
            {
                currentType = currentType.GetGenericArguments()[0];
            }

            var member = (MemberInfo?)currentType.GetProperty(segment, MemberFlags)
                         ?? currentType.GetField(segment, MemberFlags);
            if (member == null)
            {
                unknownSegment = segment;
                return null;
            }

            memberNames.Add(member.Name);
            currentType = member is PropertyInfo property ? property.PropertyType : ((FieldInfo)member).FieldType;
        }

        unknownSegment = null;
        return string.Join(".", memberNames);
    }

    private const BindingFlags MemberFlags = BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance;

    private static bool IsCollection(Type type)
        => type != typeof(string) && type.IsGenericType &&
           (type.GetGenericTypeDefinition() == typeof(IEnumerable<>) ||
            type.GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEnumerable<>)));
}
