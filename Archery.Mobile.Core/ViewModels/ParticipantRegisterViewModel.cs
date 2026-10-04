using System.Collections.ObjectModel;
using Archery.Mobile.Core.Abstractions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace Archery.Mobile.Core.ViewModels;

/// <summary>A member or guest offered for selection, reduced to what the picker needs.</summary>
public sealed record ParticipantOption(Guid Id, string Name, string? Detail);

/// <summary>
/// Registers an archer for a competition.
/// </summary>
/// <remarks>
/// The entry belongs to exactly one of a member or an external participant — enforced by the
/// CK_CompetitionParticipant_SingleParticipant check constraint. Rather than letting both ids
/// be set and nulling one before posting, the form offers a single choice, so an invalid
/// combination cannot be expressed. Already-registered archers are filtered out of the list,
/// which removes the most common cause of a rejected save.
/// </remarks>
public sealed partial class ParticipantRegisterViewModel(
    IArcheryApiClient api,
    INavigationService navigation,
    ILogger<ParticipantRegisterViewModel> logger) : BaseViewModel(logger)
{
    Guid _competitionId;
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

    public bool CanSave => Selected is not null;

    public async Task InitialiseAsync(Guid competitionId, CancellationToken ct = default)
    {
        _competitionId = competitionId;
        Title = "Register participant";

        await RunAsync(async token =>
        {
            var members = await api.GetMembersAsync(token) ?? [];
            var externals = await api.GetExternalParticipantsAsync(token) ?? [];
            var registered = await api.GetParticipantsByCompetitionAsync(_competitionId, token) ?? [];

            var takenMembers = registered.Where(p => p.MemberId.HasValue).Select(p => p.MemberId!.Value).ToHashSet();
            var takenExternals = registered.Where(p => p.ExternalParticipantId.HasValue)
                .Select(p => p.ExternalParticipantId!.Value).ToHashSet();

            _allMembers.Clear();
            _allMembers.AddRange(members
                .Where(m => m.IsActive && !takenMembers.Contains(m.Id))
                .OrderBy(m => m.FirstName).ThenBy(m => m.LastName)
                .Select(m => new ParticipantOption(m.Id, m.FullName, m.Email)));

            _allExternals.Clear();
            _allExternals.AddRange(externals
                .Where(e => !takenExternals.Contains(e.Id))
                .OrderBy(e => e.FirstName).ThenBy(e => e.LastName)
                .Select(e => new ParticipantOption(e.Id, e.FullName, e.ClubAffiliation)));

            ApplyFilter();
        }, ct);
    }

    [RelayCommand]
    async Task SaveAsync(CancellationToken ct)
    {
        if (Selected is null)
            return;

        var participant = new CompetitionParticipant
        {
            CompetitionId = _competitionId,
            // Exactly one of these is ever set, which is what the check constraint requires.
            MemberId = IsMember ? Selected.Id : null,
            ExternalParticipantId = IsMember ? null : Selected.Id,
            BowClass = BowClass,
            AgeClass = AgeClass,
            Gender = Gender
        };

        var saved = await RunAsync(token => api
            .RegisterParticipantAsync(participant, token).EnsureArcherySuccessAsync(token), ct);

        if (saved)
            await navigation.GoBackAsync();
    }

    [RelayCommand]
    Task CancelAsync() => navigation.GoBackAsync();

    partial void OnKindChanged(ParticipantKind value)
    {
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
