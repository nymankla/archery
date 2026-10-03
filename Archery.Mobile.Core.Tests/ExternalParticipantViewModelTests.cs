using System.Net;

namespace Archery.Mobile.Core.Tests;

public class ExternalParticipantsViewModelTests
{
    static ExternalParticipant E(string first, string last, string? club = null, string? email = null) => new()
    {
        Id = Guid.NewGuid(),
        FirstName = first,
        LastName = last,
        ClubAffiliation = club,
        Email = email
    };

    static (ExternalParticipantsViewModel Vm, FakeApiClient Api, FakeNavigationService Nav, FakeDialogService Dialogs)
        Build(params ExternalParticipant[] participants)
    {
        var api = new FakeApiClient { Externals = [.. participants] };
        var nav = new FakeNavigationService();
        var dialogs = new FakeDialogService();
        return (new ExternalParticipantsViewModel(api, nav, dialogs,
            TestLogger.For<ExternalParticipantsViewModel>()), api, nav, dialogs);
    }

    [Fact]
    public async Task Load_SortsByName()
    {
        var (vm, _, _, _) = Build(E("Erik", "Johansson"), E("Anna", "Lindqvist"));

        await vm.OnAppearingAsync();

        Assert.Equal(["Anna", "Erik"], vm.Participants.Select(p => p.FirstName));
        Assert.Equal("2 guests", vm.CountSummary);
    }

    // There is no club entity, so the filter list has to be derived from whatever the guests
    // actually carry.
    [Fact]
    public async Task ClubList_IsBuiltFromTheDataWithAnAllEntryFirst()
    {
        var (vm, _, _, _) = Build(
            E("Anna", "Lindqvist", "Stockholms BK"),
            E("Erik", "Johansson", "Göteborgs BK"),
            E("Björn", "Eriksson"),
            E("Sofia", "Nilsson", "Stockholms BK"));

        await vm.OnAppearingAsync();

        // null is the "all clubs" entry; duplicates and blanks are dropped.
        Assert.Equal([null, "Göteborgs BK", "Stockholms BK"], vm.Clubs);
    }

    [Fact]
    public async Task ClubFilter_NarrowsAndClears()
    {
        var (vm, _, _, _) = Build(
            E("Anna", "Lindqvist", "Stockholms BK"),
            E("Erik", "Johansson", "Göteborgs BK"));

        await vm.OnAppearingAsync();

        vm.ClubFilter = "Stockholms BK";
        Assert.Single(vm.Participants);
        Assert.Equal("1 of 2 guests", vm.CountSummary);

        vm.ClubFilter = null;
        Assert.Equal(2, vm.Participants.Count);
    }

    // Deleting the last guest from a club removes that club from the picker; leaving the filter
    // pointing at it would show an empty list with no obvious way back.
    [Fact]
    public async Task ReloadingDropsAClubFilterThatNoLongerExists()
    {
        var onlyGuestInClub = E("Anna", "Lindqvist", "Stockholms BK");
        var (vm, api, _, _) = Build(onlyGuestInClub, E("Erik", "Johansson", "Göteborgs BK"));
        await vm.OnAppearingAsync();

        vm.ClubFilter = "Stockholms BK";
        Assert.Single(vm.Participants);

        api.Externals.Remove(onlyGuestInClub);
        await vm.LoadCommand.ExecuteAsync(null);

        Assert.Null(vm.ClubFilter);
        Assert.Single(vm.Participants);
    }

    [Fact]
    public async Task ReloadingKeepsAClubFilterThatStillExists()
    {
        var (vm, _, _, _) = Build(
            E("Anna", "Lindqvist", "Stockholms BK"),
            E("Sofia", "Nilsson", "Stockholms BK"));
        await vm.OnAppearingAsync();

        vm.ClubFilter = "Stockholms BK";
        await vm.LoadCommand.ExecuteAsync(null);

        Assert.Equal("Stockholms BK", vm.ClubFilter);
    }

    [Fact]
    public async Task Search_MatchesNameEmailAndClub()
    {
        var (vm, _, _, _) = Build(
            E("Anna", "Lindqvist", "Stockholms BK", "anna@example.com"),
            E("Erik", "Johansson", "Göteborgs BK", "erik@example.com"));

        await vm.OnAppearingAsync();

        vm.SearchText = "lindq";
        Assert.Single(vm.Participants);

        vm.SearchText = "erik@";
        Assert.Single(vm.Participants);

        vm.SearchText = "göteborg";
        Assert.Single(vm.Participants);
        Assert.Equal("Erik", vm.Participants[0].FirstName);
    }

    [Fact]
    public async Task Delete_ConfirmsThenRemoves()
    {
        var (vm, api, _, dialogs) = Build(E("Anna", "Lindqvist"));
        await vm.OnAppearingAsync();

        await vm.DeleteCommand.ExecuteAsync(vm.Participants[0]);

        Assert.Single(dialogs.Confirmations);
        Assert.Contains(api.Calls, c => c.StartsWith("DeleteExternalParticipant"));
        Assert.Empty(vm.Participants);
    }

    [Fact]
    public async Task Delete_KeepsTheRowWhenTheServerRefuses()
    {
        var (vm, api, _, _) = Build(E("Anna", "Lindqvist"));
        await vm.OnAppearingAsync();

        api.MutationStatus = HttpStatusCode.BadRequest;
        api.MutationBody = """{"errors":["Guest has competition results and cannot be deleted."]}""";

        await vm.DeleteCommand.ExecuteAsync(vm.Participants[0]);

        Assert.Single(vm.Participants);
        Assert.Equal("Guest has competition results and cannot be deleted.", vm.ErrorMessage);
    }

    [Fact]
    public async Task Open_NavigatesWithTheGuestId()
    {
        var (vm, _, nav, _) = Build(E("Anna", "Lindqvist"));
        await vm.OnAppearingAsync();
        var target = vm.Participants[0];

        await vm.OpenCommand.ExecuteAsync(target);

        Assert.Equal(Routes.ExternalParticipantEdit, nav.Routes[^1]);
        Assert.Equal(target.Id, nav.LastParameters?[Routes.ExternalParticipantIdKey]);
    }
}

public class ExternalParticipantEditViewModelTests
{
    static (ExternalParticipantEditViewModel Vm, FakeApiClient Api, FakeNavigationService Nav) Build(
        params ExternalParticipant[] participants)
    {
        var api = new FakeApiClient { Externals = [.. participants] };
        var nav = new FakeNavigationService();
        return (new ExternalParticipantEditViewModel(api, nav,
            TestLogger.For<ExternalParticipantEditViewModel>()), api, nav);
    }

    [Fact]
    public async Task RequiresBothNames()
    {
        var (vm, _, _) = Build();
        await vm.InitialiseAsync(null);

        Assert.True(vm.IsNew);
        Assert.False(vm.CanSave);

        vm.FirstName = "Anna";
        Assert.False(vm.CanSave);

        vm.LastName = "Lindqvist";
        Assert.True(vm.CanSave);
    }

    [Fact]
    public async Task Save_TrimsAndNullsBlankOptionalFields()
    {
        var (vm, api, nav) = Build();
        await vm.InitialiseAsync(null);
        vm.FirstName = "  Anna ";
        vm.LastName = " Lindqvist ";
        vm.ClubAffiliation = "  Stockholms BK ";
        vm.Email = "  ";

        await vm.SaveCommand.ExecuteAsync(null);

        var created = api.LastExternal;
        Assert.Equal("Anna", created?.FirstName);
        Assert.Equal("Lindqvist", created?.LastName);
        Assert.Equal("Stockholms BK", created?.ClubAffiliation);
        Assert.Null(created?.Email);
        Assert.Equal(1, nav.BackCount);
    }

    [Fact]
    public async Task Editing_LoadsFromTheListThenUpdates()
    {
        var existing = new ExternalParticipant
        {
            Id = Guid.NewGuid(),
            FirstName = "Anna",
            LastName = "Lindqvist",
            ClubAffiliation = "Stockholms BK",
            Email = "anna@example.com"
        };
        var (vm, api, _) = Build(existing);

        await vm.InitialiseAsync(existing.Id);

        Assert.False(vm.IsNew);
        Assert.Equal("Anna", vm.FirstName);
        Assert.Equal("Stockholms BK", vm.ClubAffiliation);

        vm.ClubAffiliation = "Uppsala BK";
        await vm.SaveCommand.ExecuteAsync(null);

        Assert.Contains($"UpdateExternalParticipant:{existing.Id}", api.Calls);
        Assert.Equal("Uppsala BK", api.LastExternal?.ClubAffiliation);
    }

    [Fact]
    public async Task Save_ShowsTheServersMessageAndStaysPut()
    {
        var (vm, api, nav) = Build();
        await vm.InitialiseAsync(null);
        vm.FirstName = "Anna";
        vm.LastName = "Lindqvist";

        api.MutationStatus = HttpStatusCode.BadRequest;
        api.MutationBody = """{"errors":["A guest with this email already exists."]}""";

        await vm.SaveCommand.ExecuteAsync(null);

        Assert.Equal("A guest with this email already exists.", vm.ErrorMessage);
        Assert.Equal(0, nav.BackCount);
    }
}
