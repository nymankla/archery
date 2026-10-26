using System.Diagnostics;
using Archery.Data;
using Microsoft.EntityFrameworkCore;

namespace aspire.MigrationService;

/// <summary>
/// Applies the EF Core migrations once and exits.
/// </summary>
/// <remarks>
/// The AppHost starts the API with <c>WaitForCompletion</c> on this resource, so the schema is
/// current before any API instance runs, and API replicas never race each other to migrate.
/// A failure sets a non-zero exit code, which keeps the API from starting at all.
/// </remarks>
public sealed partial class MigrationWorker(
    IServiceProvider services,
    IHostApplicationLifetime lifetime,
    ILogger<MigrationWorker> logger) : BackgroundService
{
    public const string ActivitySourceName = "Archery.Migrations";
    static readonly ActivitySource ActivitySource = new(ActivitySourceName);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var activity = ActivitySource.StartActivity("Migrate database", ActivityKind.Client);

        try
        {
            await using var scope = services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ArcheryDbContext>();

            var pending = (await db.Database.GetPendingMigrationsAsync(stoppingToken)).ToList();
            await db.Database.MigrateAsync(stoppingToken);

            LogMigrated(logger, pending.Count, pending);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            activity?.AddException(ex);
            LogFailed(logger, ex);
            Environment.ExitCode = 1;
        }

        lifetime.StopApplication();
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Applied {Count} migration(s): {Migrations}")]
    static partial void LogMigrated(ILogger logger, int count, IEnumerable<string> migrations);

    [LoggerMessage(Level = LogLevel.Error, Message = "Database migration failed")]
    static partial void LogFailed(ILogger logger, Exception exception);
}
