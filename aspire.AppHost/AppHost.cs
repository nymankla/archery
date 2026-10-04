var builder = DistributedApplication.CreateBuilder(args);
// Enable Docker 
 builder.AddDockerComposeEnvironment("production");
 
var cache = builder.AddRedis("cache")
    .WithDataVolume("redisdata")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithContainerName("archery-redis");

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume("dbdata-pg18")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithContainerName("archery-postgres");
var db = postgres.AddDatabase("db");
// Dedicated database for Keycloak's own store. Created automatically by Aspire
// during `aspire run`; the SQL init script covers the published compose path.
var keycloakDb = postgres.AddDatabase("keycloak-db", "keycloak");

var keycloak = builder.AddKeycloak("keycloak", 8080)
    .WithDataVolume("keycloakdata")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithContainerName("archery-keycloak")
    .WithRealmImport("keycloak");

// Aspire publishes only Keycloak's HTTPS endpoint (host 8080 -> container 8443), served with
// a self-signed certificate. Android does not trust that certificate and there is no plain
// HTTP endpoint to fall back to, so the emulator cannot complete an OIDC sign-in. Publish the
// container's own HTTP listener on a separate fixed port for the mobile client to use.
// Development only: in published artifacts Keycloak sits behind TLS termination.
if (!builder.ExecutionContext.IsPublishMode)
{
    keycloak.WithHttpEndpoint(name: "http-mobile", port: 8081, targetPort: 8080);
}

// Local `dotnet run` uses `start-dev` (embedded dev DB). Published artifacts run
// production mode (`start`), which requires an explicit database plus hostname
// and proxy configuration.
if (builder.ExecutionContext.IsPublishMode)
{
    postgres.WithBindMount("postgres-init.d", "/docker-entrypoint-initdb.d", isReadOnly: true);

    keycloak
        .WaitFor(keycloakDb)
        .WithEnvironment("KC_DB", "postgres")
        .WithEnvironment("KC_DB_URL_HOST", "postgres")
        .WithEnvironment("KC_DB_URL_PORT", "5432")
        .WithEnvironment("KC_DB_URL_DATABASE", "keycloak")
        .WithEnvironment("KC_DB_USERNAME", "postgres")
        .WithEnvironment(context =>
            context.EnvironmentVariables["KC_DB_PASSWORD"] = postgres.Resource.PasswordParameter)
        .WithEnvironment("KC_HOSTNAME_STRICT", "false")
        .WithEnvironment("KC_HTTP_ENABLED", "true")
        .WithEnvironment("KC_PROXY_HEADERS", "xforwarded")
        .WithEnvironment("KC_HEALTH_ENABLED", "true")
        .WithEnvironment("KC_METRICS_ENABLED", "true");
}

// The Android client cannot use Aspire service discovery, so unlike the Blazor frontend it
// needs the API published on a real address it can reach from the emulator (10.0.2.2).
var apiService = builder.AddProject<Projects.aspire_ApiService>("apiservice")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(db)
    .WaitFor(db)
    .WithReference(keycloak)
    .WaitFor(keycloak);

builder.AddProject<Projects.aspire_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(apiService)
    .WaitFor(apiService)
    .WithReference(keycloak)
    .WaitFor(keycloak);

builder.Build().Run();
