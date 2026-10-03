using Archery.Mobile.Core;
using Archery.Mobile.Core.ViewModels;

namespace Archery.Mobile.Views;

public partial class MemberDetailPage : ContentPage, IQueryAttributable
{
    readonly MemberDetailViewModel _vm;
    Guid _memberId;

    public MemberDetailPage(MemberDetailViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query) =>
        _memberId = query.GetGuid(Routes.MemberIdKey) ?? Guid.Empty;

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Unguarded on purpose: coming back from the edit page should show the new values.
        if (_memberId != Guid.Empty)
            await _vm.InitialiseAsync(_memberId);
    }
}
