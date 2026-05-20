using aspire_sample.ApiService.Data;
using aspire_sample.ApiService.Endpoints;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

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
}

app.MapGet("/", () => "Archery Club API");

app.MapMemberEndpoints();
app.MapMembershipFeeEndpoints();
app.MapCompetitionEndpoints();
app.MapExternalParticipantEndpoints();
app.MapCompetitionResultEndpoints();

app.MapDefaultEndpoints();

app.Run();
