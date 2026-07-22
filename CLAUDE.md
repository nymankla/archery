# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
# Run the distributed app (AppHost orchestrates Redis, PostgreSQL, Keycloak, the API, and the Web frontend)
dotnet run --project aspire.AppHost

# Publish deployment artifacts from the AppHost model
aspire publish

# Deploy using the configured Aspire deployment environment
aspire deploy

# Build entire solution
dotnet build Archery.sln

# Run all tests (unit + integration)
dotnet test Archery.sln

# Fast unit tests only (no containers required)
dotnet test UnitTests

# A single integration test class
dotnet test Tests --filter "FullyQualifiedName~WebTests"
```

The AppHost launches the Aspire dashboard at the URL shown in terminal output (typically `https://localhost:17034`). A container runtime (Docker/Podman) is required — the integration tests in `Tests` boot the full AppHost in-process and also need it.

## Overview

.NET 10 Aspire distributed application: an **archery club management system** covering members, membership fees, competitions, results, and external (guest) participants. Secured minimal-API backend + interactive Blazor Server frontend, orchestrated with Redis, PostgreSQL, and Keycloak.

## Architecture

Four runtime pieces, one shared library, two test projects:

- **aspire.AppHost** — Orchestrator only. Defines the resource graph. No business logic.
- **aspire.ApiService** — ASP.NET Core minimal-API backend. EF Core (Npgsql/PostgreSQL) persistence, domain services, spreadsheet import. Secured with Keycloak JWT bearer.
- **aspire.Web** — Blazor interactive server frontend. Keycloak OIDC login, calls the API with a bearer token, Redis output caching.
- **aspire.ServiceDefaults** — Shared extension methods applied in every service's `Program.cs`: OpenTelemetry (OTLP), service discovery, HTTP resilience (retry + circuit breaker), default `/health` + `/alive` endpoints.
- **Tests** — xUnit integration tests using `Aspire.Hosting.Testing` to spin up the full AppHost in-process (incl. Postgres persistence).
- **UnitTests** — fast xUnit unit tests for services (dashboard stats, membership fee logic).

### Resource graph (AppHost.cs)

- **Redis** (`cache`) — output caching for Web.
- **PostgreSQL** (`postgres` → database `db`) — primary data store.
- **Keycloak** (`keycloak`, port 8080) — identity provider; realm imported from `keycloak/` on startup.

All three are persistent containers with data volumes. Flow: Redis + PostgreSQL + Keycloak → ApiService → Web (external HTTP), each wired with `WithReference(...)` + `WaitFor(...)`.

### Inter-service communication

Uses Aspire service discovery — no hardcoded URLs. The Web `ArcheryApiClient` targets `https://apiservice` (matching the AppHost resource name); Keycloak is resolved by service name in both projects.

## ApiService

- **Domain** (`Models/`): `Member`, `MembershipFee`, `Competition`, `CompetitionResult`, `CompetitionParticipant`, `ExternalParticipant`. Enums in `Models/Enums.cs` (`BowClass`, `AgeClass`, `Gender`, `FeeStatus`, `CompetitionType`).
- **Persistence** (`Data/ArcheryDbContext.cs`): EF Core with fluent config, check constraints (a result/participant belongs to *either* a member *or* an external participant), and unique indexes. Migrations in `Migrations/` are applied automatically at startup.
- **Endpoints** (`Endpoints/`): grouped minimal APIs, all `RequireAuthorization()`. Registered in `Program.cs` via `Map*Endpoints()` extension methods (Dashboard, Members, MembershipFees, Competitions, CompetitionResults, CompetitionParticipants, ExternalParticipants, plus `*Import` endpoints).
- **Services** (`Services/`): scoped services behind interfaces (`IMemberService`, `IMembershipFeeService`, `ICompetitionService`, etc.) hold the business logic; endpoints stay thin.
- **Import** (`Infrastructure/SpreadsheetParser.cs`): CSV/`.xlsx` upload parsing via CsvHelper + ClosedXML for members, competitions, and external participants.
- **Docs**: Scalar UI at `/scalar/v1` and OpenAPI at `/openapi/v1.json` (development only).

## Web

- Blazor interactive server components under `Components/` (pages: Home/Dashboard, Members, MemberDetail, Competitions, CompetitionDetail, FeeOverview, ExternalParticipants).
- **Auth** (`Auth/`): OIDC code flow against Keycloak backed by a cookie session. `TokenRefreshService` refreshes access tokens; `MemoryCacheTicketStore` keeps cookies small; data-protection keys persisted to disk so sessions survive restarts. `BearerTokenHandler` attaches the current token to `ArcheryApiClient` calls.
- Both services default to `sv-SE` culture (override with the `Locale` config key).

## Configuration

- API: `Keycloak:Realm`, `Keycloak:Audience` (both required).
- Web: `Keycloak:Realm`, `Keycloak:ClientId`, `Keycloak:ClientSecret`.
- `Locale` (default `sv-SE`); `AuthSession:RefreshMinutes` / `:IdleTimeoutMinutes` / `:CookieExpirationMinutes` (validated at startup).

## Conventions

- Endpoints delegate to services; keep business logic out of the `Map*` methods.
- Expected domain conflicts (e.g. duplicate fee for a member/year) throw `ConflictException` and are translated to HTTP `409` in the endpoint.
- Schema changes go through EF Core migrations (`dotnet ef migrations add <Name> --project aspire.ApiService`); they are applied automatically on API startup.

### Adding a new service

1. Add project reference to AppHost.
2. Call `builder.AddProject<Projects.YourProject>("name").WithHttpHealthCheck("/health")`.
3. Add `builder.AddServiceDefaults()` in the new project's `Program.cs`.
4. Reference it from dependent services via `.WithReference(...)` and `.WaitFor(...)`.
