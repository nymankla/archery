using System.Net;

namespace Archery.Mobile.Core.Tests;

public class CompetitionsViewModelTests
{
    static Competition C(string name, int year, string location = "Range") => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        Date = new DateOnly(year, 6, 1),
        Location = location,
        RoundType = "WA 18m",
        Type = CompetitionType.Indoor
    };

    static (CompetitionsViewModel Vm, FakeApiClient Api, FakeNavigationService Nav, FakeDialogService Dialogs)
        Build(params Competition[] competitions)
    {
        var api = new FakeApiClient { Competitions = [.. competitions] };
        var nav = new FakeNavigationService();
        var dialogs = new FakeDialogService();
        return (new CompetitionsViewModel(api, nav, dialogs, TestLogger.For<CompetitionsViewModel>()),
            api, nav, dialogs);
    }

    [Fact]
    public async Task Competitions_AreNewestFirst()
    {
        var (vm, _, _, _) = Build(C("Old", 2020), C("New", 2026), C("Middle", 2023));

        await vm.OnAppearingAsync();

        Assert.Equal(["New", "Middle", "Old"], vm.Competitions.Select(c => c.Name));
    }

    [Fact]
    public async Task Search_MatchesNameAndLocation()
    {
        var (vm, _, _, _) = Build(C("Winter Cup", 2026, "Sundsvall"), C("Spring Open", 2026, "Umeå"));

        await vm.OnAppearingAsync();

        vm.SearchText = "sundsv";
        Assert.Single(vm.Competitions);
        Assert.Equal("Winter Cup", vm.Competitions[0].Name);

        vm.SearchText = "spring";
        Assert.Single(vm.Competitions);
    }

    [Fact]
    public async Task Delete_WarnsThatChildRecordsGoTooAndRemovesOnConfirm()
    {
        var (vm, api, _, dialogs) = Build(C("Winter Cup", 2026));
        await vm.OnAppearingAsync();

        await vm.DeleteCommand.ExecuteAsync(vm.Competitions[0]);

        Assert.Single(dialogs.Confirmations);
        Assert.Contains(api.Calls, c => c.StartsWith("DeleteCompetition"));
        Assert.Empty(vm.Competitions);
    }

    [Fact]
    public async Task Open_NavigatesWithTheCompetitionId()
    {
        var (vm, _, nav, _) = Build(C("Winter Cup", 2026));
        await vm.OnAppearingAsync();
        var target = vm.Competitions[0];

        await vm.OpenCommand.ExecuteAsync(target);

        Assert.Equal(Routes.CompetitionDetail, nav.Routes[^1]);
        Assert.Equal(target.Id, nav.LastParameters?[Routes.CompetitionIdKey]);
    }
}

public class CompetitionEditViewModelTests
{
    static (CompetitionEditViewModel Vm, FakeApiClient Api, FakeNavigationService Nav) Build(
        params Competition[] competitions)
    {
        var api = new FakeApiClient { Competitions = [.. competitions] };
        var nav = new FakeNavigationService();
        return (new CompetitionEditViewModel(api, nav, TestLogger.For<CompetitionEditViewModel>()), api, nav);
    }

    [Fact]
    public async Task RequiresNameLocationAndRound()
    {
        var (vm, _, _) = Build();
        await vm.InitialiseAsync(null);

        Assert.True(vm.IsNew);
        Assert.False(vm.CanSave);

        vm.Name = "Winter Cup";
        vm.Location = "Sundsvall";
        Assert.False(vm.CanSave);

        vm.RoundType = "WA 18m";
        Assert.True(vm.CanSave);
    }

    [Fact]
    public async Task Save_TrimsAndSendsNullForABlankDescription()
    {
        var (vm, api, nav) = Build();
        await vm.InitialiseAsync(null);
        vm.Name = "  Winter Cup  ";
        vm.Location = " Sundsvall ";
        vm.RoundType = " WA 18m ";
        vm.Description = "   ";
        vm.Type = CompetitionType.Field;

        await vm.SaveCommand.ExecuteAsync(null);

        Assert.Equal("Winter Cup", api.LastCompetition?.Name);
        Assert.Equal("Sundsvall", api.LastCompetition?.Location);
        Assert.Null(api.LastCompetition?.Description);
        Assert.Equal(CompetitionType.Field, api.LastCompetition?.Type);
        Assert.Equal(1, nav.BackCount);
    }

    [Fact]
    public async Task Editing_LoadsThenUpdates()
    {
        var existing = new Competition
        {
            Id = Guid.NewGuid(),
            Name = "Winter Cup",
            Location = "Sundsvall",
            RoundType = "WA 18m",
            Date = new DateOnly(2026, 11, 21),
            Type = CompetitionType.Indoor
        };
        var (vm, api, _) = Build(existing);

        await vm.InitialiseAsync(existing.Id);
        Assert.False(vm.IsNew);
        Assert.Equal("Winter Cup", vm.Name);
        Assert.Equal(new DateTime(2026, 11, 21), vm.Date);

        vm.Name = "Winter Championship";
        await vm.SaveCommand.ExecuteAsync(null);

        Assert.Contains($"UpdateCompetition:{existing.Id}", api.Calls);
        Assert.Equal("Winter Championship", api.LastCompetition?.Name);
    }
}

public class CompetitionDetailViewModelTests
{
    static readonly Guid CompetitionId = Guid.NewGuid();

    static CompetitionParticipant P(string name, Guid? memberId = null, Guid? externalId = null,
        BowClass bow = BowClass.Recurve)
    {
        var member = memberId is { } id
            ? new Member { Id = id, FirstName = name, LastName = "X" }
            : null;
        var external = externalId is { } eid
            ? new ExternalParticipant { Id = eid, FirstName = name, LastName = "X" }
            : null;

        return new CompetitionParticipant
        {
            Id = Guid.NewGuid(),
            CompetitionId = CompetitionId,
            MemberId = memberId,
            Member = member,
            ExternalParticipantId = externalId,
            ExternalParticipant = external,
            BowClass = bow,
            AgeClass = AgeClass.Senior,
            Gender = Gender.Unknown
        };
    }

    static CompetitionResult R(string name, int score, int x = 0, Guid? memberId = null,
        BowClass bow = BowClass.Recurve) => new()
    {
        Id = Guid.NewGuid(),
        CompetitionId = CompetitionId,
        MemberId = memberId ?? Guid.NewGuid(),
        Member = new Member { Id = memberId ?? Guid.NewGuid(), FirstName = name, LastName = "X" },
        BowClass = bow,
        TotalScore = score,
        XCount = x
    };

    static (CompetitionDetailViewModel Vm, FakeApiClient Api, FakeNavigationService Nav, FakeDialogService Dialogs)
        Build(CompetitionParticipant[]? participants = null, CompetitionResult[]? results = null)
    {
        var api = new FakeApiClient
        {
            Competitions = [new Competition { Id = CompetitionId, Name = "Winter Cup", Location = "Sundsvall", RoundType = "WA 18m" }],
            Participants = [.. participants ?? []],
            Results = [.. results ?? []]
        };
        var nav = new FakeNavigationService();
        var dialogs = new FakeDialogService();
        return (new CompetitionDetailViewModel(api, nav, dialogs, TestLogger.For<CompetitionDetailViewModel>()),
            api, nav, dialogs);
    }

    // A scoreboard reads highest first. Placement cannot be the primary sort because it is only
    // set once somebody assigns it.
    [Fact]
    public async Task Results_AreHighestScoreFirstWithXCountBreakingTies()
    {
        var (vm, _, _, _) = Build(results:
        [
            R("Low", 400),
            R("TieFewerX", 560, x: 8),
            R("TieMoreX", 560, x: 12)
        ]);

        await vm.InitialiseAsync(CompetitionId);

        Assert.Equal(["TieMoreX", "TieFewerX", "Low"], vm.Results.Select(r => r.ParticipantName.Split(' ')[0]));
    }

    [Fact]
    public async Task AwaitingResult_ListsRegisteredArchersWithoutAScoreYet()
    {
        var scoredMember = Guid.NewGuid();
        var unscoredMember = Guid.NewGuid();

        var (vm, _, _, _) = Build(
            participants: [P("Scored", memberId: scoredMember), P("Unscored", memberId: unscoredMember)],
            results: [R("Scored", 500, memberId: scoredMember)]);

        await vm.InitialiseAsync(CompetitionId);

        Assert.True(vm.HasAwaitingResult);
        Assert.Single(vm.AwaitingResult);
        Assert.StartsWith("Unscored", vm.AwaitingResult[0].ParticipantName);
    }

    [Fact]
    public async Task AwaitingResult_IsEmptyOnceEveryoneHasScored()
    {
        var memberId = Guid.NewGuid();
        var (vm, _, _, _) = Build(
            participants: [P("Scored", memberId: memberId)],
            results: [R("Scored", 500, memberId: memberId)]);

        await vm.InitialiseAsync(CompetitionId);

        Assert.False(vm.HasAwaitingResult);
        Assert.Empty(vm.AwaitingResult);
    }

    [Fact]
    public async Task BowClassFilter_NarrowsBothLists()
    {
        var (vm, _, _, _) = Build(
            participants: [P("Rec", memberId: Guid.NewGuid()), P("Comp", memberId: Guid.NewGuid(), bow: BowClass.Compound)],
            results: [R("Rec", 500), R("Comp", 520, bow: BowClass.Compound)]);

        await vm.InitialiseAsync(CompetitionId);
        Assert.Equal(2, vm.Participants.Count);
        Assert.Equal(2, vm.Results.Count);

        vm.BowClassFilter = BowClass.Compound;

        Assert.Single(vm.Participants);
        Assert.Single(vm.Results);
    }

    [Fact]
    public async Task Tabs_AreMutuallyExclusive()
    {
        var (vm, _, _, _) = Build();
        await vm.InitialiseAsync(CompetitionId);

        Assert.True(vm.ShowParticipants);
        Assert.False(vm.ShowResults);

        vm.ShowResultsTabCommand.Execute(null);
        Assert.False(vm.ShowParticipants);
        Assert.True(vm.ShowResults);
    }

    [Fact]
    public async Task EmptyStatesReadAsSentencesNotZeroes()
    {
        var (vm, _, _, _) = Build();

        await vm.InitialiseAsync(CompetitionId);

        Assert.Equal("No participants registered yet.", vm.ParticipantSummary);
        Assert.Equal("No results recorded yet.", vm.ResultSummary);
    }

    [Fact]
    public async Task AddResultFor_PassesTheParticipantToPrefillFrom()
    {
        var participant = P("Unscored", memberId: Guid.NewGuid());
        var (vm, _, nav, _) = Build(participants: [participant]);
        await vm.InitialiseAsync(CompetitionId);

        await vm.AddResultForCommand.ExecuteAsync(vm.AwaitingResult[0]);

        Assert.Equal(Routes.ResultEdit, nav.Routes[^1]);
        Assert.Equal(participant.Id, nav.LastParameters?[Routes.FromParticipantIdKey]);
        Assert.Equal(CompetitionId, nav.LastParameters?[Routes.CompetitionIdKey]);
    }

    [Fact]
    public async Task RemoveParticipant_ConfirmsThenDropsTheRow()
    {
        var (vm, api, _, dialogs) = Build(participants: [P("Someone", memberId: Guid.NewGuid())]);
        await vm.InitialiseAsync(CompetitionId);

        await vm.RemoveParticipantCommand.ExecuteAsync(vm.Participants[0]);

        Assert.Single(dialogs.Confirmations);
        Assert.Contains(api.Calls, c => c.StartsWith("RemoveParticipant"));
        Assert.Empty(vm.Participants);
    }

    [Fact]
    public async Task DeleteResult_KeepsTheRowWhenTheServerRefuses()
    {
        var (vm, api, _, _) = Build(results: [R("Someone", 500)]);
        await vm.InitialiseAsync(CompetitionId);

        api.MutationStatus = HttpStatusCode.BadRequest;
        api.MutationBody = """{"errors":["Cannot delete a published result."]}""";

        await vm.DeleteResultCommand.ExecuteAsync(vm.Results[0]);

        Assert.Single(vm.Results);
        Assert.Equal("Cannot delete a published result.", vm.ErrorMessage);
    }
}
