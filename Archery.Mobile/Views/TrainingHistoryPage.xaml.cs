using Archery.Mobile.Core.ViewModels;

namespace Archery.Mobile.Views;

public partial class TrainingHistoryPage : ContentPage
{
    readonly TrainingHistoryViewModel _vm;

    public TrainingHistoryPage(TrainingHistoryViewModel vm)
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
