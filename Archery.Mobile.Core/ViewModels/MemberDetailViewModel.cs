using System.Collections.ObjectModel;
using Archery.Mobile.Core.Abstractions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace Archery.Mobile.Core.ViewModels;

public sealed partial class MemberDetailViewModel(
    IArcheryApiClient api,
    INavigationService navigation,
    IDialogService dialogs,
    ILogger<MemberDetailViewModel> logger) : BaseViewModel(logger)
{
    Guid _memberId;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasMember))]
    public partial Member? Member { get; set; }

    /// <summary>That member's fees, newest year first.</summary>
    public ObservableCollection<MembershipFee> Fees { get; } = [];

    [ObservableProperty]
    public partial bool HasFees { get; set; }

    public bool HasMember => Member is not null;

    public async Task InitialiseAsync(Guid memberId, CancellationToken ct = default)
    {
        _memberId = memberId;
        await LoadAsync(ct);
    }

    [RelayCommand]
    async Task LoadAsync(CancellationToken ct)
    {
        await RunAsync(async token =>
        {
            Member = await api.GetMemberAsync(_memberId, token);
            Title = Member?.FullName ?? "Member";

            var fees = await api.GetFeesByMemberAsync(_memberId, token) ?? [];
            Fees.Clear();
            foreach (var fee in fees.OrderByDescending(f => f.Year))
                Fees.Add(fee);

            HasFees = Fees.Count > 0;
        }, ct);
    }

    [RelayCommand]
    async Task RefreshAsync(CancellationToken ct)
    {
        IsRefreshing = true;
        await LoadAsync(ct);
    }

    [RelayCommand]
    Task EditAsync() => navigation.GoToAsync(Routes.MemberEdit,
        new Dictionary<string, object> { [Routes.MemberIdKey] = _memberId });

    /// <summary>
    /// Marks a fee paid, mirroring the web app's one-tap action: today's date and status Paid.
    /// </summary>
    [RelayCommand]
    async Task MarkPaidAsync(MembershipFee? fee, CancellationToken ct)
    {
        if (fee is null || fee.Status == FeeStatus.Paid)
            return;

        if (!await dialogs.ConfirmAsync("Mark paid",
                $"Mark the {fee.Year} fee as paid?", "Mark paid", "Cancel"))
        {
            return;
        }

        var updated = new MembershipFee
        {
            Id = fee.Id,
            MemberId = fee.MemberId,
            Year = fee.Year,
            Amount = fee.Amount,
            DueDate = fee.DueDate,
            PaidDate = DateOnly.FromDateTime(DateTime.Today),
            Status = FeeStatus.Paid
        };

        var ok = await RunAsync(
            token => api.UpdateFeeAsync(fee.Id, updated, token).EnsureArcherySuccessAsync(token), ct);

        if (ok)
            await LoadAsync(ct);
    }
}
