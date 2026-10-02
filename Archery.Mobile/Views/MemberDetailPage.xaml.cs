using Archery.Mobile.Core;
using Archery.Mobile.Core.ViewModels;

namespace Archery.Mobile.Views;

[QueryProperty(nameof(MemberId), Routes.MemberIdKey)]
public partial class MemberDetailPage : ContentPage
{
    readonly MemberDetailViewModel _vm;

    public MemberDetailPage(MemberDetailViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    public Guid MemberId { get; set; }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Unguarded on purpose: coming back from the edit page should show the new values.
        if (MemberId != Guid.Empty)
            await _vm.InitialiseAsync(MemberId);
    }
}
