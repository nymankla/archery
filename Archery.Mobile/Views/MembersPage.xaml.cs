using Archery.Mobile.Core.ViewModels;

namespace Archery.Mobile.Views;

public partial class MembersPage : ContentPage
{
    readonly MembersViewModel _vm;

    public MembersPage(MembersViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.OnAppearingAsync();
    }
}
