using System.Collections.ObjectModel;
using Archery.Mobile.Core.Abstractions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace Archery.Mobile.Core.ViewModels;

/// <summary>
/// Roll call for a training session: tick who turned up, add a note, save.
/// </summary>
/// <remarks>
/// The API is date-keyed rather than session-keyed, and the save is a full replace of the
/// attendee list for that date — not a delta. So the screen always loads the current state for
/// the chosen date first, and sends the complete set back.
/// </remarks>
public sealed partial class TrainingAttendanceViewModel(
    IArcheryApiClient api,
    IDialogService dialogs,
    ILogger<TrainingAttendanceViewModel> logger) : BaseViewModel(logger)
{
    readonly List<AttendeeRow> _allMembers = [];
    readonly List<AttendeeRow> _allExternals = [];

    public ObservableCollection<AttendeeRow> MemberRows { get; } = [];

    public ObservableCollection<AttendeeRow> ExternalRows { get; } = [];

    [ObservableProperty]
    public partial DateTime SelectedDate { get; set; } = DateTime.Today;

    [ObservableProperty]
    public partial string? Notes { get; set; }

    [ObservableProperty]
    public partial string MemberSearch { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ExternalSearch { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool ActiveOnly { get; set; } = true;

    [ObservableProperty]
    public partial int SelectedCount { get; set; }

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    /// <summary>True once a session exists for the chosen date, so the UI can say "new" vs "recorded".</summary>
    [ObservableProperty]
    public partial bool HasExistingSession { get; set; }

    public override Task OnAppearingAsync(CancellationToken ct = default)
    {
        Title = "Training";
        return MemberRows.Count == 0 ? LoadAsync(ct) : Task.CompletedTask;
    }

    [RelayCommand]
    async Task LoadAsync(CancellationToken ct)
    {
        await RunAsync(async token =>
        {
            StatusMessage = null;

            var members = await api.GetMembersAsync(token) ?? [];
            var externals = await api.GetExternalParticipantsAsync(token) ?? [];

            _allMembers.Clear();
            _allMembers.AddRange(members
                .OrderBy(m => m.FirstName).ThenBy(m => m.LastName)
                .Select(m => new AttendeeRow(m.Id, m.FullName, m.Email, m.IsActive)));

            _allExternals.Clear();
            _allExternals.AddRange(externals
                .OrderBy(e => e.FirstName).ThenBy(e => e.LastName)
                .Select(e => new AttendeeRow(e.Id, e.FullName, e.ClubAffiliation, true)));

            await ApplyExistingAttendanceAsync(token);
            ApplyFilters();
        }, ct);
    }

    /// <summary>Ticks whoever is already recorded for the chosen date.</summary>
    async Task ApplyExistingAttendanceAsync(CancellationToken ct)
    {
        var detail = await api.GetTrainingAttendanceByDateAsync(DateOnly.FromDateTime(SelectedDate), ct);

        foreach (var row in _allMembers.Concat(_allExternals))
            row.IsSelected = false;

        // The endpoint answers with sessionId null and an empty attendee list when nothing has
        // been recorded for that date yet, rather than 404.
        HasExistingSession = detail?.SessionId is not null;
        Notes = detail?.Notes;

        if (detail is null)
        {
            RecountSelected();
            return;
        }

        var memberIds = detail.Attendees
            .Where(a => a.MemberId.HasValue)
            .Select(a => a.MemberId!.Value)
            .ToHashSet();

        var externalIds = detail.Attendees
            .Where(a => a.ExternalParticipantId.HasValue)
            .Select(a => a.ExternalParticipantId!.Value)
            .ToHashSet();

        foreach (var row in _allMembers)
            row.IsSelected = memberIds.Contains(row.Id);

        foreach (var row in _allExternals)
            row.IsSelected = externalIds.Contains(row.Id);

        RecountSelected();
    }

    [RelayCommand]
    async Task SaveAsync(CancellationToken ct)
    {
        var request = new SaveTrainingAttendanceRequest
        {
            Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim(),
            // Taken from every row, not just the visible ones: a search filter must never
            // silently drop someone who was already ticked.
            MemberIds = [.. _allMembers.Where(r => r.IsSelected).Select(r => r.Id)],
            ExternalParticipantIds = [.. _allExternals.Where(r => r.IsSelected).Select(r => r.Id)]
        };

        var saved = await RunAsync(token => api
            .SaveTrainingAttendanceAsync(DateOnly.FromDateTime(SelectedDate), request, token)
            .EnsureArcherySuccessAsync(token), ct);

        if (!saved)
            return;

        HasExistingSession = true;
        var total = request.MemberIds.Count + request.ExternalParticipantIds.Count;
        StatusMessage = $"Saved {total} attendee(s) for {SelectedDate:yyyy-MM-dd}.";
    }

    [RelayCommand]
    async Task ClearSelectionAsync()
    {
        if (SelectedCount == 0)
            return;

        if (!await dialogs.ConfirmAsync("Clear selection",
                "Untick everyone for this date?", "Clear", "Cancel"))
        {
            return;
        }

        foreach (var row in _allMembers.Concat(_allExternals))
            row.IsSelected = false;

        RecountSelected();
    }

    /// <summary>Called by the view when a row is ticked, so the running total stays correct.</summary>
    [RelayCommand]
    void Toggle(AttendeeRow? row)
    {
        if (row is null)
            return;

        row.IsSelected = !row.IsSelected;
        RecountSelected();
    }

    public void RecountSelected() =>
        SelectedCount = _allMembers.Count(r => r.IsSelected) + _allExternals.Count(r => r.IsSelected);

    async partial void OnSelectedDateChanged(DateTime value)
    {
        // Changing the date means a different session entirely, so reload rather than keeping
        // the current ticks.
        if (_allMembers.Count == 0)
            return;

        StatusMessage = null;
        await RunAsync(async token =>
        {
            await ApplyExistingAttendanceAsync(token);
            ApplyFilters();
        });
    }

    partial void OnMemberSearchChanged(string value) => ApplyFilters();

    partial void OnExternalSearchChanged(string value) => ApplyFilters();

    partial void OnActiveOnlyChanged(bool value) => ApplyFilters();

    void ApplyFilters()
    {
        Fill(MemberRows, _allMembers.Where(r => !ActiveOnly || r.IsActive), MemberSearch);
        Fill(ExternalRows, _allExternals, ExternalSearch);
    }

    static void Fill(ObservableCollection<AttendeeRow> target, IEnumerable<AttendeeRow> source, string search)
    {
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            source = source.Where(r =>
                r.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
                || (r.Subtitle?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        target.Clear();
        foreach (var row in source)
            target.Add(row);
    }
}
