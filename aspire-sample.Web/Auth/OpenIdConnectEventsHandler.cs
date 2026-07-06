using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace aspire_sample.Web.Auth;

public class OpenIdConnectEventsHandler : OpenIdConnectEvents
{
    private readonly ILogger<OpenIdConnectEventsHandler> _logger;

    public OpenIdConnectEventsHandler(ILogger<OpenIdConnectEventsHandler> logger)
    {
        _logger = logger;
    }

    public override Task TokenValidated(TokenValidatedContext context)
    {
        context.Properties?.UpdateTokenValue("last_activity_utc", DateTimeOffset.UtcNow.ToString("O"));
        _logger.LogDebug("Token validated for {Subject}",
            context.Principal?.FindFirst("sub")?.Value ?? "unknown");
        return Task.CompletedTask;
    }

    public override async Task RedirectToIdentityProviderForSignOut(RedirectContext context)
    {
        var idToken = context.Properties?.GetTokenValue(OpenIdConnectParameterNames.IdToken);

        if (string.IsNullOrWhiteSpace(idToken))
        {
            var authResult = await context.HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            idToken = authResult.Properties?.GetTokenValue(OpenIdConnectParameterNames.IdToken);
        }

        if (!string.IsNullOrWhiteSpace(idToken))
        {
            context.ProtocolMessage.IdTokenHint = idToken;
        }
        else
        {
            _logger.LogWarning("OIDC sign-out is missing an id_token for id_token_hint.");
        }

        await base.RedirectToIdentityProviderForSignOut(context);
    }

    public override Task RemoteFailure(RemoteFailureContext context)
    {
        context.HandleResponse();

        var message = context.Failure?.Message ?? "unknown";
        _logger.LogWarning("OIDC remote failure: {Message}", message);

        // "Correlation failed" fires when:
        //  - The app restarted and lost its data-protection keys (correlation cookie unreadable)
        //  - Keycloak already had an active SSO session and silently redirected back while the
        //    app's correlation state was gone or stale
        // Redirecting to /login retries the full OIDC flow from a clean state.
        context.Response.Redirect("/login");
        return Task.CompletedTask;
    }
}
