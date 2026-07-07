namespace aspire_sample.Web;

/// <summary>
/// Tracks the active sort column and direction for a table and applies it to a sequence.
/// Register each sortable column with <see cref="Add"/>; the key is the string a
/// <c>SortableHeader</c> passes back on click.
/// </summary>
public sealed class SortState<T>
{
    readonly Dictionary<string, Func<T, object?>> _columns = new(StringComparer.OrdinalIgnoreCase);

    public string? Column { get; private set; }
    public bool Ascending { get; private set; } = true;

    /// <summary>Registers a sortable column and its value selector.</summary>
    public SortState<T> Add(string key, Func<T, object?> selector)
    {
        _columns[key] = selector;
        return this;
    }

    /// <summary>Sets the initial sort applied before the user clicks anything.</summary>
    public SortState<T> Default(string key, bool ascending = true)
    {
        Column = key;
        Ascending = ascending;
        return this;
    }

    /// <summary>Clicking the active column flips direction; clicking another selects it ascending.</summary>
    public void Toggle(string key)
    {
        if (string.Equals(Column, key, StringComparison.OrdinalIgnoreCase))
        {
            Ascending = !Ascending;
        }
        else
        {
            Column = key;
            Ascending = true;
        }
    }

    /// <summary>Returns the source ordered by the active column, or unchanged if none is set.</summary>
    public List<T> Apply(IEnumerable<T> source)
    {
        if (Column is null || !_columns.TryGetValue(Column, out var selector))
            return source.ToList();

        var comparer = Comparer<object?>.Create(Compare);
        return (Ascending
            ? source.OrderBy(selector, comparer)
            : source.OrderByDescending(selector, comparer)).ToList();
    }

    static int Compare(object? a, object? b)
    {
        if (a is null) return b is null ? 0 : -1;
        if (b is null) return 1;
        if (a is string sa && b is string sb)
            return string.Compare(sa, sb, StringComparison.OrdinalIgnoreCase);
        return Comparer<object>.Default.Compare(a, b);
    }
}
