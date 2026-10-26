using Archery.Data;
using aspire.MigrationService;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddSource(MigrationWorker.ActivitySourceName));

builder.AddNpgsqlDbContext<ArcheryDbContext>("db");
builder.Services.AddHostedService<MigrationWorker>();

builder.Build().Run();
