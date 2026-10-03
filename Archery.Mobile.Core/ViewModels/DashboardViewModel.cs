using System.Collections.ObjectModel;
using Archery.Mobile.Core.Abstractions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace Archery.Mobile.Core.ViewModels;

/// <summary>
/// The landing screen: one call, read-only, no forms.
/// </summary>
/// <remarks>
/// Deliberately not a chart. These are a handful of headline numbers, which is a row of stat
/// tiles; the collection rate is a single ratio against a limit, which is a meter rather than
/// a two-slice pie. The top scorers and recent competitions are short tables.
/// </remarks>
public sealed partial class DashboardViewModel(
    IArcheryApiClient api,
    INavigationService navigation,
    ILogger<DashboardViewModel> logger) : BaseViewModel(logger)
{
    public ObservableCollection<DashboardTopScorer> TopScorers { get; } = [];

    public ObservableCollection<DashboardRecentCompetition> RecentCompetitions { get; } = [];

    [ObservableProperty]
    public partial DashboardData? Data { get; set; }

    [ObservableProperty]
    public partial bool HasData { get; set; }

    [ObservableProperty]
    public partial bool HasNextCompetition { get; set; }

    [ObservableProperty]
    public partial string NextCompetitionSummary { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool HasTopScorers { get; set; }

    [ObservableProperty]
    public partial bool HasRecentCompetitions { get; set; }

    /// <summary>0..1 for the meter; the API reports a whole percentage.</summary>
    [ObservableProperty]
    public partial double CollectionRate { get; set; }

    public override Task OnAppearingAsync(CancellationToken ct = default)
    {
        Title = "Archery Club";
        return LoadAsync(ct);
    }

    [RelayCommand]
    async Task LoadAsync(CancellationToken ct)
    {
        await RunAsync(async token =>
        {
            Data = await api.GetDashboardAsync(token);
            HasData = Data is not null;

            TopScorers.Clear();
            RecentCompetitions.Clear();

            if (Data is null)
            {
                HasTopScorers = false;
                HasRecentCompetitions = false;
                HasNextCompetition = false;
                CollectionRate = 0;
                return;
            }

            foreach (var scorer in Data.TopScorers)
                TopScorers.Add(scorer);

            foreach (var competition in Data.RecentCompetitions)
                RecentCompetitions.Add(competition);

            HasTopScorers = TopScorers.Count > 0;
            HasRecentCompetitions = RecentCompetitions.Count > 0;

            // Clamped: the meter must not overflow its track if the server ever reports >100.
            CollectionRate = Math.Clamp(Data.Fees.CollectionRatePct / 100d, 0d, 1d);

            var next = Data.Competitions.NextCompetition;
            HasNextCompetition = next is not null;
            NextCompetitionSummary = next is null
                ? string.Empty
                : $"{next.Name} · {next.Date:yyyy-MM-dd} · {next.Location}";
        }, ct);
    }

    [RelayCommand]
    async Task RefreshAsync(CancellationToken ct)
    {
        IsRefreshing = true;
        await LoadAsync(ct);
    }

    [RelayCommand]
    Task OpenMembersAsync() => navigation.GoToRootAsync(Routes.Members);

    [RelayCommand]
    Task OpenCompetitionsAsync() => navigation.GoToRootAsync(Routes.Competitions);

    [RelayCommand]
    Task OpenTrainingAsync() => navigation.GoToRootAsync(Routes.TrainingAttendance);
}
