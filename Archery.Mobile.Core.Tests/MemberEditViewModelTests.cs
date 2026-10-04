using System.Net;

namespace Archery.Mobile.Core.Tests;

public class MemberEditViewModelTests
{
    static (MemberEditViewModel Vm, FakeApiClient Api, FakeNavigationService Nav) Build(
        params Member[] members)
    {
        var api = new FakeApiClient { Members = [.. members] };
        var nav = new FakeNavigationService();
        return (new MemberEditViewModel(api, nav, TestLogger.For<MemberEditViewModel>()), api, nav);
    }

    [Fact]
    public async Task NewMember_DefaultsBothDatesToToday()
    {
        var (vm, _, _) = Build();

        await vm.InitialiseAsync(null);

        Assert.True(vm.IsNew);
        Assert.Equal(DateTime.Today, vm.DateOfBirth.Date);
        Assert.Equal(DateTime.Today, vm.JoinDate.Date);
        Assert.True(vm.IsActive);
    }

    [Fact]
    public async Task CannotSaveWithoutBothNames()
    {
        var (vm, _, _) = Build();
        await vm.InitialiseAsync(null);

        Assert.False(vm.CanSave);

        vm.FirstName = "Alex";
        Assert.False(vm.CanSave);

        vm.LastName = "Anderson";
        Assert.True(vm.CanSave);

        // Whitespace is not a name.
        vm.LastName = "   ";
        Assert.False(vm.CanSave);
    }

    [Fact]
    public async Task RejectsAFutureDateOfBirth()
    {
        var (vm, _, _) = Build();
        await vm.InitialiseAsync(null);
        vm.FirstName = "Alex";
        vm.LastName = "Anderson";

        vm.DateOfBirth = DateTime.Today.AddDays(1);

        Assert.True(vm.HasDateError);
        Assert.False(vm.CanSave);
        Assert.Equal("Date of birth cannot be in the future.", vm.DateError);
    }

    [Fact]
    public async Task RejectsAJoinDateBeforeBirth()
    {
        var (vm, _, _) = Build();
        await vm.InitialiseAsync(null);
        vm.FirstName = "Alex";
        vm.LastName = "Anderson";

        vm.DateOfBirth = new DateTime(2000, 1, 1);
        vm.JoinDate = new DateTime(1999, 1, 1);

        Assert.Equal("Join date cannot be before the date of birth.", vm.DateError);
        Assert.False(vm.CanSave);
    }

    [Fact]
    public async Task Save_CreatesWhenNewAndGoesBack()
    {
        var (vm, api, nav) = Build();
        await vm.InitialiseAsync(null);
        vm.FirstName = "Alex";
        vm.LastName = "Anderson";
        vm.PreferredBowClass = BowClass.Barebow;

        await vm.SaveCommand.ExecuteAsync(null);

        Assert.Contains("CreateMember", api.Calls);
        Assert.Equal("Alex", api.LastCreated?.FirstName);
        Assert.Equal(BowClass.Barebow, api.LastCreated?.PreferredBowClass);
        Assert.Equal(1, nav.BackCount);
    }

    [Fact]
    public async Task Save_UpdatesWhenEditingAnExistingMember()
    {
        var id = Guid.NewGuid();
        var (vm, api, nav) = Build(new Member
        {
            Id = id,
            FirstName = "Alex",
            LastName = "Anderson",
            DateOfBirth = new DateOnly(1990, 5, 17),
            JoinDate = new DateOnly(2020, 1, 2),
            PreferredBowClass = BowClass.Compound,
            IsActive = true
        });

        await vm.InitialiseAsync(id);
        Assert.False(vm.IsNew);
        Assert.Equal("Alex", vm.FirstName);
        Assert.Equal(BowClass.Compound, vm.PreferredBowClass);

        vm.LastName = "Andersson";
        await vm.SaveCommand.ExecuteAsync(null);

        Assert.Contains($"UpdateMember:{id}", api.Calls);
        Assert.Equal("Andersson", api.LastUpdated?.LastName);
        Assert.Equal(1, nav.BackCount);
    }

    [Fact]
    public async Task Save_TrimsNamesAndSendsBlankOptionalFieldsAsNull()
    {
        var (vm, api, _) = Build();
        await vm.InitialiseAsync(null);
        vm.FirstName = "  Alex  ";
        vm.LastName = "  Anderson ";
        vm.Email = "   ";
        vm.Phone = "  070 123 45 67 ";

        await vm.SaveCommand.ExecuteAsync(null);

        Assert.Equal("Alex", api.LastCreated?.FirstName);
        Assert.Equal("Anderson", api.LastCreated?.LastName);
        Assert.Null(api.LastCreated?.Email);
        Assert.Equal("070 123 45 67", api.LastCreated?.Phone);
    }

    // The personnummer checksum is the server's business; the client only surfaces its verdict.
    [Fact]
    public async Task Save_ShowsTheServersValidationMessageAndStaysOnThePage()
    {
        var (vm, api, nav) = Build();
        await vm.InitialiseAsync(null);
        vm.FirstName = "Alex";
        vm.LastName = "Anderson";
        vm.Personnummer = "19900517-0000";

        api.MutationStatus = HttpStatusCode.BadRequest;
        api.MutationBody = """{"errors":["A member with this personnummer already exists."]}""";

        await vm.SaveCommand.ExecuteAsync(null);

        Assert.Equal("A member with this personnummer already exists.", vm.ErrorMessage);
        Assert.Equal(0, nav.BackCount);
    }

    [Fact]
    public async Task Save_DoesNothingWhenValidationFails()
    {
        var (vm, api, nav) = Build();
        await vm.InitialiseAsync(null);
        // No names set.

        await vm.SaveCommand.ExecuteAsync(null);

        Assert.DoesNotContain("CreateMember", api.Calls);
        Assert.Equal(0, nav.BackCount);
    }

    [Fact]
    public async Task Cancel_GoesBackWithoutSaving()
    {
        var (vm, api, nav) = Build();
        await vm.InitialiseAsync(null);

        await vm.CancelCommand.ExecuteAsync(null);

        Assert.Empty(api.Calls);
        Assert.Equal(1, nav.BackCount);
    }
}
