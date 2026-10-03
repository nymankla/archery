using Archery.Mobile.Core.ViewModels;

namespace Archery.Mobile.Views;

public partial class ExternalParticipantsPage : ContentPage
{
    readonly ExternalParticipantsViewModel _vm;

    public ExternalParticipantsPage(ExternalParticipantsViewModel vm)
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
