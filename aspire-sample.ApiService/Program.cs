using System.Globalization;
using System.Text.Json.Serialization;
using aspire_sample.ApiService.Data;
using aspire_sample.ApiService.Endpoints;
using aspire_sample.ApiService.Services;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

var culture = new CultureInfo(builder.Configuration["Locale"] ?? "sv-SE");
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

builder.AddServiceDefaults();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);
builder.Services.AddAuthentication()
    .AddKeycloakJwtBearer(
        serviceName: "keycloak",
        realm: "archery",
        options =>
        {
            options.Audience = "archery.api";
            if (builder.Environment.IsDevelopment())
                options.RequireHttpsMetadata = false;
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

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ArcheryDbContext>();
    await db.Database.MigrateAsync();
}

app.UseExceptionHandler();
app.UseHttpsRedirection();
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
app.MapExternalParticipantEndpoints();
app.MapExternalParticipantImportEndpoints();
app.MapCompetitionResultEndpoints();
app.MapCompetitionParticipantEndpoints();

app.MapDefaultEndpoints();

app.Run();
