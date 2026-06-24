using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Logging;

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
        LogTokenInformation(context);
        return Task.CompletedTask;
    }

    private void LogTokenInformation(TokenValidatedContext context)
    {
        var token = context.Properties?.GetTokenValue("your_token_name_here");
        var claims = context.Principal?.Claims.Select(c => c.Type + ": " + c.Value);

        _logger.LogInformation("Token validated. Token: {Token}, Claims: {Claims}",
            token,
            claims != null ? string.Join(", ", claims) : "no claims"
        );
    }
}
