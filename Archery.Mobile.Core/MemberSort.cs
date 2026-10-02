namespace Archery.Mobile.Core;

/// <summary>
/// Sort orders offered on the member list. A phone has no column headers to click, so the web
/// app's SortState/SortableHeader pairing does not carry over; a small enum bound to a menu is
/// the equivalent.
/// </summary>
public enum MemberSort
{
    Name,
    JoinDateNewest,
    JoinDateOldest,
    BowClass
}

public static class MemberSortExtensions
{
    public static string ToDisplayString(this MemberSort sort) => sort switch
    {
        MemberSort.Name => "Name",
        MemberSort.JoinDateNewest => "Joined (newest first)",
        MemberSort.JoinDateOldest => "Joined (oldest first)",
        MemberSort.BowClass => "Bow class",
        _ => sort.ToString()
    };
}
