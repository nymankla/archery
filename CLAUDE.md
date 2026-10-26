# CLAUDE.md

Archery club management (members, fees, competitions, results, guests). .NET 11 Aspire app: minimal-API
backend, Blazor Server frontend, Redis/PostgreSQL/Keycloak, plus a MAUI Android client. Project-specific
notes live in each project's own CLAUDE.md.

## Commands

```bash
dotnet run --project aspire.AppHost                     # whole stack; dashboard URL in output (~https://localhost:17034)
dotnet build Archery.sln
dotnet test Archery.sln                                 # unit + integration; integration needs Docker/Podman
dotnet test UnitTests                                   # fast, no containers
dotnet test Tests --filter "FullyQualifiedName~WebTests"
dotnet build Archery.Mobile -f net11.0-android -t:Run   # needs maui-android workload + JAVA_HOME=JDK 21
aspire publish | aspire deploy
```

`Archery.Mobile` has no `Build.0` entries in `Archery.sln`, so the solution builds without the MAUI workload (CI).

## Projects

- **aspire.AppHost**: resource graph only. Redis `cache`, Postgres `postgres`→`db`, Keycloak `keycloak`
  (persistent volumes) → `apiservice` → `webfrontend`.
- **aspire.ApiService**: minimal API, EF Core/Npgsql, Keycloak JWT bearer.
- **aspire.Web**: Blazor interactive server, Keycloak OIDC, Redis output cache.
- **aspire.ServiceDefaults**: OTel, service discovery, HTTP resilience, `/health` + `/alive`.
- **Archery.Client** (`net11.0`): wire models + typed `ArcheryApiClient`, shared by Web and Mobile.
- **Archery.Mobile.Core** (`net11.0`): mobile ViewModels + platform abstractions; no MAUI reference.
- **Archery.Mobile** (`net11.0-android`): MAUI head.
- **Tests**: integration, boots the full AppHost via `Aspire.Hosting.Testing`. **UnitTests**: services.
  **Archery.Client.Tests**: contract tests over a stubbed `HttpMessageHandler`. **Archery.Mobile.Core.Tests**: ViewModels.

Services find each other via Aspire service discovery (`https://apiservice`, `keycloak`), never hardcoded
URLs. Culture defaults to `sv-SE` (`Locale` config key).

## Cross-cutting rules

- Enums cross the wire as **integers** (no `JsonStringEnumConverter`): never reorder `aspire.ApiService/Models/Enums.cs`.
- Expected domain conflicts return `400 {"errors":[...]}` via `Result<T>` failures. Nothing returns 409
  (`ConflictException` exists but is never thrown).

## .NET 11 RC workarounds (remove on .NET 11 GA / next Aspire)

Targets .NET 11 RC2 (SDK pinned in `global.json`); Aspire 13.6 is built against .NET 10.

- `aspire.ApiService` references `Npgsql.EntityFrameworkCore.PostgreSQL` directly, or Aspire resolves the
  EF Core 10 provider. Keep it on the EF Core line.
- `aspire.Web` calls `builder.Services.AddOutputCache()` after `AddRedisOutputCache`; without it the
  middleware can't resolve `IOutputCachePolicyProvider` and the frontend crashes on startup.
