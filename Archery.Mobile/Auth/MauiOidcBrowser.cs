using System.Net;
using Duende.IdentityModel.OidcClient.Browser;
// Disambiguated: MAUI's Microsoft.Maui.ApplicationModel.IBrowser is in scope too.
using IOidcBrowser = Duende.IdentityModel.OidcClient.Browser.IBrowser;

namespace Archery.Mobile.Auth;

/// <summary>
/// Bridges OidcClient to MAUI's <see cref="WebAuthenticator"/>, which on Android opens a Chrome
/// Custom Tab. Google blocks OAuth in embedded WebViews, so the system browser is the only
/// supported option; it also means the user's existing session and password manager work.
/// </summary>
public sealed class MauiOidcBrowser : IOidcBrowser
{
    public async Task<BrowserResult> InvokeAsync(
        BrowserOptions options, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await WebAuthenticator.Default.AuthenticateAsync(
                new WebAuthenticatorOptions
                {
                    Url = new Uri(options.StartUrl),
                    CallbackUrl = new Uri(options.EndUrl),
                    // Avoids silently reusing a stale Keycloak SSO cookie, which otherwise makes
                    // "sign in as someone else" impossible without clearing browser data.
                    PrefersEphemeralWebBrowserSession = true
                });

            // WebAuthenticator hands back the parsed callback parameters; OidcClient wants the
            // original redirect URL, so reassemble it.
            var query = string.Join("&", result.Properties.Select(p =>
                $"{WebUtility.UrlEncode(p.Key)}={WebUtility.UrlEncode(p.Value)}"));

            return new BrowserResult
            {
                Response = $"{options.EndUrl}?{query}",
                ResultType = BrowserResultType.Success
            };
        }
        catch (TaskCanceledException)
        {
            // The user dismissed the Custom Tab.
            return new BrowserResult { ResultType = BrowserResultType.UserCancel };
        }
        catch (OperationCanceledException)
        {
            return new BrowserResult { ResultType = BrowserResultType.UserCancel };
        }
        catch (Exception ex)
        {
            return new BrowserResult
            {
                ResultType = BrowserResultType.UnknownError,
                Error = ex.Message
            };
        }
    }
}
