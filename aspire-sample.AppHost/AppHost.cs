var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache")
    .WithDataVolume("redisdata")
    .WithLifetime(ContainerLifetime.Persistent);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume("dbdata")
    .WithLifetime(ContainerLifetime.Persistent);
var db = postgres.AddDatabase("db");

var apiService = builder.AddProject<Projects.aspire_sample_ApiService>("apiservice")
    .WithHttpHealthCheck("/health")
    .WithReference(db)
    .WaitFor(db);

builder.AddProject<Projects.aspire_sample_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
