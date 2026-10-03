using Archery.Mobile.Core;
using Archery.Mobile.Core.ViewModels;

namespace Archery.Mobile.Views;

public partial class ParticipantRegisterPage : ContentPage, IQueryAttributable
{
    readonly ParticipantRegisterViewModel _vm;
    Guid _competitionId;
    bool _initialised;

    public ParticipantRegisterPage(ParticipantRegisterViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query) =>
        _competitionId = query.GetGuid(Routes.CompetitionIdKey) ?? Guid.Empty;

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_initialised || _competitionId == Guid.Empty)
            return;

        _initialised = true;
        await _vm.InitialiseAsync(_competitionId);
    }

    void OnMemberClicked(object? sender, EventArgs e) => _vm.Kind = ParticipantKind.Member;

    void OnGuestClicked(object? sender, EventArgs e) => _vm.Kind = ParticipantKind.External;
}
