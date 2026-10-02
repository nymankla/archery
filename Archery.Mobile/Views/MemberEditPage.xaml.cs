using Archery.Mobile.Core;
using Archery.Mobile.Core.ViewModels;

namespace Archery.Mobile.Views;

/// <summary>
/// Add or edit a member. The member id arrives as a Shell query parameter; its absence means
/// "new member".
/// </summary>
[QueryProperty(nameof(MemberId), Routes.MemberIdKey)]
public partial class MemberEditPage : ContentPage
{
    readonly MemberEditViewModel _vm;
    bool _initialised;

    public MemberEditPage(MemberEditViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    public Guid? MemberId { get; set; }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Shell sets the query property after construction but before appearing, so load here.
        // Guarded because OnAppearing also fires when returning from a pushed page, and
        // reloading then would discard whatever the user had typed.
        if (_initialised)
            return;

        _initialised = true;
        await _vm.InitialiseAsync(MemberId);
    }
}
