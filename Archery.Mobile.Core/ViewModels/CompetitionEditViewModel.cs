using Archery.Mobile.Core.Abstractions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace Archery.Mobile.Core.ViewModels;

public sealed partial class CompetitionEditViewModel(
    IArcheryApiClient api,
    INavigationService navigation,
    ILogger<CompetitionEditViewModel> logger) : BaseViewModel(logger)
{
    Guid? _competitionId;

    public IReadOnlyList<CompetitionType> Types { get; } = Enum.GetValues<CompetitionType>();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    public partial string Location { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    public partial string RoundType { get; set; } = string.Empty;

    [ObservableProperty]
    public partial DateTime Date { get; set; } = DateTime.Today;

    [ObservableProperty]
    public partial CompetitionType Type { get; set; } = CompetitionType.Indoor;

    [ObservableProperty]
    public partial string? Description { get; set; }

    public bool IsNew => _competitionId is null;

    public bool CanSave =>
        !string.IsNullOrWhiteSpace(Name)
        && !string.IsNullOrWhiteSpace(Location)
        && !string.IsNullOrWhiteSpace(RoundType);

    public async Task InitialiseAsync(Guid? competitionId, CancellationToken ct = default)
    {
        _competitionId = competitionId;
        Title = competitionId is null ? "Add competition" : "Edit competition";

        if (competitionId is null)
            return;

        await RunAsync(async token =>
        {
            var competition = await api.GetCompetitionAsync(competitionId.Value, token)
                ?? throw new InvalidOperationException("Competition not found.");

            Name = competition.Name;
            Location = competition.Location;
            RoundType = competition.RoundType;
            Date = competition.Date.ToDateTime(TimeOnly.MinValue);
            Type = competition.Type;
            Description = competition.Description;
        }, ct);
    }

    [RelayCommand]
    async Task SaveAsync(CancellationToken ct)
    {
        if (!CanSave)
            return;

        var competition = new Competition
        {
            Id = _competitionId ?? Guid.Empty,
            Name = Name.Trim(),
            Location = Location.Trim(),
            RoundType = RoundType.Trim(),
            Date = DateOnly.FromDateTime(Date),
            Type = Type,
            Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim()
        };

        var saved = await RunAsync(token => _competitionId is { } id
            ? api.UpdateCompetitionAsync(id, competition, token).EnsureArcherySuccessAsync(token)
            : api.CreateCompetitionAsync(competition, token).EnsureArcherySuccessAsync(token), ct);

        if (saved)
            await navigation.GoBackAsync();
    }

    [RelayCommand]
    Task CancelAsync() => navigation.GoBackAsync();
}
