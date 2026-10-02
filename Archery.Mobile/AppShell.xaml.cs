using Archery.Mobile.Core;
using Archery.Mobile.Views;

namespace Archery.Mobile;

public partial class AppShell : Shell
{
    public AppShell(IAuthService auth)
    {
        InitializeComponent();

        // Detail and edit pages are pushed onto the stack rather than being flyout
        // destinations, so they have to be registered by route.
        Routing.RegisterRoute(Routes.MemberDetail, typeof(MemberDetailPage));
        Routing.RegisterRoute(Routes.MemberEdit, typeof(MemberEditPage));

        // Raised when a refresh fails and the session cannot be recovered — a refresh token
        // that expired or was revoked server-side. Without this the user is left on a screen
        // that only ever reports "your session has expired", with no way back to signing in.
        auth.SessionEnded += OnSessionEnded;
    }

    async void OnSessionEnded(object? sender, EventArgs e)
    {
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            // Absolute route, so the whole navigation stack is discarded along with any
            // half-filled form belonging to the signed-out session.
            await GoToAsync(Routes.Login);
            await DisplayAlert("Signed out",
                "Your session has expired. Please sign in again.", "OK");
        });
    }
}
