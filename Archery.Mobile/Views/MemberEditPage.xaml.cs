using Archery.Mobile.Core;
using Archery.Mobile.Core.ViewModels;

namespace Archery.Mobile.Views;

/// <summary>Add or edit a member; no id means a new one.</summary>
public partial class MemberEditPage : ContentPage, IQueryAttributable
{
    readonly MemberEditViewModel _vm;
    Guid? _memberId;
    bool _initialised;

    public MemberEditPage(MemberEditViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query) =>
        _memberId = query.GetGuid(Routes.MemberIdKey);

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Guarded: OnAppearing also fires on the way back from a pushed page, and reloading
        // would discard whatever the user had typed.
        if (_initialised)
            return;

        _initialised = true;
        await _vm.InitialiseAsync(_memberId);
    }
}
