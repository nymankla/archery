using Archery.Mobile.Core;
using Archery.Mobile.Core.ViewModels;

namespace Archery.Mobile.Views;

public partial class ExternalParticipantEditPage : ContentPage, IQueryAttributable
{
    readonly ExternalParticipantEditViewModel _vm;
    Guid? _participantId;
    bool _initialised;

    public ExternalParticipantEditPage(ExternalParticipantEditViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query) =>
        _participantId = query.GetGuid(Routes.ExternalParticipantIdKey);

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_initialised)
            return;

        _initialised = true;
        await _vm.InitialiseAsync(_participantId);
    }
}
