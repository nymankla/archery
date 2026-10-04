using Archery.Mobile.Core.ViewModels;

namespace Archery.Mobile.Views;

public partial class DashboardPage : ContentPage
{
    readonly DashboardViewModel _vm;

    public DashboardPage(DashboardViewModel vm)
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
