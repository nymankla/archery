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

var apiService = builder.AddProject<Projects.aspire_ApiService>("apiservice")
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
