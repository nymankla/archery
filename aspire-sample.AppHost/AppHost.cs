var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

var postgres = builder.AddPostgres("postgres");
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
