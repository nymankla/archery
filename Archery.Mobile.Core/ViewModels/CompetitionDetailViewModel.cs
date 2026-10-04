using System.Collections.ObjectModel;
using Archery.Mobile.Core.Abstractions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace Archery.Mobile.Core.ViewModels;

/// <summary>Which half of the detail screen is showing.</summary>
public enum CompetitionTab
{
    Participants,
    Results
}

public sealed partial class CompetitionDetailViewModel(
    IArcheryApiClient api,
    INavigationService navigation,
    IDialogService dialogs,
    ILogger<CompetitionDetailViewModel> logger) : BaseViewModel(logger)
{
    Guid _competitionId;
    readonly List<CompetitionParticipant> _allParticipants = [];
    readonly List<CompetitionResult> _allResults = [];

    public ObservableCollection<CompetitionParticipant> Participants { get; } = [];

    public ObservableCollection<CompetitionResult> Results { get; } = [];

    /// <summary>Registered archers who have no result yet — the queue to work through.</summary>
    public ObservableCollection<CompetitionParticipant> AwaitingResult { get; } = [];

    public IReadOnlyList<BowClass?> BowClassOptions { get; } =
        [null, .. Enum.GetValues<BowClass>().Cast<BowClass?>()];

    [ObservableProperty]
    public partial Competition? Competition { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowParticipants))]
    [NotifyPropertyChangedFor(nameof(ShowResults))]
    public partial CompetitionTab Tab { get; set; } = CompetitionTab.Participants;

    [ObservableProperty]
    public partial BowClass? BowClassFilter { get; set; }

    [ObservableProperty]
    public partial string ResultSearch { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ParticipantSummary { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ResultSummary { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool HasAwaitingResult { get; set; }

    public bool ShowParticipants => Tab == CompetitionTab.Participants;

    public bool ShowResults => Tab == CompetitionTab.Results;

    public async Task InitialiseAsync(Guid competitionId, CancellationToken ct = default)
    {
        _competitionId = competitionId;
        await LoadAsync(ct);
    }

    [RelayCommand]
    async Task LoadAsync(CancellationToken ct)
    {
        await RunAsync(async token =>
        {
            Competition = await api.GetCompetitionAsync(_competitionId, token);
            Title = Competition?.Name ?? "Competition";

            var participants = await api.GetParticipantsByCompetitionAsync(_competitionId, token) ?? [];
            var results = await api.GetResultsByCompetitionAsync(_competitionId, token) ?? [];

            _allParticipants.Clear();
            _allParticipants.AddRange(participants);

            _allResults.Clear();
            _allResults.AddRange(results);

            ApplyFilters();
        }, ct);
    }

    [RelayCommand]
    async Task RefreshAsync(CancellationToken ct)
    {
        IsRefreshing = true;
        await LoadAsync(ct);
    }

    [RelayCommand]
    void ShowParticipantsTab() => Tab = CompetitionTab.Participants;

    [RelayCommand]
    void ShowResultsTab() => Tab = CompetitionTab.Results;

    [RelayCommand]
    Task EditAsync() => navigation.GoToAsync(Routes.CompetitionEdit,
        new Dictionary<string, object> { [Routes.CompetitionIdKey] = _competitionId });

    [RelayCommand]
    Task RegisterParticipantAsync() => navigation.GoToAsync(Routes.ParticipantRegister,
        new Dictionary<string, object> { [Routes.CompetitionIdKey] = _competitionId });

    [RelayCommand]
    Task AddResultAsync() => navigation.GoToAsync(Routes.ResultEdit,
        new Dictionary<string, object> { [Routes.CompetitionIdKey] = _competitionId });

    /// <summary>
    /// Opens the result form pre-filled from a registration, so bow class, age class and gender
    /// come from what the archer actually entered under rather than being retyped.
    /// </summary>
    [RelayCommand]
    Task AddResultForAsync(CompetitionParticipant? participant) => participant is null
        ? Task.CompletedTask
        : navigation.GoToAsync(Routes.ResultEdit, new Dictionary<string, object>
        {
            [Routes.CompetitionIdKey] = _competitionId,
            [Routes.FromParticipantIdKey] = participant.Id
        });

    [RelayCommand]
    Task EditResultAsync(CompetitionResult? result) => result is null
        ? Task.CompletedTask
        : navigation.GoToAsync(Routes.ResultEdit, new Dictionary<string, object>
        {
            [Routes.CompetitionIdKey] = _competitionId,
            [Routes.ResultIdKey] = result.Id
        });

    [RelayCommand]
    async Task RemoveParticipantAsync(CompetitionParticipant? participant, CancellationToken ct)
    {
        if (participant is null)
            return;

        if (!await dialogs.ConfirmAsync("Remove participant",
                $"Remove {participant.ParticipantName} from this competition?", "Remove", "Cancel"))
        {
            return;
        }

        var removed = await RunAsync(
            token => api.RemoveParticipantAsync(participant.Id, token).EnsureArcherySuccessAsync(token), ct);

        if (removed)
        {
            _allParticipants.Remove(participant);
            ApplyFilters();
        }
    }

    [RelayCommand]
    async Task DeleteResultAsync(CompetitionResult? result, CancellationToken ct)
    {
        if (result is null)
            return;

        if (!await dialogs.ConfirmAsync("Delete result",
                $"Delete the result for {result.ParticipantName}?", "Delete", "Cancel"))
        {
            return;
        }

        var deleted = await RunAsync(
            token => api.DeleteResultAsync(result.Id, token).EnsureArcherySuccessAsync(token), ct);

        if (deleted)
        {
            _allResults.Remove(result);
            ApplyFilters();
        }
    }

    partial void OnBowClassFilterChanged(BowClass? value) => ApplyFilters();

    partial void OnResultSearchChanged(string value) => ApplyFilters();

    void ApplyFilters()
    {
        IEnumerable<CompetitionParticipant> participants = _allParticipants;
        IEnumerable<CompetitionResult> results = _allResults;

        if (BowClassFilter is { } bowClass)
        {
            participants = participants.Where(p => p.BowClass == bowClass);
            results = results.Where(r => r.BowClass == bowClass);
        }

        if (!string.IsNullOrWhiteSpace(ResultSearch))
        {
            var term = ResultSearch.Trim();
            results = results.Where(r => r.ParticipantName.Contains(term, StringComparison.OrdinalIgnoreCase));
            participants = participants.Where(p => p.ParticipantName.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        Participants.Clear();
        foreach (var participant in participants.OrderBy(p => p.ParticipantName))
            Participants.Add(participant);

        // Highest score first, which is the order a scoreboard is read in. Placement is only
        // set once someone assigns it, so it cannot be the primary sort.
        Results.Clear();
        foreach (var result in results.OrderByDescending(r => r.TotalScore).ThenByDescending(r => r.XCount))
            Results.Add(result);

        var scored = _allResults
            .Select(r => r.MemberId ?? r.ExternalParticipantId)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .ToHashSet();

        AwaitingResult.Clear();
        foreach (var participant in _allParticipants
                     .Where(p => !scored.Contains(p.MemberId ?? p.ExternalParticipantId ?? Guid.Empty))
                     .OrderBy(p => p.ParticipantName))
        {
            AwaitingResult.Add(participant);
        }

        HasAwaitingResult = AwaitingResult.Count > 0;

        ParticipantSummary = _allParticipants.Count == 0
            ? "No participants registered yet."
            : $"{Participants.Count} of {_allParticipants.Count} registered";

        ResultSummary = _allResults.Count == 0
            ? "No results recorded yet."
            : $"{Results.Count} of {_allResults.Count} results";
    }
}
