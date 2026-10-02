namespace Archery.Mobile.Core.Tests;

public class TrainingHistoryViewModelTests
{
    static (TrainingHistoryViewModel Vm, FakeApiClient Api) Build(params DateOnly[] dates)
    {
        var api = new FakeApiClient { TrainingDates = [.. dates] };
        return (new TrainingHistoryViewModel(api, TestLogger.For<TrainingHistoryViewModel>()), api);
    }

    [Fact]
    public async Task Dates_AreNewestFirstAndTheLatestOpensAutomatically()
    {
        var (vm, api) = Build(new DateOnly(2026, 1, 5), new DateOnly(2026, 3, 9), new DateOnly(2026, 2, 1));
        api.Detail = new TrainingSessionDetail { SessionId = Guid.NewGuid(), Date = new DateOnly(2026, 3, 9) };

        await vm.OnAppearingAsync();

        Assert.True(vm.HasDates);
        Assert.Equal(new DateTime(2026, 3, 9), vm.Dates[0]);
        Assert.Equal(new DateTime(2026, 1, 5), vm.Dates[^1]);
        // Opening on the most recent session saves a tap in the common case.
        Assert.Equal(new DateTime(2026, 3, 9), vm.SelectedDate);
    }

    [Fact]
    public async Task NoDates_LeavesTheScreenEmptyWithoutError()
    {
        var (vm, _) = Build();

        await vm.OnAppearingAsync();

        Assert.False(vm.HasDates);
        Assert.Null(vm.SelectedDate);
        Assert.False(vm.HasError);
    }

    [Fact]
    public async Task SelectingADate_SplitsMembersFromGuests()
    {
        var (vm, api) = Build(new DateOnly(2026, 3, 9));
        api.Detail = new TrainingSessionDetail
        {
            SessionId = Guid.NewGuid(),
            Date = new DateOnly(2026, 3, 9),
            Notes = "Indoor",
            Attendees =
            [
                new TrainingAttendeeInfo
                {
                    MemberId = Guid.NewGuid(),
                    MemberFirstName = "Alex",
                    MemberLastName = "Anderson",
                    MemberPersonnummer = "199005170000"
                },
                new TrainingAttendeeInfo
                {
                    ExternalParticipantId = Guid.NewGuid(),
                    ExternalParticipantFirstName = "Björn",
                    ExternalParticipantLastName = "Eriksson"
                }
            ]
        };

        await vm.OnAppearingAsync();

        Assert.Single(vm.MemberAttendees);
        Assert.Equal("Alex Anderson", vm.MemberAttendees[0].Name);
        Assert.Equal("199005170000", vm.MemberAttendees[0].Detail);

        Assert.Single(vm.ExternalAttendees);
        Assert.Equal("Björn Eriksson", vm.ExternalAttendees[0].Name);
        Assert.True(vm.HasExternals);

        Assert.Equal("Indoor", vm.Notes);
        Assert.Equal("2 attendee(s): 1 member(s), 1 guest(s)", vm.Summary);
    }

    [Fact]
    public async Task ADateWithNoAttendeesSaysSoRatherThanLookingBroken()
    {
        var (vm, api) = Build(new DateOnly(2026, 3, 9));
        api.Detail = new TrainingSessionDetail { SessionId = Guid.NewGuid(), Date = new DateOnly(2026, 3, 9) };

        await vm.OnAppearingAsync();

        Assert.Empty(vm.MemberAttendees);
        Assert.False(vm.HasExternals);
        Assert.Equal("No attendees recorded for this date.", vm.Summary);
    }

    [Fact]
    public async Task SwitchingDate_ReplacesRatherThanAppends()
    {
        var (vm, api) = Build(new DateOnly(2026, 3, 9), new DateOnly(2026, 2, 1));
        api.Detail = new TrainingSessionDetail
        {
            SessionId = Guid.NewGuid(),
            Date = new DateOnly(2026, 3, 9),
            Attendees = [new TrainingAttendeeInfo { MemberId = Guid.NewGuid(), MemberFirstName = "Alex", MemberLastName = "Anderson" }]
        };

        await vm.OnAppearingAsync();
        Assert.Single(vm.MemberAttendees);

        api.Detail = new TrainingSessionDetail
        {
            SessionId = Guid.NewGuid(),
            Date = new DateOnly(2026, 2, 1),
            Attendees = [new TrainingAttendeeInfo { MemberId = Guid.NewGuid(), MemberFirstName = "Bo", MemberLastName = "Berg" }]
        };
        vm.SelectedDate = new DateTime(2026, 2, 1);

        Assert.Single(vm.MemberAttendees);
        Assert.Equal("Bo Berg", vm.MemberAttendees[0].Name);
    }

    [Fact]
    public async Task LoadFailure_IsReported()
    {
        var (vm, api) = Build(new DateOnly(2026, 3, 9));
        api.Throws = new ArcheryApiException(new ApiError(500, []));

        await vm.OnAppearingAsync();

        Assert.True(vm.HasError);
    }
}
