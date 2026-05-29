using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace aspire_sample.Web.Auth;

public class TokenRefreshService(IOptions<AuthSessionOptions> options)
{
    private readonly AuthSessionOptions _options = options.Value;

    public async Task<bool> TryRefreshAsync(HttpContext httpContext)
    {
        var authResult = await httpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        if (!authResult.Succeeded || authResult.Properties is null)
            return false;

        var issuedUtc = authResult.Properties.IssuedUtc;
        if (issuedUtc is not null && DateTimeOffset.UtcNow - issuedUtc.Value < TimeSpan.FromMinutes(_options.RefreshMinutes))
            return false;

        var refreshToken = authResult.Properties.GetTokenValue(OpenIdConnectParameterNames.RefreshToken);
        if (string.IsNullOrWhiteSpace(refreshToken))
            return false;

        var schemeProvider = httpContext.RequestServices.GetRequiredService<IAuthenticationSchemeProvider>();
        var oidcScheme = await schemeProvider.GetSchemeAsync(OpenIdConnectDefaults.AuthenticationScheme);
        if (oidcScheme is null)
            return false;

        var oidcOptionsMonitor = httpContext.RequestServices.GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>();
        var oidcOptions = oidcOptionsMonitor.Get(oidcScheme.Name);
        var configuration = await oidcOptions.ConfigurationManager!.GetConfigurationAsync(httpContext.RequestAborted);

        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, configuration.TokenEndpoint)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                [OpenIdConnectParameterNames.ClientId] = oidcOptions.ClientId!,
                [OpenIdConnectParameterNames.ClientSecret] = oidcOptions.ClientSecret!,
                [OpenIdConnectParameterNames.GrantType] = OpenIdConnectGrantTypes.RefreshToken,
                [OpenIdConnectParameterNames.RefreshToken] = refreshToken
            })
        };

        using var response = await oidcOptions.Backchannel.SendAsync(requestMessage, httpContext.RequestAborted);
        if (!response.IsSuccessStatusCode)
            return false;

        var payload = await response.Content.ReadFromJsonAsync<TokenResponse>(httpContext.RequestAborted);
        if (payload is null || string.IsNullOrWhiteSpace(payload.AccessToken))
            return false;

        var expiresAt = DateTimeOffset.UtcNow.AddSeconds(payload.ExpiresIn > 0 ? payload.ExpiresIn : 300);
        var newRefreshToken = string.IsNullOrWhiteSpace(payload.RefreshToken) ? refreshToken : payload.RefreshToken;

        authResult.Properties.UpdateTokenValue(OpenIdConnectParameterNames.AccessToken, payload.AccessToken);
        if (!string.IsNullOrWhiteSpace(payload.IdToken))
            authResult.Properties.UpdateTokenValue(OpenIdConnectParameterNames.IdToken, payload.IdToken);
        authResult.Properties.UpdateTokenValue(OpenIdConnectParameterNames.RefreshToken, newRefreshToken);
        authResult.Properties.UpdateTokenValue("expires_at", expiresAt.ToString("O"));
        authResult.Properties.UpdateTokenValue("last_activity_utc", DateTimeOffset.UtcNow.ToString("O"));
        authResult.Properties.IssuedUtc = DateTimeOffset.UtcNow;
        authResult.Properties.ExpiresUtc = expiresAt;

        await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, authResult.Principal!, authResult.Properties);
        return true;
    }

    private sealed class TokenResponse
    {
        [JsonPropertyName("access_token")]  public string? AccessToken  { get; set; }
        [JsonPropertyName("id_token")]      public string? IdToken      { get; set; }
        [JsonPropertyName("refresh_token")] public string? RefreshToken { get; set; }
        [JsonPropertyName("expires_in")]    public int     ExpiresIn    { get; set; }
    }
}
