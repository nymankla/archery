using System.Globalization;
using aspire.Web;
using aspire.Web.Auth;
using aspire.Web.Components;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestHeadersTotalSize = 131072);

// Persist data-protection keys to disk so they survive restarts.
// Without this, any in-flight OIDC correlation cookie (and existing auth cookies)
// become unreadable after a restart, causing "Correlation failed" on the callback.
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(
        Path.Combine(builder.Environment.ContentRootPath, ".dp-keys")));


var culture = new CultureInfo(builder.Configuration["Locale"] ?? "sv-SE");
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

var keycloakRealm = builder.Configuration["Keycloak:Realm"]
    ?? throw new InvalidOperationException("Keycloak:Realm is required.");
var keycloakClientId = builder.Configuration["Keycloak:ClientId"]
    ?? throw new InvalidOperationException("Keycloak:ClientId is required.");

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
builder.Services.AddMemoryCache();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<AccessTokenProvider>();
builder.Services.AddScoped<TokenRefreshService>();
builder.Services.AddScoped<CookieOidcEvents>();
builder.Services.AddScoped<OpenIdConnectEventsHandler>();
builder.Services.AddSingleton<ITicketStore, MemoryCacheTicketStore>();

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
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
})
.AddKeycloakOpenIdConnect(
    serviceName: "keycloak",
    realm: keycloakRealm,
    options =>
    {
        options.ClientId = keycloakClientId;
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

builder.Services.AddOptions<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme)
    .Configure<ITicketStore, IOptions<AuthSessionOptions>>((options, ticketStore, authSessionOptions) =>
    {
        options.SessionStore = ticketStore;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(authSessionOptions.Value.CookieExpirationMinutes);
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
    var authResult = await httpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    var properties = authResult.Succeeded && authResult.Properties is not null
        ? authResult.Properties
        : new AuthenticationProperties();

    properties.RedirectUri = "/";

    return Results.SignOut(properties,
        [CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme]);
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
    // Return the current (possibly just-refreshed) token so the Blazor circuit can update its in-memory copy.
    var token = await httpContext.GetTokenAsync("access_token");
    return Results.Ok(new { accessToken = token });
}).RequireAuthorization();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .RequireAuthorization();

app.MapDefaultEndpoints();

app.Run();
