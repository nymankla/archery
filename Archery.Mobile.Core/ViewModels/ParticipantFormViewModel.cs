using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

namespace Archery.Mobile.Core.ViewModels;

/// <summary>A member or guest offered for selection, reduced to what the picker needs.</summary>
public sealed record ParticipantOption(Guid Id, string Name, string? Detail);

/// <summary>
/// The member-or-guest picker plus bow class, age class and gender, shared by registration and
/// result entry.
/// </summary>
/// <remarks>
/// The row being edited belongs to exactly one of a member or an external participant (the
/// SingleParticipant check constraints). Rather than letting both ids be set and nulling one
/// before posting, the form offers a single choice through <see cref="Kind"/>, so an invalid
/// combination cannot be expressed. Use <see cref="SelectedMemberId"/> and
/// <see cref="SelectedExternalParticipantId"/> when building the request.
/// </remarks>
public abstract partial class ParticipantFormViewModel(ILogger logger) : BaseViewModel(logger)
{
    readonly List<ParticipantOption> _allMembers = [];
    readonly List<ParticipantOption> _allExternals = [];

    public ObservableCollection<ParticipantOption> Options { get; } = [];

    public IReadOnlyList<BowClass> BowClasses { get; } = Enum.GetValues<BowClass>();

    public IReadOnlyList<AgeClass> AgeClasses { get; } = Enum.GetValues<AgeClass>();

    public IReadOnlyList<Gender> Genders { get; } = Enum.GetValues<Gender>();

    [ObservableProperty]
    public partial ParticipantKind Kind { get; set; } = ParticipantKind.Member;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    public partial ParticipantOption? Selected { get; set; }

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial BowClass BowClass { get; set; } = BowClass.Recurve;

    [ObservableProperty]
    public partial AgeClass AgeClass { get; set; } = AgeClass.Senior;

    [ObservableProperty]
    public partial Gender Gender { get; set; } = Gender.Unknown;

    public bool IsMember => Kind == ParticipantKind.Member;

    public abstract bool CanSave { get; }

    /// <summary>Set only when a member is picked: exactly one of the two ids is ever non-null.</summary>
    protected Guid? SelectedMemberId => IsMember ? Selected?.Id : null;

    protected Guid? SelectedExternalParticipantId => IsMember ? null : Selected?.Id;

    /// <summary>False once the participant is fixed, e.g. when editing an existing result.</summary>
    protected virtual bool AllowsParticipantChange => true;

    /// <summary>Replaces both pick lists; callers filter first (inactive, already registered, ...).</summary>
    protected void SetOptions(IEnumerable<Member> members, IEnumerable<ExternalParticipant> externals)
    {
        _allMembers.Clear();
        _allMembers.AddRange(members
            .OrderBy(m => m.FirstName).ThenBy(m => m.LastName)
            .Select(m => new ParticipantOption(m.Id, m.FullName, m.Email)));

        _allExternals.Clear();
        _allExternals.AddRange(externals
            .OrderBy(e => e.FirstName).ThenBy(e => e.LastName)
            .Select(e => new ParticipantOption(e.Id, e.FullName, e.ClubAffiliation)));

        ApplyFilter();
    }

    /// <summary>Shows <paramref name="memberId"/> or <paramref name="externalId"/> as the current pick.</summary>
    protected void SelectParticipant(Guid? memberId, Guid? externalId)
    {
        Kind = memberId.HasValue ? ParticipantKind.Member : ParticipantKind.External;

        var id = memberId ?? externalId;
        var source = memberId.HasValue ? _allMembers : _allExternals;
        Selected = source.FirstOrDefault(o => o.Id == id);
    }

    partial void OnKindChanged(ParticipantKind value)
    {
        if (!AllowsParticipantChange)
            return;

        // Switching between member and guest must drop the previous pick, or Save would post
        // an id belonging to the wrong list.
        Selected = null;
        OnPropertyChanged(nameof(IsMember));
        ApplyFilter();
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    void ApplyFilter()
    {
        IEnumerable<ParticipantOption> source = IsMember ? _allMembers : _allExternals;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var term = SearchText.Trim();
            source = source.Where(o =>
                o.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
                || (o.Detail?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        Options.Clear();
        foreach (var option in source)
            Options.Add(option);
    }
}
