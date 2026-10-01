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

    // Matches each segment to a member, ignoring case, in the order of Expression.PropertyOrField like v1.14.2:
    // a public property, a public field, a non-public property, and then a non-public field. An indexer does not match.
    // A segment after a collection resolves on the element type.
    // After a collection, only public properties match: the first segment in the exact case, a later segment in any case.
    // A segment after a collection that does not match throws NullReferenceException.
    private static string? ResolveMemberPath(Type rootType, string path, out string? unknownSegment)
    {
        var memberNames = new List<string>();
        var currentType = rootType;
        var afterCollection = false;

        foreach (var segment in path.Split('.'))
        {
            var firstAfterCollection = !afterCollection && memberNames.Count > 0 && IsCollection(currentType);
            while (IsCollection(currentType))
            {
                currentType = currentType.GetGenericArguments()[0];
            }

            MemberInfo? member;
            if (firstAfterCollection || afterCollection)
            {
                member = (firstAfterCollection ? currentType.GetProperty(segment) : currentType.GetProperty(segment, PublicMemberFlags))
                         ?? throw new NullReferenceException();
                afterCollection = true;
            }
            else
            {
                member = (MemberInfo?)currentType.GetProperty(segment, PublicMemberFlags)
                         ?? (MemberInfo?)currentType.GetField(segment, PublicMemberFlags)
                         ?? (MemberInfo?)currentType.GetProperty(segment, NonPublicMemberFlags)
                         ?? currentType.GetField(segment, NonPublicMemberFlags);
            }

            if (member == null || member is PropertyInfo indexer && indexer.GetIndexParameters().Length > 0)
            {
                // v1.14.2 named an unknown member by the name of the public property with that name, if there was one.
                unknownSegment = currentType.GetProperty(segment, PublicMemberFlags)?.Name ?? segment;
                return null;
            }

            memberNames.Add(member.Name);
            currentType = member is PropertyInfo property ? property.PropertyType : ((FieldInfo)member).FieldType;
        }

        unknownSegment = null;
        return string.Join(".", memberNames);
    }

    private const BindingFlags PublicMemberFlags = BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance;
    private const BindingFlags NonPublicMemberFlags = BindingFlags.IgnoreCase | BindingFlags.NonPublic | BindingFlags.Instance;

    private static bool IsCollection(Type type)
        => type != typeof(string) && type.IsGenericType &&
           (type.GetGenericTypeDefinition() == typeof(IEnumerable<>) ||
            type.GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEnumerable<>)));
}
