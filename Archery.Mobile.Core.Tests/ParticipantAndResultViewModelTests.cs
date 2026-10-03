using System.Globalization;
using System.Net;

namespace Archery.Mobile.Core.Tests;

public class ParticipantRegisterViewModelTests
{
    static readonly Guid CompetitionId = Guid.NewGuid();

    static Member M(string name, bool active = true) => new()
    {
        Id = Guid.NewGuid(),
        FirstName = name,
        LastName = "Archer",
        IsActive = active,
        Email = $"{name.ToLowerInvariant()}@example.com"
    };

    static ExternalParticipant E(string name, string? club = null) => new()
    {
        Id = Guid.NewGuid(),
        FirstName = name,
        LastName = "Guest",
        ClubAffiliation = club
    };

    static (ParticipantRegisterViewModel Vm, FakeApiClient Api, FakeNavigationService Nav) Build(
        Member[]? members = null, ExternalParticipant[]? externals = null,
        CompetitionParticipant[]? registered = null)
    {
        var api = new FakeApiClient
        {
            Members = [.. members ?? []],
            Externals = [.. externals ?? []],
            Participants = [.. registered ?? []]
        };
        var nav = new FakeNavigationService();
        return (new ParticipantRegisterViewModel(api, nav, TestLogger.For<ParticipantRegisterViewModel>()),
            api, nav);
    }

    [Fact]
    public async Task CannotSaveUntilSomeoneIsChosen()
    {
        var (vm, _, _) = Build(members: [M("Alex")]);

        await vm.InitialiseAsync(CompetitionId);

        Assert.False(vm.CanSave);
        vm.Selected = vm.Options[0];
        Assert.True(vm.CanSave);
    }

    // The database requires exactly one of MemberId / ExternalParticipantId. Posting a member
    // registration must leave the external id null, and vice versa.
    [Fact]
    public async Task RegisteringAMember_SetsOnlyTheMemberId()
    {
        var alex = M("Alex");
        var (vm, api, nav) = Build(members: [alex], externals: [E("Björn")]);
        await vm.InitialiseAsync(CompetitionId);

        vm.Kind = ParticipantKind.Member;
        vm.Selected = vm.Options.First(o => o.Id == alex.Id);
        vm.BowClass = BowClass.Barebow;
        vm.AgeClass = AgeClass.Master;
        vm.Gender = Gender.Female;

        await vm.SaveCommand.ExecuteAsync(null);

        Assert.Equal(alex.Id, api.LastRegistered?.MemberId);
        Assert.Null(api.LastRegistered?.ExternalParticipantId);
        Assert.Equal(BowClass.Barebow, api.LastRegistered?.BowClass);
        Assert.Equal(AgeClass.Master, api.LastRegistered?.AgeClass);
        Assert.Equal(Gender.Female, api.LastRegistered?.Gender);
        Assert.Equal(1, nav.BackCount);
    }

    [Fact]
    public async Task RegisteringAGuest_SetsOnlyTheExternalId()
    {
        var bjorn = E("Björn", "Göteborgs BK");
        var (vm, api, _) = Build(members: [M("Alex")], externals: [bjorn]);
        await vm.InitialiseAsync(CompetitionId);

        vm.Kind = ParticipantKind.External;
        vm.Selected = vm.Options.First(o => o.Id == bjorn.Id);

        await vm.SaveCommand.ExecuteAsync(null);

        Assert.Equal(bjorn.Id, api.LastRegistered?.ExternalParticipantId);
        Assert.Null(api.LastRegistered?.MemberId);
    }

    // Switching member/guest with a pick already made would otherwise post an id from the
    // wrong list, which the check constraint would reject with a confusing message.
    [Fact]
    public async Task SwitchingKind_DropsThePreviousSelection()
    {
        var (vm, _, _) = Build(members: [M("Alex")], externals: [E("Björn")]);
        await vm.InitialiseAsync(CompetitionId);

        vm.Selected = vm.Options[0];
        Assert.True(vm.CanSave);

        vm.Kind = ParticipantKind.External;

        Assert.Null(vm.Selected);
        Assert.False(vm.CanSave);
        Assert.Equal("Björn Guest", vm.Options[0].Name);
    }

    [Fact]
    public async Task AlreadyRegisteredArchersAreNotOffered()
    {
        var registeredMember = M("Taken");
        var freeMember = M("Free");
        var registeredGuest = E("TakenGuest");
        var freeGuest = E("FreeGuest");

        var (vm, _, _) = Build(
            members: [registeredMember, freeMember],
            externals: [registeredGuest, freeGuest],
            registered:
            [
                new CompetitionParticipant { Id = Guid.NewGuid(), CompetitionId = CompetitionId, MemberId = registeredMember.Id },
                new CompetitionParticipant { Id = Guid.NewGuid(), CompetitionId = CompetitionId, ExternalParticipantId = registeredGuest.Id }
            ]);

        await vm.InitialiseAsync(CompetitionId);

        Assert.Single(vm.Options);
        Assert.Equal("Free Archer", vm.Options[0].Name);

        vm.Kind = ParticipantKind.External;
        Assert.Single(vm.Options);
        Assert.Equal("FreeGuest Guest", vm.Options[0].Name);
    }

    [Fact]
    public async Task InactiveMembersAreNotOffered()
    {
        var (vm, _, _) = Build(members: [M("Active"), M("Inactive", active: false)]);

        await vm.InitialiseAsync(CompetitionId);

        Assert.Single(vm.Options);
        Assert.Equal("Active Archer", vm.Options[0].Name);
    }

    [Fact]
    public async Task Save_SurfacesADuplicateRegistrationFromTheServer()
    {
        var (vm, api, nav) = Build(members: [M("Alex")]);
        await vm.InitialiseAsync(CompetitionId);
        vm.Selected = vm.Options[0];

        api.MutationStatus = HttpStatusCode.BadRequest;
        api.MutationBody = """{"errors":["This participant is already registered for the competition."]}""";

        await vm.SaveCommand.ExecuteAsync(null);

        Assert.Equal("This participant is already registered for the competition.", vm.ErrorMessage);
        Assert.Equal(0, nav.BackCount);
    }
}

public class ResultEditViewModelTests
{
    static readonly Guid CompetitionId = Guid.NewGuid();

    static Member M(string name) => new()
    {
        Id = Guid.NewGuid(),
        FirstName = name,
        LastName = "Archer"
    };

    static (ResultEditViewModel Vm, FakeApiClient Api, FakeNavigationService Nav) Build(
        Member[]? members = null, ExternalParticipant[]? externals = null,
        CompetitionParticipant[]? participants = null, CompetitionResult[]? results = null)
    {
        var api = new FakeApiClient
        {
            Members = [.. members ?? []],
            Externals = [.. externals ?? []],
            Participants = [.. participants ?? []],
            Results = [.. results ?? []]
        };
        var nav = new FakeNavigationService();
        return (new ResultEditViewModel(api, nav, TestLogger.For<ResultEditViewModel>()), api, nav);
    }

    [Fact]
    public async Task NewResult_SetsOnlyOneParticipantId()
    {
        var alex = M("Alex");
        var (vm, api, _) = Build(members: [alex]);
        await vm.InitialiseAsync(CompetitionId, null, null);

        vm.Selected = vm.Options.First(o => o.Id == alex.Id);
        vm.TotalScore = "560";
        vm.XCount = "12";

        await vm.SaveCommand.ExecuteAsync(null);

        Assert.Equal(alex.Id, api.LastResult?.MemberId);
        Assert.Null(api.LastResult?.ExternalParticipantId);
        Assert.Equal(560, api.LastResult?.TotalScore);
        Assert.Equal(12, api.LastResult?.XCount);
    }

    // Opening from a registration should carry the classes the archer actually signed on under,
    // rather than making the scorer retype them and risk a mismatch.
    [Fact]
    public async Task PrefillFromParticipant_CarriesClassesAndArcher()
    {
        var alex = M("Alex");
        var participant = new CompetitionParticipant
        {
            Id = Guid.NewGuid(),
            CompetitionId = CompetitionId,
            MemberId = alex.Id,
            BowClass = BowClass.Compound,
            AgeClass = AgeClass.Junior,
            Gender = Gender.Female
        };
        var (vm, _, _) = Build(members: [alex], participants: [participant]);

        await vm.InitialiseAsync(CompetitionId, null, participant.Id);

        Assert.Equal(alex.Id, vm.Selected?.Id);
        Assert.Equal(BowClass.Compound, vm.BowClass);
        Assert.Equal(AgeClass.Junior, vm.AgeClass);
        Assert.Equal(Gender.Female, vm.Gender);
        Assert.True(vm.IsMember);
    }

    [Fact]
    public async Task EditingAnExistingResult_LoadsItAndLocksTheArcher()
    {
        var alex = M("Alex");
        var result = new CompetitionResult
        {
            Id = Guid.NewGuid(),
            CompetitionId = CompetitionId,
            MemberId = alex.Id,
            BowClass = BowClass.Barebow,
            TotalScore = 480,
            XCount = 6,
            Placement = 3,
            IsDisqualified = false,
            Notes = "Windy"
        };
        var (vm, api, _) = Build(members: [alex], results: [result]);

        await vm.InitialiseAsync(CompetitionId, result.Id, null);

        Assert.Equal("480", vm.TotalScore);
        Assert.Equal("3", vm.Placement);
        Assert.Equal("Windy", vm.Notes);
        // Reassigning a recorded result to a different archer is a different operation, and
        // would risk colliding with that archer's own result.
        Assert.False(vm.CanChooseParticipant);

        vm.TotalScore = "500";
        await vm.SaveCommand.ExecuteAsync(null);

        Assert.Contains($"UpdateResult:{result.Id}", api.Calls);
        Assert.Equal(500, api.LastResult?.TotalScore);
    }

    [Theory]
    [InlineData("", "0")]
    [InlineData("abc", "0")]
    [InlineData("-5", "0")]
    public async Task RejectsANonNumericOrNegativeScore(string score, string xCount)
    {
        var (vm, _, _) = Build(members: [M("Alex")]);
        await vm.InitialiseAsync(CompetitionId, null, null);
        vm.Selected = vm.Options[0];

        vm.TotalScore = score;
        vm.XCount = xCount;

        Assert.True(vm.HasScoreError);
        Assert.False(vm.CanSave);
    }

    [Fact]
    public async Task RejectsMoreXsThanPoints()
    {
        var (vm, _, _) = Build(members: [M("Alex")]);
        await vm.InitialiseAsync(CompetitionId, null, null);
        vm.Selected = vm.Options[0];

        vm.TotalScore = "100";
        vm.XCount = "101";

        Assert.Equal("X count cannot be greater than the total score.", vm.ScoreError);
        Assert.False(vm.CanSave);
    }

    [Fact]
    public async Task PlacementMayBeBlankButNotZero()
    {
        var (vm, api, _) = Build(members: [M("Alex")]);
        await vm.InitialiseAsync(CompetitionId, null, null);
        vm.Selected = vm.Options[0];
        vm.TotalScore = "500";

        vm.Placement = "0";
        Assert.False(vm.CanSave);

        vm.Placement = string.Empty;
        Assert.True(vm.CanSave);

        await vm.SaveCommand.ExecuteAsync(null);
        Assert.Null(api.LastResult?.Placement);
    }

    // The app runs under sv-SE; an Entry bound straight to an int would silently drop input the
    // current culture cannot parse, so parsing is explicit and tolerant of both separators.
    [Theory]
    [InlineData("sv-SE")]
    [InlineData("en-US")]
    [InlineData("de-DE")]
    public async Task ScoresParseUnderAnyCulture(string cultureName)
    {
        var previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo(cultureName);
        try
        {
            var (vm, api, _) = Build(members: [M("Alex")]);
            await vm.InitialiseAsync(CompetitionId, null, null);
            vm.Selected = vm.Options[0];
            vm.TotalScore = "560";
            vm.XCount = "12";

            await vm.SaveCommand.ExecuteAsync(null);

            Assert.Equal(560, api.LastResult?.TotalScore);
            Assert.Equal(12, api.LastResult?.XCount);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public async Task DisqualificationAndNotesRoundTrip()
    {
        var (vm, api, _) = Build(members: [M("Alex")]);
        await vm.InitialiseAsync(CompetitionId, null, null);
        vm.Selected = vm.Options[0];
        vm.TotalScore = "0";
        vm.IsDisqualified = true;
        vm.Notes = "  Equipment check failed  ";

        await vm.SaveCommand.ExecuteAsync(null);

        Assert.True(api.LastResult?.IsDisqualified);
        Assert.Equal("Equipment check failed", api.LastResult?.Notes);
    }
}
