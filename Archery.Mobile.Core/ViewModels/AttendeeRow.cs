using CommunityToolkit.Mvvm.ComponentModel;

namespace Archery.Mobile.Core.ViewModels;

/// <summary>
/// One tickable line on the attendance screen.
/// </summary>
/// <remarks>
/// Selection lives on these row objects rather than in a separate set of ids because the list
/// is filtered as the user searches. Rows are created once per load and the filtered collection
/// only ever holds references to them, so ticking someone, searching for someone else and
/// clearing the search leaves the first tick intact — which is exactly what goes wrong if the
/// view binds selection to the filtered collection instead.
/// </remarks>
public sealed partial class AttendeeRow : ObservableObject
{
    public AttendeeRow(Guid id, string name, string? subtitle, bool isActive)
    {
        Id = id;
        Name = name;
        Subtitle = subtitle;
        IsActive = isActive;
    }

    public Guid Id { get; }

    public string Name { get; }

    /// <summary>Email for a member, club affiliation for an external participant.</summary>
    public string? Subtitle { get; }

    public bool IsActive { get; }

    [ObservableProperty]
    public partial bool IsSelected { get; set; }
}
