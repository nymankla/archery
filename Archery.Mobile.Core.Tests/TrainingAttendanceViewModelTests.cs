namespace Archery.Mobile.Core.Tests;

public class TrainingAttendanceViewModelTests
{
    static Member M(string first, string last, bool active = true) => new()
    {
        Id = Guid.NewGuid(),
        FirstName = first,
        LastName = last,
        IsActive = active,
        Email = $"{first.ToLowerInvariant()}@example.com",
        DateOfBirth = new DateOnly(1990, 1, 1),
        JoinDate = new DateOnly(2020, 1, 1)
    };

    static ExternalParticipant E(string first, string last, string? club = null) => new()
    {
        Id = Guid.NewGuid(),
        FirstName = first,
        LastName = last,
        ClubAffiliation = club
    };

    static (TrainingAttendanceViewModel Vm, FakeApiClient Api, FakeDialogService Dialogs) Build(
        Member[]? members = null, ExternalParticipant[]? externals = null)
    {
        var api = new FakeApiClient
        {
            Members = [.. members ?? []],
            Externals = [.. externals ?? []]
        };
        var dialogs = new FakeDialogService();
        return (new TrainingAttendanceViewModel(api, dialogs,
            TestLogger.For<TrainingAttendanceViewModel>()), api, dialogs);
    }

    // The endpoint answers with sessionId null and no attendees when nothing has been recorded
    // for the date, rather than 404. Treating that as an error, or dereferencing SessionId,
    // is the most likely way this screen breaks.
    [Fact]
    public async Task NoSessionForTheDate_LoadsCleanlyWithNothingTicked()
    {
        var (vm, api, _) = Build([M("Alex", "Anderson")], [E("Björn", "Eriksson")]);
        api.Detail = new TrainingSessionDetail { SessionId = null, Date = DateOnly.FromDateTime(DateTime.Today) };

        await vm.OnAppearingAsync();

        Assert.False(vm.HasError);
        Assert.False(vm.HasExistingSession);
        Assert.Equal(0, vm.SelectedCount);
        Assert.Single(vm.MemberRows);
        Assert.Single(vm.ExternalRows);
    }

    [Fact]
    public async Task ExistingSession_TicksWhoeverWasRecorded()
    {
        var present = M("Alex", "Anderson");
        var absent = M("Bo", "Berg");
        var guest = E("Björn", "Eriksson");
        var (vm, api, _) = Build([present, absent], [guest]);

        api.Detail = new TrainingSessionDetail
        {
            SessionId = Guid.NewGuid(),
            Date = DateOnly.FromDateTime(DateTime.Today),
            Notes = "Indoor, 18m",
            Attendees =
            [
                new TrainingAttendeeInfo { MemberId = present.Id },
                new TrainingAttendeeInfo { ExternalParticipantId = guest.Id }
            ]
        };

        await vm.OnAppearingAsync();

        Assert.True(vm.HasExistingSession);
        Assert.Equal("Indoor, 18m", vm.Notes);
        Assert.Equal(2, vm.SelectedCount);
        Assert.True(vm.MemberRows.First(r => r.Id == present.Id).IsSelected);
        Assert.False(vm.MemberRows.First(r => r.Id == absent.Id).IsSelected);
        Assert.True(vm.ExternalRows[0].IsSelected);
    }

    // The thing that goes wrong if selection is bound to the filtered collection instead of to
    // stable row objects: tick someone, search for someone else, and the first tick is lost.
    [Fact]
    public async Task SelectionSurvivesSearching()
    {
        var (vm, _, _) = Build([M("Alex", "Anderson"), M("Bo", "Berg")]);
        await vm.OnAppearingAsync();

        var alex = vm.MemberRows.First(r => r.Name.StartsWith("Alex"));
        vm.ToggleCommand.Execute(alex);
        Assert.Equal(1, vm.SelectedCount);

        vm.MemberSearch = "Bo";
        Assert.Single(vm.MemberRows);
        Assert.Equal(1, vm.SelectedCount);

        vm.MemberSearch = string.Empty;
        Assert.True(vm.MemberRows.First(r => r.Id == alex.Id).IsSelected);
        Assert.Equal(1, vm.SelectedCount);
    }

    [Fact]
    public async Task ActiveOnly_HidesInactiveMembersButNotGuests()
    {
        var (vm, _, _) = Build(
            [M("Alex", "Anderson"), M("Bo", "Berg", active: false)],
            [E("Björn", "Eriksson")]);

        await vm.OnAppearingAsync();

        Assert.True(vm.ActiveOnly);
        Assert.Single(vm.MemberRows);
        Assert.Single(vm.ExternalRows);

        vm.ActiveOnly = false;
        Assert.Equal(2, vm.MemberRows.Count);
    }

    // Save is a full replace of the attendee list for that date, not a delta.
    [Fact]
    public async Task Save_SendsEveryTickedRowIncludingOnesHiddenByTheSearch()
    {
        var alex = M("Alex", "Anderson");
        var bo = M("Bo", "Berg");
        var guest = E("Björn", "Eriksson");
        var (vm, api, _) = Build([alex, bo], [guest]);
        await vm.OnAppearingAsync();

        vm.ToggleCommand.Execute(vm.MemberRows.First(r => r.Id == alex.Id));
        vm.ToggleCommand.Execute(vm.ExternalRows[0]);

        // Hide Alex behind a search before saving.
        vm.MemberSearch = "Bo";
        Assert.DoesNotContain(vm.MemberRows, r => r.Id == alex.Id);

        await vm.SaveCommand.ExecuteAsync(null);

        Assert.NotNull(api.LastSaved);
        Assert.Equal([alex.Id], api.LastSaved!.MemberIds);
        Assert.Equal([guest.Id], api.LastSaved.ExternalParticipantIds);
    }

    [Fact]
    public async Task Save_UsesTheSelectedDateAndTrimsNotes()
    {
        var (vm, api, _) = Build([M("Alex", "Anderson")]);
        await vm.OnAppearingAsync();

        vm.SelectedDate = new DateTime(2026, 3, 9);
        vm.Notes = "   Indoor   ";

        await vm.SaveCommand.ExecuteAsync(null);

        Assert.Equal(new DateOnly(2026, 3, 9), api.LastSavedDate);
        Assert.Equal("Indoor", api.LastSaved?.Notes);
    }

    [Fact]
    public async Task Save_SendsNullRatherThanBlankNotes()
    {
        var (vm, api, _) = Build([M("Alex", "Anderson")]);
        await vm.OnAppearingAsync();
        vm.Notes = "   ";

        await vm.SaveCommand.ExecuteAsync(null);

        Assert.Null(api.LastSaved?.Notes);
    }

    [Fact]
    public async Task Save_CanRecordAnEmptyRollCall()
    {
        // Unticking everyone and saving is how a session gets emptied, so it must still send.
        var (vm, api, _) = Build([M("Alex", "Anderson")]);
        await vm.OnAppearingAsync();

        await vm.SaveCommand.ExecuteAsync(null);

        Assert.NotNull(api.LastSaved);
        Assert.Empty(api.LastSaved!.MemberIds);
        Assert.Contains("Saved 0 attendee(s)", vm.StatusMessage);
    }

    [Fact]
    public async Task Save_ReportsTheServersMessageAndSetsNoSuccess()
    {
        var (vm, api, _) = Build([M("Alex", "Anderson")]);
        await vm.OnAppearingAsync();

        api.MutationStatus = System.Net.HttpStatusCode.BadRequest;
        api.MutationBody = """{"errors":["One or more selected participants are already registered."]}""";

        await vm.SaveCommand.ExecuteAsync(null);

        Assert.Equal("One or more selected participants are already registered.", vm.ErrorMessage);
        Assert.Null(vm.StatusMessage);
    }

    [Fact]
    public async Task ChangingTheDate_ReloadsThatDatesAttendance()
    {
        var alex = M("Alex", "Anderson");
        var (vm, api, _) = Build([alex]);
        await vm.OnAppearingAsync();

        vm.ToggleCommand.Execute(vm.MemberRows[0]);
        Assert.Equal(1, vm.SelectedCount);

        // A different date has no session, so the ticks must not carry over.
        api.Detail = new TrainingSessionDetail { SessionId = null, Date = new DateOnly(2026, 1, 1) };
        vm.SelectedDate = new DateTime(2026, 1, 1);

        Assert.Equal(0, vm.SelectedCount);
        Assert.False(vm.MemberRows[0].IsSelected);
        Assert.False(vm.HasExistingSession);
    }

    [Fact]
    public async Task ClearSelection_AsksFirstAndUnticksEverything()
    {
        var (vm, _, dialogs) = Build([M("Alex", "Anderson"), M("Bo", "Berg")]);
        await vm.OnAppearingAsync();
        vm.ToggleCommand.Execute(vm.MemberRows[0]);
        vm.ToggleCommand.Execute(vm.MemberRows[1]);

        await vm.ClearSelectionCommand.ExecuteAsync(null);

        Assert.Single(dialogs.Confirmations);
        Assert.Equal(0, vm.SelectedCount);
    }

    [Fact]
    public async Task ClearSelection_CancelledKeepsTheTicks()
    {
        var (vm, _, dialogs) = Build([M("Alex", "Anderson")]);
        await vm.OnAppearingAsync();
        vm.ToggleCommand.Execute(vm.MemberRows[0]);
        dialogs.ConfirmResult = false;

        await vm.ClearSelectionCommand.ExecuteAsync(null);

        Assert.Equal(1, vm.SelectedCount);
    }

    [Fact]
    public async Task LoadFailure_IsReportedNotSwallowed()
    {
        var (vm, api, _) = Build([M("Alex", "Anderson")]);
        api.Throws = new HttpRequestException("down");

        await vm.OnAppearingAsync();

        Assert.True(vm.HasError);
        Assert.Equal("Cannot reach the server. Check your connection and try again.", vm.ErrorMessage);
    }
}
