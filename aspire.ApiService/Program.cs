using System.Globalization;
using System.Text.Json.Serialization;
using aspire.ApiService.Data;
using aspire.ApiService.Endpoints;
using aspire.ApiService.Services;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

var culture = new CultureInfo(builder.Configuration["Locale"] ?? "sv-SE");
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

var keycloakRealm = builder.Configuration["Keycloak:Realm"]
    ?? throw new InvalidOperationException("Keycloak:Realm is required.");
var keycloakAudience = builder.Configuration["Keycloak:Audience"]
    ?? throw new InvalidOperationException("Keycloak:Audience is required.");

builder.AddServiceDefaults();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);
builder.Services.AddAuthentication()
    .AddKeycloakJwtBearer(
        serviceName: "keycloak",
        realm: keycloakRealm,
        options =>
        {
            options.Audience = keycloakAudience;
            if (builder.Environment.IsDevelopment())
            {
                options.RequireHttpsMetadata = false;

                // Keycloak in start-dev mode derives the "iss" claim from the Host header of
                // the token request. A token the Android emulator obtains via 10.0.2.2 therefore
                // carries a different issuer than the one in the discovery document the API
                // reads over service discovery, and validation fails with IDX10205 — surfacing
                // as a bare 401 with no body. Accept the extra dev issuers explicitly.
                // Signing keys still come from the JWKS at Authority, so this only widens which
                // issuer strings are allowed, and only outside production.
                var extraIssuers = builder.Configuration
                    .GetSection("Keycloak:AdditionalValidIssuers").Get<string[]>();

                if (extraIssuers is { Length: > 0 })
                {
                    options.TokenValidationParameters.ValidIssuers = extraIssuers;
                }
            }
        });
builder.Services.AddAuthorization();
builder.AddNpgsqlDbContext<ArcheryDbContext>("db");

builder.Services.AddScoped<IMemberService, MemberService>();
builder.Services.AddScoped<IMembershipFeeService, MembershipFeeService>();
builder.Services.AddScoped<ICompetitionService, CompetitionService>();
builder.Services.AddScoped<ICompetitionParticipantService, CompetitionParticipantService>();
builder.Services.AddScoped<ICompetitionResultService, CompetitionResultService>();
builder.Services.AddScoped<IExternalParticipantService, ExternalParticipantService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<ITrainingAttendanceService, TrainingAttendanceService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ArcheryDbContext>();
    await db.Database.MigrateAsync();
}

app.UseExceptionHandler();
// Not in development: the Android emulator reaches the API over plain HTTP, because it does
// not trust the ASP.NET development certificate. Redirecting would bounce it to an endpoint
// it cannot validate. The Blazor frontend calls the API over HTTPS via service discovery
// either way, so it is unaffected.
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapGet("/", (IHostEnvironment env) => env.IsDevelopment()
    ? Results.Content("""
        <h1>Archery Club API</h1>
        <p><a href="/scalar/v1">API Documentation (Scalar)</a></p>
        """, "text/html")
    : Results.Ok("Archery Club API"));

app.MapDashboardEndpoints();
app.MapMemberEndpoints();
app.MapMemberImportEndpoints();
app.MapMembershipFeeEndpoints();
app.MapCompetitionEndpoints();
app.MapCompetitionImportEndpoints();
app.MapExternalParticipantEndpoints();
app.MapExternalParticipantImportEndpoints();
app.MapCompetitionResultEndpoints();
app.MapCompetitionParticipantEndpoints();
app.MapTrainingAttendanceEndpoints();

app.MapDefaultEndpoints();

app.Run();
