using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace aspire.Web.Auth;

public partial class OpenIdConnectEventsHandler : OpenIdConnectEvents
{
    private readonly ILogger<OpenIdConnectEventsHandler> _logger;

    public OpenIdConnectEventsHandler(ILogger<OpenIdConnectEventsHandler> logger)
    {
        _logger = logger;
    }

    public override Task TokenValidated(TokenValidatedContext context)
    {
        context.Properties?.UpdateTokenValue("last_activity_utc", DateTimeOffset.UtcNow.ToString("O"));
        LogTokenValidated(_logger, context.Principal?.FindFirst("sub")?.Value ?? "unknown");
        return Task.CompletedTask;
    }

    public override async Task RedirectToIdentityProviderForSignOut(RedirectContext context)
    {
        context.ProtocolMessage.ClientId = context.Options.ClientId;

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
            LogSignOutWithoutIdToken(_logger);
        }

        await base.RedirectToIdentityProviderForSignOut(context);
    }

    public override Task RemoteFailure(RemoteFailureContext context)
    {
        context.HandleResponse();

        var message = context.Failure?.Message ?? "unknown";
        LogRemoteFailure(_logger, message);

        // "Correlation failed" fires when:
        //  - The app restarted and lost its data-protection keys (correlation cookie unreadable)
        //  - Keycloak already had an active SSO session and silently redirected back while the
        //    app's correlation state was gone or stale
        // Redirecting to /login retries the full OIDC flow from a clean state.
        context.Response.Redirect("/login");
        return Task.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Token validated for {Subject}")]
    static partial void LogTokenValidated(ILogger logger, string subject);

    [LoggerMessage(Level = LogLevel.Warning, Message = "OIDC sign-out is missing an id_token for id_token_hint.")]
    static partial void LogSignOutWithoutIdToken(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "OIDC remote failure: {Message}")]
    static partial void LogRemoteFailure(ILogger logger, string message);
}
