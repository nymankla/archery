using Archery.Mobile.Core.ViewModels;

namespace Archery.Mobile.Views;

public partial class LoginPage : ContentPage
{
    readonly LoginViewModel _vm;

    public LoginPage(LoginViewModel vm)
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
