using System.Collections.ObjectModel;
using Archery.Mobile.Core.Abstractions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace Archery.Mobile.Core.ViewModels;

public sealed partial class MembersViewModel(
    IArcheryApiClient api,
    IAuthService auth,
    INavigationService navigation,
    IDialogService dialogs,
    ILogger<MembersViewModel> logger) : BaseViewModel(logger)
{
    /// <summary>Everything fetched, before filtering; <see cref="Members"/> is the visible slice.</summary>
    readonly List<Member> _all = [];

    public ObservableCollection<Member> Members { get; } = [];

    public IReadOnlyList<MemberSort> SortOptions { get; } = Enum.GetValues<MemberSort>();

    /// <summary>Includes a null entry for "all", so the picker can clear the filter.</summary>
    public IReadOnlyList<BowClass?> BowClassOptions { get; } =
        [null, .. Enum.GetValues<BowClass>().Cast<BowClass?>()];

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool ActiveOnly { get; set; } = true;

    [ObservableProperty]
    public partial BowClass? BowClassFilter { get; set; }

    [ObservableProperty]
    public partial MemberSort Sort { get; set; } = MemberSort.Name;

    [ObservableProperty]
    public partial string CountSummary { get; set; } = string.Empty;

    public string? SignedInAs => auth.DisplayName;

    public override Task OnAppearingAsync(CancellationToken ct = default)
    {
        Title = "Members";
        // Reload every time: another device, or the web app, may have changed the data while
        // this page was off screen.
        return LoadAsync(ct);
    }

    [RelayCommand]
    async Task LoadAsync(CancellationToken ct)
    {
        await RunAsync(async token =>
        {
            var members = await api.GetMembersAsync(token) ?? [];
            _all.Clear();
            _all.AddRange(members);
            ApplyFilter();
        }, ct);
    }

    [RelayCommand]
    async Task RefreshAsync(CancellationToken ct)
    {
        IsRefreshing = true;
        await LoadAsync(ct);
    }

    [RelayCommand]
    Task AddAsync() => navigation.GoToAsync(Routes.MemberEdit);

    [RelayCommand]
    Task OpenAsync(Member? member) => member is null
        ? Task.CompletedTask
        : navigation.GoToAsync(Routes.MemberDetail,
            new Dictionary<string, object> { [Routes.MemberIdKey] = member.Id });

    [RelayCommand]
    async Task DeleteAsync(Member? member, CancellationToken ct)
    {
        if (member is null)
            return;

        if (!await dialogs.ConfirmAsync("Delete member", $"Delete {member.FullName}?", "Delete", "Cancel"))
            return;

        var deleted = await RunAsync(
            token => api.DeleteMemberAsync(member.Id, token).EnsureArcherySuccessAsync(token), ct);

        if (deleted)
        {
            _all.Remove(member);
            ApplyFilter();
        }
    }

    [RelayCommand]
    async Task SignOutAsync(CancellationToken ct)
    {
        if (!await dialogs.ConfirmAsync("Sign out", "Sign out of Archery Club?", "Sign out", "Cancel"))
            return;

        await auth.SignOutAsync(ct);
        _all.Clear();
        Members.Clear();
        await navigation.GoToRootAsync(Routes.Login);
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    partial void OnActiveOnlyChanged(bool value) => ApplyFilter();

    partial void OnBowClassFilterChanged(BowClass? value) => ApplyFilter();

    partial void OnSortChanged(MemberSort value) => ApplyFilter();

    /// <summary>Mirrors the filter composition on the web app's Members page.</summary>
    void ApplyFilter()
    {
        IEnumerable<Member> query = _all;

        if (ActiveOnly)
            query = query.Where(m => m.IsActive);

        if (BowClassFilter is { } bowClass)
            query = query.Where(m => m.PreferredBowClass == bowClass);

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var term = SearchText.Trim();
            query = query.Where(m =>
                m.FullName.Contains(term, StringComparison.OrdinalIgnoreCase)
                || (m.Email?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        query = Sort switch
        {
            MemberSort.JoinDateNewest => query.OrderByDescending(m => m.JoinDate),
            MemberSort.JoinDateOldest => query.OrderBy(m => m.JoinDate),
            MemberSort.BowClass => query.OrderBy(m => m.PreferredBowClass).ThenBy(m => m.FirstName),
            _ => query.OrderBy(m => m.FirstName).ThenBy(m => m.LastName)
        };

        Members.Clear();
        foreach (var member in query)
            Members.Add(member);

        CountSummary = Members.Count == _all.Count
            ? $"{_all.Count} members"
            : $"{Members.Count} of {_all.Count} members";
    }
}
