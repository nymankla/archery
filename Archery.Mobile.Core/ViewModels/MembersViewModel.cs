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
    ILogger<MembersViewModel> logger) : BaseViewModel(logger)
{
    readonly List<Member> _all = [];

    public ObservableCollection<Member> Members { get; } = [];

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool ActiveOnly { get; set; } = true;

    public string? SignedInAs => auth.DisplayName;

    public override Task OnAppearingAsync(CancellationToken ct = default)
    {
        Title = "Members";
        return Members.Count == 0 ? LoadAsync(ct) : Task.CompletedTask;
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
    async Task SignOutAsync(CancellationToken ct)
    {
        await auth.SignOutAsync(ct);
        _all.Clear();
        Members.Clear();
        await navigation.GoToRootAsync(Routes.Login);
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    partial void OnActiveOnlyChanged(bool value) => ApplyFilter();

    void ApplyFilter()
    {
        IEnumerable<Member> query = _all;

        if (ActiveOnly)
            query = query.Where(m => m.IsActive);

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var term = SearchText.Trim();
            query = query.Where(m =>
                m.FullName.Contains(term, StringComparison.OrdinalIgnoreCase)
                || (m.Email?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        Members.Clear();
        foreach (var member in query.OrderBy(m => m.FirstName).ThenBy(m => m.LastName))
            Members.Add(member);
    }
}
