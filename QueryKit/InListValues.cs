namespace QueryKit;

using System.Collections.ObjectModel;

// The items of an in-list (^^ and !^^) when filter values are constants. EF Core finds a compiled
// query with Equals and GetHashCode on each constant. A List<T> compares by reference, so each
// request compiled a new query. This type compares every item, so equal lists share one query.
internal sealed class InListValues<T> : ReadOnlyCollection<T>
{
    public InListValues(IList<T> items) : base(items)
    {
    }

    public override bool Equals(object? obj)
        => obj is InListValues<T> other && this.Select(Key).SequenceEqual(other.Select(Key));

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var item in this)
        {
            hash.Add(Key(item));
        }

        return hash.ToHashCode();
    }

    // DateTime.Equals ignores Kind and DateTimeOffset.Equals ignores Offset. A single value keeps both
    // in its constructor call, so a list item keeps them too. Npgsql writes a Utc and an Unspecified
    // DateTime differently.
    private static object? Key(T item) => item switch
    {
        DateTime dateTime => (dateTime.Ticks, dateTime.Kind),
        DateTimeOffset dateTimeOffset => (dateTimeOffset.Ticks, dateTimeOffset.Offset),
        _ => item
    };
}
