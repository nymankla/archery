using Archery.Mobile.Core.ViewModels;

namespace Archery.Mobile.Views;

public partial class CompetitionsPage : ContentPage
{
    readonly CompetitionsViewModel _vm;

    public CompetitionsPage(CompetitionsViewModel vm)
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
