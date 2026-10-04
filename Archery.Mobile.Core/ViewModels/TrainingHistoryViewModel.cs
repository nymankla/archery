using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace Archery.Mobile.Core.ViewModels;

/// <summary>One attendee as shown in the history, already resolved to a display name.</summary>
public sealed record HistoryAttendee(string Name, string? Detail);

public sealed partial class TrainingHistoryViewModel(
    IArcheryApiClient api,
    ILogger<TrainingHistoryViewModel> logger) : BaseViewModel(logger)
{
    public ObservableCollection<DateTime> Dates { get; } = [];

    public ObservableCollection<HistoryAttendee> MemberAttendees { get; } = [];

    public ObservableCollection<HistoryAttendee> ExternalAttendees { get; } = [];

    [ObservableProperty]
    public partial DateTime? SelectedDate { get; set; }

    [ObservableProperty]
    public partial string? Notes { get; set; }

    [ObservableProperty]
    public partial string Summary { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool HasDates { get; set; }

    [ObservableProperty]
    public partial bool HasExternals { get; set; }

    public override Task OnAppearingAsync(CancellationToken ct = default)
    {
        Title = "Training history";
        return Dates.Count == 0 ? LoadDatesAsync(ct) : Task.CompletedTask;
    }

    [RelayCommand]
    async Task LoadDatesAsync(CancellationToken ct)
    {
        await RunAsync(async token =>
        {
            var dates = await api.GetTrainingDatesAsync(token) ?? [];

            Dates.Clear();
            foreach (var date in dates.OrderByDescending(d => d))
                Dates.Add(date.ToDateTime(TimeOnly.MinValue));

            HasDates = Dates.Count > 0;

            // Open on the most recent session, which is almost always the one being looked for.
            //
            // The detail is awaited here rather than left to OnSelectedDateChanged. That handler
            // routes through RunAsync, which refuses to re-enter while IsBusy — and IsBusy is
            // already true because this very method is running inside it. Assigning the property
            // and walking away therefore left the auto-selected date showing no attendees at all.
            if (HasDates && SelectedDate is null)
            {
                _suppressDateReload = true;
                SelectedDate = Dates[0];
                _suppressDateReload = false;

                await LoadDetailAsync(Dates[0], token);
            }
        }, ct);
    }

    [RelayCommand]
    async Task RefreshAsync(CancellationToken ct)
    {
        IsRefreshing = true;
        SelectedDate = null;
        Dates.Clear();
        await LoadDatesAsync(ct);
    }

    /// <summary>Set while the view model selects a date itself, so the handler below stands down.</summary>
    bool _suppressDateReload;

    async partial void OnSelectedDateChanged(DateTime? value)
    {
        if (_suppressDateReload)
            return;

        if (value is null)
        {
            MemberAttendees.Clear();
            ExternalAttendees.Clear();
            Summary = string.Empty;
            return;
        }

        await RunAsync(token => LoadDetailAsync(value.Value, token));
    }

    async Task LoadDetailAsync(DateTime date, CancellationToken ct)
    {
        var detail = await api.GetTrainingAttendanceByDateAsync(DateOnly.FromDateTime(date), ct);

        MemberAttendees.Clear();
        ExternalAttendees.Clear();
        Notes = detail?.Notes;

        if (detail is not null)
        {
            foreach (var attendee in detail.Attendees.Where(a => a.MemberId.HasValue))
            {
                MemberAttendees.Add(new HistoryAttendee(
                    $"{attendee.MemberFirstName} {attendee.MemberLastName}".Trim(),
                    attendee.MemberPersonnummer));
            }

            foreach (var attendee in detail.Attendees.Where(a => a.ExternalParticipantId.HasValue))
            {
                ExternalAttendees.Add(new HistoryAttendee(
                    $"{attendee.ExternalParticipantFirstName} {attendee.ExternalParticipantLastName}".Trim(),
                    null));
            }
        }

        HasExternals = ExternalAttendees.Count > 0;
        var total = MemberAttendees.Count + ExternalAttendees.Count;
        Summary = total == 0
            ? "No attendees recorded for this date."
            : $"{total} attendee(s): {MemberAttendees.Count} member(s), {ExternalAttendees.Count} guest(s)";
    }
}
