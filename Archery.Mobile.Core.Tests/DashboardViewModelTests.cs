namespace Archery.Mobile.Core.Tests;

public class DashboardViewModelTests
{
    static DashboardData Full() => new()
    {
        Members = new DashboardMemberStats { TotalActive = 117, TotalInactive = 36, NewThisYear = 11 },
        Fees = new DashboardFeeStats
        {
            Paid = 115,
            Unpaid = 0,
            Partial = 0,
            NoFee = 2,
            TotalCollected = 57800m,
            TotalOutstanding = 0m,
            CollectionRatePct = 98
        },
        Competitions = new DashboardCompetitionStats
        {
            TotalThisYear = 13,
            UpcomingCount = 1,
            NextCompetition = new DashboardNextCompetition
            {
                Name = "Winter Championship Qualifier",
                Date = new DateOnly(2026, 11, 21),
                Location = "Sundsvall Arena"
            }
        },
        TopScorers =
        [
            new DashboardTopScorer { MemberName = "Klas Nyman", BowClass = BowClass.Recurve, Score = 500, CompetitionName = "Midsummer Match" }
        ],
        RecentCompetitions =
        [
            new DashboardRecentCompetition { Name = "Autumn Target Meet", Date = new DateOnly(2026, 9, 5), Location = "Malmo", Type = CompetitionType.Indoor, ParticipantCount = 0 }
        ]
    };

    static (DashboardViewModel Vm, FakeApiClient Api, FakeNavigationService Nav) Build(DashboardData? data)
    {
        var api = new FakeApiClient { Dashboard = data };
        var nav = new FakeNavigationService();
        return (new DashboardViewModel(api, nav, TestLogger.For<DashboardViewModel>()), api, nav);
    }

    [Fact]
    public async Task Load_PopulatesEverythingFromOneCall()
    {
        var (vm, api, _) = Build(Full());

        await vm.OnAppearingAsync();

        Assert.True(vm.HasData);
        Assert.Single(api.Calls);
        Assert.Equal("GetDashboard", api.Calls[0]);
        Assert.Equal(117, vm.Data?.Members.TotalActive);
        Assert.Single(vm.TopScorers);
        Assert.Single(vm.RecentCompetitions);
        Assert.True(vm.HasTopScorers);
        Assert.True(vm.HasRecentCompetitions);
    }

    // The collection rate is a single ratio against a limit, so it drives a meter; the API
    // reports a whole percentage and the control wants 0..1.
    [Fact]
    public async Task CollectionRate_IsScaledForTheMeter()
    {
        var (vm, _, _) = Build(Full());

        await vm.OnAppearingAsync();

        Assert.Equal(0.98d, vm.CollectionRate, 3);
    }

    [Theory]
    [InlineData(-5, 0d)]
    [InlineData(0, 0d)]
    [InlineData(150, 1d)]
    public async Task CollectionRate_IsClampedSoTheMeterCannotOverflow(int reported, double expected)
    {
        var data = Full();
        data.Fees.CollectionRatePct = reported;
        var (vm, _, _) = Build(data);

        await vm.OnAppearingAsync();

        Assert.Equal(expected, vm.CollectionRate, 3);
    }

    [Fact]
    public async Task NextCompetition_IsSummarisedForTheCallout()
    {
        var (vm, _, _) = Build(Full());

        await vm.OnAppearingAsync();

        Assert.True(vm.HasNextCompetition);
        Assert.Equal("Winter Championship Qualifier · 2026-11-21 · Sundsvall Arena",
            vm.NextCompetitionSummary);
    }

    [Fact]
    public async Task NoUpcomingCompetition_HidesTheCallout()
    {
        var data = Full();
        data.Competitions.NextCompetition = null;
        var (vm, _, _) = Build(data);

        await vm.OnAppearingAsync();

        Assert.False(vm.HasNextCompetition);
        Assert.Equal(string.Empty, vm.NextCompetitionSummary);
    }

    [Fact]
    public async Task EmptyClub_ShowsNoSectionsRatherThanBlankHeadings()
    {
        var data = Full();
        data.TopScorers = [];
        data.RecentCompetitions = [];
        var (vm, _, _) = Build(data);

        await vm.OnAppearingAsync();

        Assert.False(vm.HasTopScorers);
        Assert.False(vm.HasRecentCompetitions);
    }

    [Fact]
    public async Task NullResponse_LeavesTheScreenEmptyWithoutCrashing()
    {
        var (vm, _, _) = Build(null);

        await vm.OnAppearingAsync();

        Assert.False(vm.HasData);
        Assert.False(vm.HasNextCompetition);
        Assert.Equal(0d, vm.CollectionRate);
        Assert.Empty(vm.TopScorers);
    }

    [Fact]
    public async Task Reload_ReplacesRatherThanAppends()
    {
        var (vm, _, _) = Build(Full());
        await vm.OnAppearingAsync();
        Assert.Single(vm.TopScorers);

        await vm.LoadCommand.ExecuteAsync(null);

        Assert.Single(vm.TopScorers);
        Assert.Single(vm.RecentCompetitions);
    }

    [Fact]
    public async Task LoadFailure_IsReported()
    {
        var (vm, api, _) = Build(Full());
        api.Throws = new HttpRequestException("down");

        await vm.OnAppearingAsync();

        Assert.True(vm.HasError);
        Assert.Equal("Cannot reach the server. Check your connection and try again.", vm.ErrorMessage);
    }

    [Fact]
    public async Task TilesNavigateToTheirSections()
    {
        var (vm, _, nav) = Build(Full());
        await vm.OnAppearingAsync();

        await vm.OpenMembersCommand.ExecuteAsync(null);
        Assert.Equal(Routes.Members, nav.Routes[^1]);

        await vm.OpenCompetitionsCommand.ExecuteAsync(null);
        Assert.Equal(Routes.Competitions, nav.Routes[^1]);

        await vm.OpenTrainingCommand.ExecuteAsync(null);
        Assert.Equal(Routes.TrainingAttendance, nav.Routes[^1]);
    }
}
