using Archery.Mobile.Core.ViewModels;

namespace Archery.Mobile.Views;

public partial class TrainingAttendancePage : ContentPage
{
    readonly TrainingAttendanceViewModel _vm;

    public TrainingAttendancePage(TrainingAttendanceViewModel vm)
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
