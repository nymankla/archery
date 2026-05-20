# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
# Run the distributed app (starts AppHost, which orchestrates all services + Redis)
dotnet run --project aspire-sample.AppHost

# Build entire solution
dotnet build aspire-sample.sln

# Run all tests
dotnet test aspire-sample.sln

# Run a single test class
dotnet test aspire-sample.Tests --filter "FullyQualifiedName~WebTests"
```

The AppHost launches the Aspire dashboard at the URL shown in terminal output (typically `https://localhost:17034`).

## Architecture

.NET 10 Aspire distributed application with three runtime projects and one shared library:

- **AppHost** — Orchestrator only. Defines the resource graph: Redis → ApiService → Web frontend. No business logic.
- **ApiService** — ASP.NET Core Web API. Single `/weatherforecast` endpoint returning random forecast data. Entry point for backend logic.
- **Web** — Blazor interactive server app. Calls ApiService via `WeatherApiClient` (typed `HttpClient`). Uses Redis output caching.
- **ServiceDefaults** — Shared extension methods applied in every service's `Program.cs`. Wires OpenTelemetry (OTLP), service discovery, HTTP resilience (retry + circuit breaker), and default `/health` + `/alive` endpoints.
- **Tests** — xUnit integration tests using `Aspire.Hosting.Testing` to spin up the full AppHost in-process.

### Inter-service communication

Web → ApiService uses Aspire service discovery with the scheme `https+http://apiservice`. The name `"apiservice"` matches the resource name in `AppHost.cs`. No hardcoded URLs.

### Adding a new service

1. Add project reference to AppHost.
2. Call `builder.AddProject<Projects.YourProject>("name").WithHttpHealthCheck("/health")`.
3. Add `builder.AddServiceDefaults()` in the new project's `Program.cs`.
4. Reference it from dependent services via `.WithReference(...)` and `.WaitFor(...)`.
