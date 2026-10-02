using Archery.Client;
using Microsoft.AspNetCore.Authentication;

namespace aspire.Web.Auth;

/// <summary>
/// Supplies the API bearer token on the Blazor server, where it lives in the auth cookie's
/// saved tokens rather than in a mobile secure store.
/// </summary>
public sealed class WebArcheryTokenProvider(
    AccessTokenProvider tokenProvider,
    IHttpContextAccessor httpContextAccessor) : IArcheryTokenProvider
{
    public ValueTask<string?> GetAccessTokenAsync(CancellationToken ct = default)
    {
        // During interactive circuit, AccessTokenProvider holds the token.
        // During SSR prerender, Interactive Server components run in a separate DI scope where
        // AccessTokenProvider.AccessToken is null — fall back to reading from the HTTP context
        // (the auth middleware cached it in IAuthenticateResultFeature for this request).
        var token = tokenProvider.AccessToken;

        if (string.IsNullOrWhiteSpace(token))
        {
            var feature = httpContextAccessor.HttpContext?
                .Features.Get<IAuthenticateResultFeature>();
            token = feature?.AuthenticateResult?.Properties?.GetTokenValue("access_token");
        }

        return ValueTask.FromResult(token);
    }
}
