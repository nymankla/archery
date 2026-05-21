using System.Globalization;
using System.Text.Json.Serialization;
using aspire_sample.ApiService.Data;
using aspire_sample.ApiService.Endpoints;
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

builder.AddNpgsqlDbContext<ArcheryDbContext>("db");

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ArcheryDbContext>();
    await db.Database.MigrateAsync();
}

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapGet("/", () => "Archery Club API");

app.MapMemberEndpoints();
app.MapMemberImportEndpoints();
app.MapMembershipFeeEndpoints();
app.MapCompetitionEndpoints();
app.MapExternalParticipantEndpoints();
app.MapCompetitionResultEndpoints();
app.MapCompetitionParticipantEndpoints();

app.MapDefaultEndpoints();

app.Run();
