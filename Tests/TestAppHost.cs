using Aspire.Hosting;
using Microsoft.Extensions.Logging;

namespace Tests;

/// <summary>Builds and starts the real AppHost for an integration test.</summary>
static class TestAppHost
{
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

    public static async Task<DistributedApplication> StartAsync(LogLevel logLevel, CancellationToken ct)
    {
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.aspire_AppHost>(ct);
        appHost.Services.AddLogging(logging =>
        {
            logging.SetMinimumLevel(logLevel);
            // Override the logging filters from the app's configuration
            logging.AddFilter(appHost.Environment.ApplicationName, logLevel);
            logging.AddFilter("Aspire.", logLevel);
        });
        appHost.Services.ConfigureHttpClientDefaults(clientBuilder => clientBuilder.AddStandardResilienceHandler());

        // The testing builder runs without the dashboard but still hands every project an OTLP
        // endpoint. Exporters then retry against nothing on shutdown, which held the migrations
        // worker — and so the API waiting on it — for ~20s. No dashboard, no export.
        foreach (var project in appHost.Resources.OfType<ProjectResource>())
        {
            project.Annotations.Add(new EnvironmentCallbackAnnotation(
                context => context.EnvironmentVariables.Remove("OTEL_EXPORTER_OTLP_ENDPOINT")));
        }

        var app = await appHost.BuildAsync(ct).WaitAsync(DefaultTimeout, ct);
        try
        {
            await app.StartAsync(ct).WaitAsync(DefaultTimeout, ct);
            return app;
        }
        catch
        {
            await app.DisposeAsync();
            throw;
        }
    }

    /// <summary>The schema comes from the migrations worker, so wait for it to finish, not just for the database.</summary>
    public static Task WaitForMigrationsAsync(this DistributedApplication app, CancellationToken ct)
        => app.ResourceNotifications
            .WaitForResourceAsync("migrations", KnownResourceStates.Finished, ct)
            .WaitAsync(DefaultTimeout, ct);
}
