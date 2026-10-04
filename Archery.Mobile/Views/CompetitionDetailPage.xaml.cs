using Archery.Mobile.Core;
using Archery.Mobile.Core.ViewModels;

namespace Archery.Mobile.Views;

public partial class CompetitionDetailPage : ContentPage, IQueryAttributable
{
    readonly CompetitionDetailViewModel _vm;
    Guid _competitionId;

    public CompetitionDetailPage(CompetitionDetailViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query) =>
        _competitionId = query.GetGuid(Routes.CompetitionIdKey) ?? Guid.Empty;

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Unguarded: returning from registering a participant or recording a result must show
        // the new row.
        if (_competitionId != Guid.Empty)
            await _vm.InitialiseAsync(_competitionId);
    }
}
