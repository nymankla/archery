# Archery Club Management

A .NET 10 [Aspire](https://learn.microsoft.com/dotnet/aspire/) distributed application for managing an archery club — members, membership fees, competitions, results, training attendance, and external (guest) participants. It exposes a secured minimal-API backend and an interactive Blazor Server frontend, orchestrated together with Redis, PostgreSQL, and Keycloak.

## Architecture

The solution is an Aspire app model that wires four runtime pieces plus shared defaults:

| Project | Type | Responsibility |
| --- | --- | --- |
| **aspire.AppHost** | Aspire orchestrator | Defines the resource graph (Redis, PostgreSQL, Keycloak, API, Web). No business logic. |
| **aspire.ApiService** | ASP.NET Core Web API | Domain logic, EF Core persistence, minimal-API endpoints, spreadsheet import. Secured with Keycloak JWT bearer. |
| **aspire.Web** | Blazor interactive server | UI for the club. Authenticates users via Keycloak OIDC, calls the API with a bearer token, caches output in Redis. |
| **aspire.ServiceDefaults** | Shared library | Cross-cutting wiring applied in every service: OpenTelemetry (OTLP), service discovery, HTTP resilience, `/health` + `/alive` endpoints. |
| **Tests** | xUnit | Integration tests spinning up the full AppHost in-process (`Aspire.Hosting.Testing`), including Postgres persistence checks. |
| **UnitTests** | xUnit | Fast unit tests for services (dashboard stats, membership fee logic). |

### Resource graph

The AppHost ([AppHost.cs](aspire.AppHost/AppHost.cs)) provisions:

- **Redis** (`cache`) — output caching for the Web frontend. Persistent container with a data volume.
- **PostgreSQL** (`postgres` → database `db`) — primary data store. Persistent container with a data volume.
- **Keycloak** (`keycloak`, port 8080) — identity provider. Persistent container, realm imported from the `keycloak/` config on startup.

Dependency flow:

```
Redis ─────────────┐
                   ▼
PostgreSQL ──► ApiService ──► Web (external HTTP)
                   ▲              │
Keycloak ──────────┴──────────────┘
```

`WithReference(...)` + `WaitFor(...)` ensure each service starts only once its dependencies are healthy. Inter-service calls use Aspire service discovery (e.g. the Web client targets `https://apiservice`, Keycloak is resolved by service name) — no hardcoded URLs.

## Domain model

Persisted with EF Core (Npgsql) via `ArcheryDbContext` ([ArcheryDbContext.cs](aspire.ApiService/Data/ArcheryDbContext.cs)); schema is created/updated by migrations applied automatically at API startup.

- **Member** — club member: contact details, personnummer (unique), date of birth, join date, active flag, preferred bow class.
- **MembershipFee** — a member's fee for a given year, with amount, due date, and status (`Unpaid` / `Paid` / `Partial`). Unique per member + year.
- **Competition** — an event with date, location, round type, and type (`Indoor` / `Outdoor` / `ThreeD` / `Field`).
- **CompetitionResult** — a score for a competition, tied to *either* a member *or* an external participant (enforced by a DB check constraint), classified by bow class, age class, and gender.
- **CompetitionParticipant** — registration/enrollment of a member or external participant into a competition.
- **TrainingSession** — a training date with optional notes.
- **TrainingAttendance** — a member's or external participant's attendance at a training session (same either/or check constraint as competition results), unique per session + participant.
- **ExternalParticipant** — a guest competitor from another club (not a member).

Deleting a member or external participant cascades to their competition results, competition registrations, and training attendance records — there's no way to keep those rows once the participant is gone.

Enums used across the model: `BowClass` (Recurve, Compound, Barebow, Traditional), `AgeClass` (Cadet, Junior, Senior, Master), `Gender`, `FeeStatus`, `CompetitionType`.

## API surface

All endpoints require authorization (Keycloak JWT bearer) and are grouped by tag. In development, interactive docs are served via [Scalar](https://scalar.com) at `/scalar/v1` (OpenAPI at `/openapi/v1.json`).

| Group | Routes |
| --- | --- |
| Dashboard | `GET /dashboard` — aggregate club statistics (see below) |
| Members | `GET/POST /members`, `GET/PUT/DELETE /members/{id}`, `POST /members/import` (CSV/xlsx), `GET /members/export` (CSV/xlsx) |
| Membership fees | `GET /membership-fees`, `GET /membership-fees/overview`, `GET /membership-fees/member/{memberId}`, `POST /membership-fees`, `POST /membership-fees/bulk`, `GET/PUT/DELETE /membership-fees/{id}` |
| Competitions | `GET/POST /competitions`, `GET/PUT/DELETE /competitions/{id}`, `POST /competitions/import` |
| Competition results | CRUD under `/…` result endpoints |
| Competition participants | CRUD under `/…` participant endpoints |
| Training attendance | `GET /training-attendance/dates`, `GET/PUT /training-attendance/by-date`, `GET /training-attendance/by-date/export` (CSV/xlsx) |
| External participants | CRUD + `POST /…/import` |

### Dashboard

`GET /dashboard` returns a single `DashboardStats` snapshot (computed in [DashboardService.cs](aspire.ApiService/Services/DashboardService.cs)) for the **current year**, giving the frontend everything it needs for the home page in one call:

- **Members** — active count, inactive count, and members who joined this year.
- **Fees** — counts of `Paid` / `Unpaid` / `Partial` fees for the year plus a `NoFee` count (active members with no fee record yet), total amount collected, total outstanding, and a collection-rate percentage (`Paid` fees ÷ active members).
- **Competitions** — number of competitions this year, count of upcoming (future-dated) competitions, and the next upcoming competition (name, date, location) if any.
- **Top scorers** — the top 5 member results this year by total score (excluding disqualified entries), each with member name, bow class, score, and competition name.
- **Recent competitions** — the 3 most recent past competitions with name, date, location, type, and participant count.

**Bulk, import & export features:**
- **Spreadsheet import** — members, competitions, and external participants can be imported from CSV or `.xlsx` uploads. Parsing is handled by `SpreadsheetParser` ([SpreadsheetParser.cs](aspire.ApiService/Infrastructure/SpreadsheetParser.cs)) using CsvHelper and ClosedXML.
- **Spreadsheet export** — members and training attendance for a date can be exported as CSV or `.xlsx` via `SpreadsheetWriter` ([SpreadsheetWriter.cs](aspire.ApiService/Infrastructure/SpreadsheetWriter.cs)), the generic counterpart to the parser. Since exported values (names, addresses, notes, etc.) come from user-editable data, `SpreadsheetWriter` guards against CSV/formula injection by prefixing any cell that starts with a formula-trigger character (`=`, `+`, `-`, `@`, tab, CR) with an apostrophe, so spreadsheet apps render it as text instead of evaluating it as a formula when the file is opened.
- **Bulk fee creation** — `POST /membership-fees/bulk` generates fees for many members at once, optionally filtered by age.
- Conflicts (e.g. duplicate fee for a member/year, duplicate personnummer) surface as HTTP `409`/`400` with validation errors.

## Web frontend

A Blazor interactive server app ([aspire.Web](aspire.Web/)) with pages for the Dashboard, Members (list + detail), Competitions (list + detail), Fees overview, Training Attendance (record) and Training History (view + export), and External Participants.

File downloads (e.g. the training attendance CSV/Excel export) go through `FileDownloader` ([FileDownloader.cs](aspire.Web/FileDownloader.cs)), a small reusable JS-interop helper — Blazor Server has no direct client filesystem access, so it fetches the file server-side and hands the browser a Blob to save.

Authentication uses the OpenID Connect code flow against Keycloak, backed by a cookie session:
- Cookie + OIDC schemes, with tokens saved and refreshed via `TokenRefreshService`.
- A server-side ticket store (`MemoryCacheTicketStore`) keeps cookies small.
- Data-protection keys are persisted to disk so sessions survive restarts.
- `ArcheryApiClient` calls the API through a `BearerTokenHandler` that attaches the current access token.
- Idle-timeout and refresh behavior are configurable via `AuthSession` options.

Both services default to the `sv-SE` culture (override with the `Locale` config key).

## Getting started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- A container runtime (Docker Desktop or Podman) — required for the Redis, PostgreSQL, and Keycloak containers.
- The [Aspire CLI / workload](https://learn.microsoft.com/dotnet/aspire/fundamentals/setup-tooling) (optional but convenient).

### Run

```bash
# Start the whole app: AppHost boots Redis, PostgreSQL, Keycloak, the API, and the Web frontend
dotnet run --project aspire.AppHost
```

The Aspire dashboard URL is printed in the terminal (typically `https://localhost:17034`); from there you can open the Web frontend, the API's Scalar docs, and view logs/traces/metrics for every resource.

On first run the API applies EF Core migrations automatically, and Keycloak imports its realm from the `keycloak/` folder.

### Publish and deploy to Docker

This AppHost already includes a Docker Compose deployment environment (`production`) in [AppHost.cs](aspire.AppHost/AppHost.cs).

```bash
# Preview deployment pipeline steps
aspire publish --list-steps
aspire deploy --list-steps

# Generate Docker Compose deployment artifacts
aspire publish -o aspire-output/docker

# Deploy with Docker Compose using the configured Aspire environment
aspire deploy
```

The generated artifacts are written under `aspire-output/docker` (including Docker Compose files and environment templates).

### Build & test

```bash
# Build the full solution
dotnet build Archery.sln

# Run all tests (unit + integration)
dotnet test Archery.sln

# Fast unit tests only
dotnet test UnitTests

# A single integration test class
dotnet test Tests --filter "FullyQualifiedName~WebTests"
```

> The integration tests in `Tests` start the full AppHost in-process and therefore require a running container runtime.

## Configuration

Key settings (via `appsettings*.json`, user secrets, or environment variables):

- `Keycloak:Realm`, `Keycloak:Audience` — required by the API.
- `Keycloak:Realm`, `Keycloak:ClientId`, `Keycloak:ClientSecret` — required by the Web frontend.
- `Locale` — thread/UI culture (default `sv-SE`).
- `AuthSession:RefreshMinutes`, `AuthSession:IdleTimeoutMinutes`, `AuthSession:CookieExpirationMinutes` — Web session lifetimes (validated at startup).

## Tech stack

.NET 10 · Aspire 13.4 · ASP.NET Core Minimal APIs · Blazor (interactive server) · Entity Framework Core + Npgsql (PostgreSQL) · Redis output caching · Keycloak (OIDC / JWT) · OpenTelemetry · Scalar/OpenAPI · CsvHelper + ClosedXML · xUnit.
