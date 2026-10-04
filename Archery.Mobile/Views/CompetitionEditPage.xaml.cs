using Archery.Mobile.Core;
using Archery.Mobile.Core.ViewModels;

namespace Archery.Mobile.Views;

public partial class CompetitionEditPage : ContentPage, IQueryAttributable
{
    readonly CompetitionEditViewModel _vm;
    Guid? _competitionId;
    bool _initialised;

    public CompetitionEditPage(CompetitionEditViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query) =>
        _competitionId = query.GetGuid(Routes.CompetitionIdKey);

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_initialised)
            return;

        _initialised = true;
        await _vm.InitialiseAsync(_competitionId);
    }
}
