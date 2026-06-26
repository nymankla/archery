using System.Globalization;
using aspire_sample.Web;
using aspire_sample.Web.Auth;
using aspire_sample.Web.Components;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Localization;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestHeadersTotalSize = 131072);

var culture = new CultureInfo(builder.Configuration["Locale"] ?? "sv-SE");
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

var authSessionSection = builder.Configuration.GetSection(AuthSessionOptions.SectionName);

builder.Services.AddOptions<AuthSessionOptions>()
    .Bind(authSessionSection)
    .Validate(options => options.RefreshMinutes > 0, "AuthSession:RefreshMinutes must be greater than 0.")
    .Validate(options => options.IdleTimeoutMinutes > 0, "AuthSession:IdleTimeoutMinutes must be greater than 0.")
    .Validate(options => options.CookieExpirationMinutes > options.IdleTimeoutMinutes,
        "AuthSession:CookieExpirationMinutes must be greater than AuthSession:IdleTimeoutMinutes.")
    .ValidateOnStart();

builder.AddServiceDefaults();
builder.AddRedisOutputCache("cache");

builder.Services.AddLocalization();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddScoped<AccessTokenProvider>();
builder.Services.AddScoped<TokenRefreshService>();
builder.Services.AddScoped<CookieOidcEvents>();
builder.Services.AddScoped<OpenIdConnectEventsHandler>();

builder.Services.AddHttpClient<ArcheryApiClient>(client =>
    {
        client.BaseAddress = new("https://apiservice");
    });

builder.Services.AddHttpClient("external");

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.LoginPath = "/login";
    options.EventsType = typeof(CookieOidcEvents);
    options.SlidingExpiration = true;
})
.AddKeycloakOpenIdConnect(
    serviceName: "keycloak",
    realm: "archery",
    options =>
    {
        options.ClientId = "archeryweb";
        options.ClientSecret = builder.Configuration["Keycloak:ClientSecret"];
        options.ResponseType = OpenIdConnectResponseType.Code;
        options.SaveTokens = true;
        options.UseTokenLifetime = false;
        options.EventsType = typeof(OpenIdConnectEventsHandler);
        options.PushedAuthorizationBehavior = PushedAuthorizationBehavior.Disable;
        if (builder.Environment.IsDevelopment())
        {
            options.RequireHttpsMetadata = false;
            options.BackchannelHttpHandler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };
        }
    });
var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(culture),
    SupportedCultures = [culture],
    SupportedUICultures = [culture]
});

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.UseOutputCache();

app.MapStaticAssets();

app.MapGet("/login", (string? returnUrl) =>
    Results.Challenge(
        new AuthenticationProperties { RedirectUri = returnUrl ?? "/" },
        [OpenIdConnectDefaults.AuthenticationScheme]))
    .AllowAnonymous();

app.MapGet("/logout", async (HttpContext httpContext) =>
{
    await httpContext.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme, new AuthenticationProperties
    {
        RedirectUri = "/"
    });
    await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
})
    .AllowAnonymous();

app.MapPost("/auth/activity", async (HttpContext httpContext) =>
{
    var result = await httpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    if (!result.Succeeded || result.Properties is null || result.Principal is null)
        return Results.Unauthorized();

    result.Properties.UpdateTokenValue("last_activity_utc", DateTimeOffset.UtcNow.ToString("O"));
    await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, result.Principal, result.Properties);
    return Results.Ok();
}).RequireAuthorization();

app.MapPost("/auth/refresh", async (HttpContext httpContext, TokenRefreshService tokenRefreshService) =>
{
    await tokenRefreshService.TryRefreshAsync(httpContext);
    return Results.Ok();
}).RequireAuthorization();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .RequireAuthorization();

app.MapDefaultEndpoints();

app.Run();
