var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache")
    .WithDataVolume("redisdata")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithContainerName("archery-redis");

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume("dbdata")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithContainerName("archery-postgres");
var db = postgres.AddDatabase("db");


var keycloak = builder.AddKeycloak("keycloak", 8080)
    .WithDataVolume("keycloakdata")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithContainerName("archery-keycloak")
    .WithRealmImport("keycloak");

var apiService = builder.AddProject<Projects.aspire_sample_ApiService>("apiservice")
    .WithHttpHealthCheck("/health")
    .WithReference(db)
    .WaitFor(db)
    .WithReference(keycloak)
    .WaitFor(keycloak);

builder.AddProject<Projects.aspire_sample_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(apiService)
    .WaitFor(apiService)
    .WithReference(keycloak)
    .WaitFor(keycloak);

builder.Build().Run();
