using Archery.Mobile.Core;
using Archery.Mobile.Core.ViewModels;

namespace Archery.Mobile.Views;

public partial class ResultEditPage : ContentPage, IQueryAttributable
{
    readonly ResultEditViewModel _vm;
    Guid _competitionId;
    Guid? _resultId;
    Guid? _fromParticipantId;
    bool _initialised;

    public ResultEditPage(ResultEditViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        _competitionId = query.GetGuid(Routes.CompetitionIdKey) ?? Guid.Empty;
        _resultId = query.GetGuid(Routes.ResultIdKey);
        // Set when opened from a registration, to pre-fill that archer and their classes.
        _fromParticipantId = query.GetGuid(Routes.FromParticipantIdKey);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_initialised || _competitionId == Guid.Empty)
            return;

        _initialised = true;
        await _vm.InitialiseAsync(_competitionId, _resultId, _fromParticipantId);
    }

    void OnMemberClicked(object? sender, EventArgs e) => _vm.Kind = ParticipantKind.Member;

    void OnGuestClicked(object? sender, EventArgs e) => _vm.Kind = ParticipantKind.External;
}
