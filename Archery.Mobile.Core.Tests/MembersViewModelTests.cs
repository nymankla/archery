using System.Net;

namespace Archery.Mobile.Core.Tests;

public class MembersViewModelTests
{
    static Member M(string first, string last, bool active = true,
        BowClass bow = BowClass.Recurve, string? email = null, int joinYear = 2020) =>
        new()
        {
            Id = Guid.NewGuid(),
            FirstName = first,
            LastName = last,
            IsActive = active,
            PreferredBowClass = bow,
            Email = email,
            JoinDate = new DateOnly(joinYear, 1, 1),
            DateOfBirth = new DateOnly(1990, 1, 1)
        };

    static (MembersViewModel Vm, FakeApiClient Api, FakeNavigationService Nav, FakeDialogService Dialogs, FakeAuthService Auth)
        Build(params Member[] members)
    {
        var api = new FakeApiClient { Members = [.. members] };
        var nav = new FakeNavigationService();
        var dialogs = new FakeDialogService();
        var auth = new FakeAuthService();
        return (new MembersViewModel(api, auth, nav, dialogs, TestLogger.For<MembersViewModel>()),
            api, nav, dialogs, auth);
    }

    [Fact]
    public async Task Load_PopulatesMembers()
    {
        var (vm, _, _, _, _) = Build(M("Alex", "Anderson"), M("Bo", "Berg"));

        await vm.OnAppearingAsync();

        Assert.Equal(2, vm.Members.Count);
        Assert.False(vm.HasError);
        Assert.False(vm.IsBusy);
    }

    [Fact]
    public async Task ActiveOnly_IsOnByDefaultAndFiltersInactive()
    {
        var (vm, _, _, _, _) = Build(M("Alex", "Anderson"), M("Bo", "Berg", active: false));

        await vm.OnAppearingAsync();

        Assert.True(vm.ActiveOnly);
        Assert.Single(vm.Members);

        vm.ActiveOnly = false;
        Assert.Equal(2, vm.Members.Count);
    }

    [Fact]
    public async Task Search_MatchesNameAndEmailCaseInsensitively()
    {
        var (vm, _, _, _, _) = Build(
            M("Alex", "Anderson", email: "alex@example.com"),
            M("Bo", "Berg", email: "bo@example.com"));

        await vm.OnAppearingAsync();

        vm.SearchText = "ANDER";
        Assert.Single(vm.Members);

        vm.SearchText = "bo@EXAMPLE";
        Assert.Single(vm.Members);
        Assert.Equal("Bo", vm.Members[0].FirstName);
    }

    [Fact]
    public async Task BowClassFilter_NarrowsAndClears()
    {
        var (vm, _, _, _, _) = Build(
            M("Alex", "Anderson", bow: BowClass.Recurve),
            M("Bo", "Berg", bow: BowClass.Compound));

        await vm.OnAppearingAsync();

        vm.BowClassFilter = BowClass.Compound;
        Assert.Single(vm.Members);

        // null is the "all bow classes" entry in the picker.
        vm.BowClassFilter = null;
        Assert.Equal(2, vm.Members.Count);
    }

    [Fact]
    public async Task Sort_OrdersByJoinDate()
    {
        var (vm, _, _, _, _) = Build(
            M("Old", "Member", joinYear: 2010),
            M("New", "Member", joinYear: 2024));

        await vm.OnAppearingAsync();

        vm.Sort = MemberSort.JoinDateNewest;
        Assert.Equal("New", vm.Members[0].FirstName);

        vm.Sort = MemberSort.JoinDateOldest;
        Assert.Equal("Old", vm.Members[0].FirstName);
    }

    [Fact]
    public async Task CountSummary_ReportsFilteringOnlyWhenItNarrows()
    {
        var (vm, _, _, _, _) = Build(M("Alex", "Anderson"), M("Bo", "Berg"));

        await vm.OnAppearingAsync();
        Assert.Equal("2 members", vm.CountSummary);

        vm.SearchText = "Alex";
        Assert.Equal("1 of 2 members", vm.CountSummary);
    }

    [Fact]
    public async Task Delete_AsksFirstAndRemovesOnConfirm()
    {
        var (vm, api, _, dialogs, _) = Build(M("Alex", "Anderson"), M("Bo", "Berg"));
        await vm.OnAppearingAsync();
        var target = vm.Members[0];

        await vm.DeleteCommand.ExecuteAsync(target);

        Assert.Single(dialogs.Confirmations);
        Assert.Contains(api.Calls, c => c == $"DeleteMember:{target.Id}");
        Assert.Single(vm.Members);
    }

    [Fact]
    public async Task Delete_DoesNothingWhenCancelled()
    {
        var (vm, api, _, dialogs, _) = Build(M("Alex", "Anderson"));
        await vm.OnAppearingAsync();
        dialogs.ConfirmResult = false;

        await vm.DeleteCommand.ExecuteAsync(vm.Members[0]);

        Assert.DoesNotContain(api.Calls, c => c.StartsWith("DeleteMember"));
        Assert.Single(vm.Members);
    }

    [Fact]
    public async Task Delete_KeepsTheRowWhenTheServerRejectsIt()
    {
        var (vm, api, _, _, _) = Build(M("Alex", "Anderson"));
        await vm.OnAppearingAsync();

        api.MutationStatus = HttpStatusCode.BadRequest;
        api.MutationBody = """{"errors":["Member has results and cannot be deleted."]}""";

        await vm.DeleteCommand.ExecuteAsync(vm.Members[0]);

        Assert.Single(vm.Members);
        Assert.Equal("Member has results and cannot be deleted.", vm.ErrorMessage);
    }

    [Fact]
    public async Task LoadFailure_SetsErrorAndLeavesTheListIntact()
    {
        var (vm, api, _, _, _) = Build(M("Alex", "Anderson"));
        await vm.OnAppearingAsync();
        Assert.Single(vm.Members);

        api.Throws = new HttpRequestException("no route to host");
        await vm.LoadCommand.ExecuteAsync(null);

        Assert.True(vm.HasError);
        Assert.Equal("Cannot reach the server. Check your connection and try again.", vm.ErrorMessage);
        Assert.Single(vm.Members);
    }

    [Fact]
    public async Task UnauthorizedFailure_AsksTheUserToSignInAgain()
    {
        var (vm, api, _, _, _) = Build();
        api.Throws = new ArcheryApiException(new ApiError(401, ["nope"]));

        await vm.LoadCommand.ExecuteAsync(null);

        Assert.Equal("Your session has expired. Please sign in again.", vm.ErrorMessage);
    }

    [Fact]
    public async Task Open_NavigatesToDetailWithTheMemberId()
    {
        var (vm, _, nav, _, _) = Build(M("Alex", "Anderson"));
        await vm.OnAppearingAsync();
        var target = vm.Members[0];

        await vm.OpenCommand.ExecuteAsync(target);

        Assert.Equal(Routes.MemberDetail, nav.Routes[^1]);
        Assert.Equal(target.Id, nav.LastParameters?[Routes.MemberIdKey]);
    }

    [Fact]
    public async Task SignOut_ConfirmsThenClearsAndReturnsToLogin()
    {
        var (vm, _, nav, dialogs, auth) = Build(M("Alex", "Anderson"));
        await vm.OnAppearingAsync();

        await vm.SignOutCommand.ExecuteAsync(null);

        Assert.Single(dialogs.Confirmations);
        Assert.Equal(1, auth.SignOutCount);
        Assert.Empty(vm.Members);
        Assert.Equal(Routes.Login, nav.Routes[^1]);
    }

    [Fact]
    public async Task SignOut_CancelledLeavesTheSessionAlone()
    {
        var (vm, _, _, dialogs, auth) = Build(M("Alex", "Anderson"));
        await vm.OnAppearingAsync();
        dialogs.ConfirmResult = false;

        await vm.SignOutCommand.ExecuteAsync(null);

        Assert.Equal(0, auth.SignOutCount);
        Assert.Single(vm.Members);
    }
}
