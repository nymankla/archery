namespace Archery.Mobile.Core.Tests;

public class MemberDetailViewModelTests
{
    static (MemberDetailViewModel Vm, FakeApiClient Api, FakeNavigationService Nav, FakeDialogService Dialogs)
        Build(Member member, params MembershipFee[] fees)
    {
        var api = new FakeApiClient { Members = [member], Fees = [.. fees] };
        var nav = new FakeNavigationService();
        var dialogs = new FakeDialogService();
        return (new MemberDetailViewModel(api, nav, dialogs, TestLogger.For<MemberDetailViewModel>()),
            api, nav, dialogs);
    }

    static Member Member(Guid id) => new()
    {
        Id = id,
        FirstName = "Alex",
        LastName = "Anderson",
        DateOfBirth = new DateOnly(1990, 5, 17),
        JoinDate = new DateOnly(2020, 1, 2),
        IsActive = true
    };

    static MembershipFee Fee(Guid memberId, int year, FeeStatus status = FeeStatus.Unpaid) => new()
    {
        Id = Guid.NewGuid(),
        MemberId = memberId,
        Year = year,
        Amount = 500,
        DueDate = new DateOnly(year, 3, 31),
        Status = status
    };

    [Fact]
    public async Task Load_FetchesMemberAndFees()
    {
        var id = Guid.NewGuid();
        var (vm, _, _, _) = Build(Member(id), Fee(id, 2025), Fee(id, 2026));

        await vm.InitialiseAsync(id);

        Assert.True(vm.HasMember);
        Assert.Equal("Alex Anderson", vm.Title);
        Assert.Equal(2, vm.Fees.Count);
        Assert.True(vm.HasFees);
    }

    [Fact]
    public async Task Fees_AreNewestYearFirst()
    {
        var id = Guid.NewGuid();
        var (vm, _, _, _) = Build(Member(id), Fee(id, 2024), Fee(id, 2026), Fee(id, 2025));

        await vm.InitialiseAsync(id);

        Assert.Equal([2026, 2025, 2024], vm.Fees.Select(f => f.Year));
    }

    [Fact]
    public async Task HasFees_IsFalseWhenThereAreNone()
    {
        var id = Guid.NewGuid();
        var (vm, _, _, _) = Build(Member(id));

        await vm.InitialiseAsync(id);

        Assert.False(vm.HasFees);
        Assert.Empty(vm.Fees);
    }

    [Fact]
    public async Task MarkPaid_SetsTodayAndPaidStatus()
    {
        var id = Guid.NewGuid();
        var (vm, api, _, _) = Build(Member(id), Fee(id, 2026));
        await vm.InitialiseAsync(id);
        var fee = vm.Fees[0];

        await vm.MarkPaidCommand.ExecuteAsync(fee);

        Assert.Equal(FeeStatus.Paid, api.LastFeeUpdate?.Status);
        Assert.Equal(DateOnly.FromDateTime(DateTime.Today), api.LastFeeUpdate?.PaidDate);
        // The rest of the fee must survive the round trip untouched.
        Assert.Equal(2026, api.LastFeeUpdate?.Year);
        Assert.Equal(500, api.LastFeeUpdate?.Amount);
        Assert.Equal(new DateOnly(2026, 3, 31), api.LastFeeUpdate?.DueDate);
    }

    [Fact]
    public async Task MarkPaid_AsksFirst()
    {
        var id = Guid.NewGuid();
        var (vm, api, _, dialogs) = Build(Member(id), Fee(id, 2026));
        await vm.InitialiseAsync(id);
        dialogs.ConfirmResult = false;

        await vm.MarkPaidCommand.ExecuteAsync(vm.Fees[0]);

        Assert.Single(dialogs.Confirmations);
        Assert.Null(api.LastFeeUpdate);
    }

    [Fact]
    public async Task MarkPaid_IgnoresAFeeThatIsAlreadyPaid()
    {
        var id = Guid.NewGuid();
        var (vm, api, _, dialogs) = Build(Member(id), Fee(id, 2026, FeeStatus.Paid));
        await vm.InitialiseAsync(id);

        await vm.MarkPaidCommand.ExecuteAsync(vm.Fees[0]);

        Assert.Empty(dialogs.Confirmations);
        Assert.Null(api.LastFeeUpdate);
    }

    [Fact]
    public async Task Edit_NavigatesWithTheMemberId()
    {
        var id = Guid.NewGuid();
        var (vm, _, nav, _) = Build(Member(id));
        await vm.InitialiseAsync(id);

        await vm.EditCommand.ExecuteAsync(null);

        Assert.Equal(Routes.MemberEdit, nav.Routes[^1]);
        Assert.Equal(id, nav.LastParameters?[Routes.MemberIdKey]);
    }
}
