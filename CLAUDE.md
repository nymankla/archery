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

# Build and deploy the Android app to a running emulator or device
dotnet build Archery.Mobile -f net10.0-android -t:Run
```

The AppHost launches the Aspire dashboard at the URL shown in terminal output (typically `https://localhost:17034`). A container runtime (Docker/Podman) is required — the integration tests in `Tests` boot the full AppHost in-process and also need it.

`Archery.Mobile` is **excluded from the solution build configurations** (no `Build.0` entries
in `Archery.sln`), so `dotnet build Archery.sln` works on a machine without the MAUI workload —
including CI. Build the app with the explicit command above; it needs
`dotnet workload install maui-android` and a `JAVA_HOME` pointing at JDK 21.

## Overview

.NET 10 Aspire distributed application: an **archery club management system** covering members, membership fees, competitions, results, and external (guest) participants. Secured minimal-API backend + interactive Blazor Server frontend, orchestrated with Redis, PostgreSQL, and Keycloak.

A **.NET MAUI Android client** covers the subset of that done at the range — members, training
attendance, competitions and guests. It is a client of the system, not a resource in the Aspire
graph.

## Architecture

Four runtime pieces, two shared libraries, an Android head, and four test projects:

- **aspire.AppHost** — Orchestrator only. Defines the resource graph. No business logic.
- **aspire.ApiService** — ASP.NET Core minimal-API backend. EF Core (Npgsql/PostgreSQL) persistence, domain services, spreadsheet import. Secured with Keycloak JWT bearer.
- **aspire.Web** — Blazor interactive server frontend. Keycloak OIDC login, calls the API with a bearer token, Redis output caching.
- **aspire.ServiceDefaults** — Shared extension methods applied in every service's `Program.cs`: OpenTelemetry (OTLP), service discovery, HTTP resilience (retry + circuit breaker), default `/health` + `/alive` endpoints.
- **Archery.Client** (`net10.0`) — wire models, the typed `ArcheryApiClient`, error normalisation
  and source-generated JSON. Shared by `aspire.Web` and the Android app. **No ASP.NET
  dependency** — it must never reference `aspire.ServiceDefaults`, which carries
  `<FrameworkReference Include="Microsoft.AspNetCore.App" />` and cannot load under MAUI.
- **Archery.Mobile.Core** (`net10.0`) — every mobile ViewModel plus the platform abstractions
  (`IAuthService`, `INavigationService`, `IDialogService`, …). **No MAUI reference**, which is
  what makes the ViewModels testable.
- **Archery.Mobile** (`net10.0-android`) — MAUI head: XAML views, Shell, `MauiProgram`, the
  Keycloak OIDC implementation and Android platform code.
- **Tests** — xUnit integration tests using `Aspire.Hosting.Testing` to spin up the full AppHost in-process (incl. Postgres persistence).
- **UnitTests** — fast xUnit unit tests for services (dashboard stats, membership fee logic).
- **Archery.Client.Tests** — contract tests over `ArcheryApiClient` against a stubbed
  `HttpMessageHandler`: URL construction, date formatting, bearer attachment, the error
  envelopes, and the exact JSON shape on the wire.
- **Archery.Mobile.Core.Tests** — ViewModel tests, no emulator required.

### Resource graph (AppHost.cs)

- **Redis** (`cache`) — output caching for Web.
- **PostgreSQL** (`postgres` → database `db`) — primary data store.
- **Keycloak** (`keycloak`, port 8080) — identity provider; realm imported from `keycloak/` on startup.

All three are persistent containers with data volumes. Flow: Redis + PostgreSQL + Keycloak → ApiService → Web (external HTTP), each wired with `WithReference(...)` + `WaitFor(...)`.

### Inter-service communication

Uses Aspire service discovery — no hardcoded URLs. The Web `ArcheryApiClient` targets `https://apiservice` (matching the AppHost resource name); Keycloak is resolved by service name in both projects.

Service discovery does not work off-box, so the Android app uses real URLs from its embedded
`appsettings.json` instead. `apiservice` therefore carries `.WithExternalHttpEndpoints()`.

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

## Mobile (Archery.Mobile)

Android-only MAUI app, native XAML with MVVM (CommunityToolkit.Mvvm source generators).
Structured so iOS is one extra TFM plus `Platforms/iOS/`, with no ViewModel rework.

- **Screens**: Dashboard (landing), Members (list/detail/edit, plus view + mark-paid on that
  member's fees), Training attendance, Training history, Competitions (list/detail/edit) with
  participant registration and result entry, and External participants.
- **Deliberately absent**: all import and export flows, and the year-wide fee matrix and bulk
  fee run. Those stay on the web. This is a UI choice, not a security boundary — see the note
  on realm roles below.
- **Auth** (`Auth/`): `Duende.IdentityModel.OidcClient` over MAUI `WebAuthenticator` (Chrome
  Custom Tabs; an embedded WebView is blocked by Google). Authorization code + PKCE S256
  against the public `archerymobile` Keycloak client — a mobile app cannot keep a secret, so it
  must not reuse the confidential `archeryweb` client. Tokens live in `SecureStorage`
  (Keystore-backed), never `Preferences`, and refresh proactively with a 5-minute skew.
  `ArcheryAuthService` implements both `IAuthService` and `Archery.Client`'s
  `IArcheryTokenProvider`.
- **Config**: `appsettings.json` is an `EmbeddedResource` so it travels inside the APK
  (`Api:BaseUrl`, `Keycloak:Authority`, `ClientId`, `RedirectUri`). Both the API and Keycloak
  are reached over `10.0.2.2`, the emulator's alias for the host loopback.
- Culture is forced to `sv-SE` in `MauiProgram` to match the two services.

### Mobile conventions worth knowing before editing

- **Use `IQueryAttributable`, never `[QueryProperty]`, for navigation parameters.** Shell
  applies `[QueryProperty]` via `Convert.ChangeType`, and **`Guid` does not implement
  `IConvertible`** — passing one throws *"Object must implement IConvertible"* and kills the
  process. Every parameterised page reads the dictionary directly instead.
- **An `[ObservableProperty]` handler that calls `RunAsync` is a no-op when the property is
  assigned from inside another `RunAsync`** — the outer call already set `IsBusy`, and
  `RunAsync` refuses to re-enter. Await the work directly in that case.
- **Bind numeric `Entry` fields as strings and parse explicitly** against current *and*
  invariant culture. Under `sv-SE` an `Entry` bound straight to an `int` silently drops input
  the culture cannot parse.
- `Archery.Client` is `IsTrimmable` + `IsAotCompatible` with the trim analyzer on. Serialization
  goes through `ArcheryJsonContext`, so **every new wire type needs a `[JsonSerializable]`
  entry** and every client call passes a `JsonTypeInfo`. Reflection-based JSON builds clean on
  the desktop and fails at runtime in a trimmed Release APK.
- Every `DataTemplate` needs an `x:DataType`: compiled bindings are enforced, because
  reflection-based bindings break silently under trimming.
- **Enums cross the wire as integers** — the API registers no `JsonStringEnumConverter`, so the
  declaration order in `Models/Enums.cs` is load-bearing. Do not reorder them.

### Cleartext HTTP is Debug-only

The dev stack speaks plain HTTP and Android forbids that by default. The exemption is scoped to
`10.0.2.2` in `Platforms/Android/Resources/xml/network_security_config.xml`, and it **cannot
reach a Release package**: the base manifest does not declare `networkSecurityConfig`, a
Debug-only `AndroidManifestOverlay` adds it, and the config resource is removed from every other
configuration. `ArcheryReleaseSmokeTest=true` re-adds it so a trimmed build can be exercised
against the local stack — a test harness switch only, never for a package that leaves the
machine.

## Configuration

- API: `Keycloak:Realm`, `Keycloak:Audience` (both required).
- Web: `Keycloak:Realm`, `Keycloak:ClientId`, `Keycloak:ClientSecret`.
- `Locale` (default `sv-SE`); `AuthSession:RefreshMinutes` / `:IdleTimeoutMinutes` / `:CookieExpirationMinutes` (validated at startup).
- API, development only: `Keycloak:AdditionalValidIssuers`. Keycloak derives the `iss` claim
  from the Host header of the token request, so a token fetched by the Android emulator over
  `10.0.2.2` carries a different issuer than the one in the discovery document the API reads.
  Without this, such tokens fail validation with `IDX10205`, which surfaces as a bare 401 with
  an empty body — set `Microsoft.AspNetCore.Authentication` to `Debug` to see the real reason.
  These issuers are accepted *in addition to* the metadata issuer, so the web app is unaffected.

### Editing the Keycloak realm

`.WithRealmImport(...)` only runs against an empty Keycloak store, and the `keycloakdata`
volume is persistent. **Editing `aspire.AppHost/keycloak/archery-realm.json` therefore has no
effect on an existing dev environment.** Apply the change one of two ways:

- Through the Keycloak admin console or admin API (`http://localhost:8081`, realm `master`,
  user `admin`, password from the AppHost's `Parameters:keycloak-password` user secret).
  Non-destructive; preferred.
- Or recreate the store: `docker rm -f archery-keycloak && docker volume rm keycloakdata`,
  then restart the AppHost. This wipes anything not reproduced by the realm JSON.

Keycloak serves **HTTPS** on host port 8080 (container 8443) with a self-signed certificate.
Port 8081 publishes the container's plain-HTTP listener for the Android emulator, which cannot
validate that certificate; it is added only outside publish mode.

Two things about the realm JSON that are easy to get wrong:

- A client that lists `optionalClientScopes` **must** also list `defaultClientScopes`.
  Supplying only the optional set leaves the client with no default scopes at all, and
  authorization then fails with `invalid_scope` for `openid profile email`.
- A user needs `"realmRoles": ["default-roles-archery"]`. Without it the import creates the
  user with no realm roles whatsoever, and anything requesting `offline_access` fails the
  token exchange with *"Offline tokens not allowed for the user or client"* — `offline_access`
  is a realm role as well as a client scope. The web app never hit this because it does not
  request that scope; the mobile client does, to get a refresh token.

## Conventions

- Endpoints delegate to services; keep business logic out of the `Map*` methods.
- Expected domain conflicts (e.g. duplicate fee for a member/year) surface as HTTP `400` with
  `{"errors":[...]}`, from the services' `Result<T>` failure path. Note `ConflictException` is
  declared in `Infrastructure/PersistenceExceptionExtensions.cs` but is never thrown — nothing
  in the API returns `409` today.
- Schema changes go through EF Core migrations (`dotnet ef migrations add <Name> --project aspire.ApiService`); they are applied automatically on API startup.

### Adding a new service

1. Add project reference to AppHost.
2. Call `builder.AddProject<Projects.YourProject>("name").WithHttpHealthCheck("/health")`.
3. Add `builder.AddServiceDefaults()` in the new project's `Program.cs`.
4. Reference it from dependent services via `.WithReference(...)` and `.WaitFor(...)`.
