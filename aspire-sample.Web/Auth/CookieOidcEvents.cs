using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;

namespace aspire_sample.Web.Auth;

public class CookieOidcEvents(IOptions<AuthSessionOptions> options) : CookieAuthenticationEvents
{
    private readonly AuthSessionOptions _options = options.Value;

    public override Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        var lastActivity = context.Properties.GetTokenValue("last_activity_utc");
        if (DateTimeOffset.TryParse(lastActivity, out var lastSeenUtc))
        {
            if (DateTimeOffset.UtcNow - lastSeenUtc > TimeSpan.FromMinutes(_options.IdleTimeoutMinutes))
            {
                context.RejectPrincipal();
                return context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
        }

        context.ShouldRenew = true;
        context.Properties.UpdateTokenValue("last_activity_utc", DateTimeOffset.UtcNow.ToString("O"));
        return Task.CompletedTask;
    }
}