using Android.App;
using Android.Content;
using Android.Content.PM;

namespace Archery.Mobile.Platforms.Android;

/// <summary>
/// Receives the OIDC redirect back from the Chrome Custom Tab.
/// </summary>
/// <remarks>
/// Without this activity and its intent filter, Keycloak's redirect to
/// se.archeryclub.mobile://oauth/callback goes nowhere and sign-in appears to hang. The scheme
/// must match both the ApplicationId and the redirect URIs registered on the archerymobile
/// Keycloak client.
/// </remarks>
[Activity(NoHistory = true, LaunchMode = LaunchMode.SingleTop, Exported = true)]
[IntentFilter(
    [Intent.ActionView],
    Categories = [Intent.CategoryDefault, Intent.CategoryBrowsable],
    DataScheme = CallbackScheme)]
public class WebAuthenticationCallbackActivity : Microsoft.Maui.Authentication.WebAuthenticatorCallbackActivity
{
    const string CallbackScheme = "se.archeryclub.mobile";
}
