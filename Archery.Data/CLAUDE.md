# Archery.Data

EF Core entities (`Models/`), `ArcheryDbContext` and `Migrations/`. Referenced by `aspire.ApiService`
(queries) and `aspire.MigrationService` (applies migrations). Enums come from `Archery.Client`.

- `ArcheryDbContext.cs`: fluent config, unique indexes, check constraints. `IHasParticipant` rows are
  *either* a member *or* an external participant: `ConfigureSingleParticipant` sets that up.
- Schema changes: `dotnet ef migrations add <Name> --project Archery.Data`. They are applied by the
  `migrations` resource before the API starts, never by the API itself.
- Deleting a member cascades to their fees, registrations, **competition results** and attendance. That
  is a deliberate stopgap (see the note in `ArcheryDbContext`); something more elegant, such as anonymising
  results or allowing only deactivation, may replace it.
- Entity changes usually also need `aspire.ApiService/Contracts/ContractMapping.cs` and the wire type.
