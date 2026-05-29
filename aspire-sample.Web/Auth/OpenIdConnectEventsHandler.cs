using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

namespace aspire_sample.Web.Auth;

public class OpenIdConnectEventsHandler : OpenIdConnectEvents
{
    public override Task TokenValidated(TokenValidatedContext context)
    {
        context.Properties?.UpdateTokenValue("last_activity_utc", DateTimeOffset.UtcNow.ToString("O"));
        return Task.CompletedTask;
    }
}
