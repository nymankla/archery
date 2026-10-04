using System.Collections.ObjectModel;
using Archery.Mobile.Core.Abstractions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace Archery.Mobile.Core.ViewModels;

public sealed partial class CompetitionsViewModel(
    IArcheryApiClient api,
    INavigationService navigation,
    IDialogService dialogs,
    ILogger<CompetitionsViewModel> logger) : BaseViewModel(logger)
{
    readonly List<Competition> _all = [];

    public ObservableCollection<Competition> Competitions { get; } = [];

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string CountSummary { get; set; } = string.Empty;

    public override Task OnAppearingAsync(CancellationToken ct = default)
    {
        Title = "Competitions";
        return LoadAsync(ct);
    }

    [RelayCommand]
    async Task LoadAsync(CancellationToken ct)
    {
        await RunAsync(async token =>
        {
            var competitions = await api.GetCompetitionsAsync(token) ?? [];
            _all.Clear();
            _all.AddRange(competitions);
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
    Task AddAsync() => navigation.GoToAsync(Routes.CompetitionEdit);

    [RelayCommand]
    Task OpenAsync(Competition? competition) => competition is null
        ? Task.CompletedTask
        : navigation.GoToAsync(Routes.CompetitionDetail,
            new Dictionary<string, object> { [Routes.CompetitionIdKey] = competition.Id });

    [RelayCommand]
    async Task DeleteAsync(Competition? competition, CancellationToken ct)
    {
        if (competition is null)
            return;

        if (!await dialogs.ConfirmAsync("Delete competition",
                $"Delete {competition.Name}? Its participants and results go with it.",
                "Delete", "Cancel"))
        {
            return;
        }

        var deleted = await RunAsync(
            token => api.DeleteCompetitionAsync(competition.Id, token).EnsureArcherySuccessAsync(token), ct);

        if (deleted)
        {
            _all.Remove(competition);
            ApplyFilter();
        }
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    void ApplyFilter()
    {
        IEnumerable<Competition> query = _all;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var term = SearchText.Trim();
            query = query.Where(c =>
                c.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
                || c.Location.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        // Newest first: the competition being looked at is nearly always a recent or upcoming one.
        Competitions.Clear();
        foreach (var competition in query.OrderByDescending(c => c.Date))
            Competitions.Add(competition);

        CountSummary = Competitions.Count == _all.Count
            ? $"{_all.Count} competitions"
            : $"{Competitions.Count} of {_all.Count} competitions";
    }
}
