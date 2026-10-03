using System.Collections.ObjectModel;
using Archery.Mobile.Core.Abstractions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace Archery.Mobile.Core.ViewModels;

/// <summary>
/// Visiting archers from other clubs, who can be entered into competitions and training
/// sessions without being club members.
/// </summary>
public sealed partial class ExternalParticipantsViewModel(
    IArcheryApiClient api,
    INavigationService navigation,
    IDialogService dialogs,
    ILogger<ExternalParticipantsViewModel> logger) : BaseViewModel(logger)
{
    readonly List<ExternalParticipant> _all = [];

    public ObservableCollection<ExternalParticipant> Participants { get; } = [];

    /// <summary>Clubs actually present in the data, with a null entry meaning "all".</summary>
    public ObservableCollection<string?> Clubs { get; } = [];

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? ClubFilter { get; set; }

    [ObservableProperty]
    public partial string CountSummary { get; set; } = string.Empty;

    public override Task OnAppearingAsync(CancellationToken ct = default)
    {
        Title = "Guests";
        return LoadAsync(ct);
    }

    [RelayCommand]
    async Task LoadAsync(CancellationToken ct)
    {
        await RunAsync(async token =>
        {
            var participants = await api.GetExternalParticipantsAsync(token) ?? [];
            _all.Clear();
            _all.AddRange(participants);

            // The filter list is derived from the data rather than fixed: clubs come and go,
            // and there is no club entity to enumerate.
            var previous = ClubFilter;
            Clubs.Clear();
            Clubs.Add(null);
            foreach (var club in _all
                         .Select(p => p.ClubAffiliation)
                         .Where(c => !string.IsNullOrWhiteSpace(c))
                         .Distinct(StringComparer.OrdinalIgnoreCase)
                         .OrderBy(c => c, StringComparer.CurrentCultureIgnoreCase))
            {
                Clubs.Add(club);
            }

            // Keep the chosen club if it still exists, otherwise fall back to "all" rather
            // than leaving the picker pointing at something that is no longer there.
            ClubFilter = previous is not null && Clubs.Contains(previous) ? previous : null;

            ApplyFilter();
        }, ct);
    }

    [RelayCommand]
    async Task RefreshAsync(CancellationToken ct)
    {
        IsRefreshing = true;
        await LoadAsync(ct);
    }

    [RelayCommand]
    Task AddAsync() => navigation.GoToAsync(Routes.ExternalParticipantEdit);

    [RelayCommand]
    Task OpenAsync(ExternalParticipant? participant) => participant is null
        ? Task.CompletedTask
        : navigation.GoToAsync(Routes.ExternalParticipantEdit,
            new Dictionary<string, object> { [Routes.ExternalParticipantIdKey] = participant.Id });

    [RelayCommand]
    async Task DeleteAsync(ExternalParticipant? participant, CancellationToken ct)
    {
        if (participant is null)
            return;

        if (!await dialogs.ConfirmAsync("Delete guest",
                $"Delete {participant.FullName}?", "Delete", "Cancel"))
        {
            return;
        }

        var deleted = await RunAsync(
            token => api.DeleteExternalParticipantAsync(participant.Id, token).EnsureArcherySuccessAsync(token), ct);

        if (deleted)
        {
            _all.Remove(participant);
            ApplyFilter();
        }
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    partial void OnClubFilterChanged(string? value) => ApplyFilter();

    void ApplyFilter()
    {
        IEnumerable<ExternalParticipant> query = _all;

        if (ClubFilter is not null)
            query = query.Where(p => string.Equals(p.ClubAffiliation, ClubFilter, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var term = SearchText.Trim();
            query = query.Where(p =>
                p.FullName.Contains(term, StringComparison.OrdinalIgnoreCase)
                || (p.Email?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
                || (p.ClubAffiliation?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        Participants.Clear();
        foreach (var participant in query.OrderBy(p => p.FirstName).ThenBy(p => p.LastName))
            Participants.Add(participant);

        CountSummary = Participants.Count == _all.Count
            ? $"{_all.Count} guests"
            : $"{Participants.Count} of {_all.Count} guests";
    }
}
