# aspire.ApiService

- `Models/`: EF entities (`Member`, `MembershipFee`, `Competition`, `CompetitionResult`,
  `CompetitionParticipant`, `ExternalParticipant`, training). Enums come from `Archery.Client`.
- Endpoints **never bind or return entities** (over-posting: EF inserts any posted navigation graph).
  Bind `Archery.Client` types and map with `Contracts/ContractMapping.cs` (`ToEntity` copies scalars only).
  A new entity field needs adding there too.
- `Data/ArcheryDbContext.cs`: fluent config, unique indexes, check constraints (a result/participant is
  *either* a member *or* an external participant).
- Schema changes: `dotnet ef migrations add <Name> --project aspire.ApiService`; applied on startup.
- `Endpoints/`: grouped minimal APIs, all `RequireAuthorization()`, registered via `Map*Endpoints()`. Keep
  them thin; logic goes in `Services/` (scoped, behind `I*Service` interfaces, returning `Result<T>`).
  Turn outcomes into statuses only through `EndpointResults` (`ToHttpResult`, `OkOrNotFound`, ...).
- Imports: a service implements `ISpreadsheetImporter` and gets a route in `ImportEndpoints`. Unreadable
  files surface as `SpreadsheetFormatException` → 400; other exceptions go to the global handler.
- `Infrastructure/SpreadsheetParser.cs`: CSV/xlsx import (CsvHelper + ClosedXML).
- Dev only: Scalar at `/scalar/v1`, OpenAPI at `/openapi/v1.json`.

## Config

`Keycloak:Realm`, `Keycloak:Audience` (required). Dev only: `Keycloak:AdditionalValidIssuers`. Keycloak
derives `iss` from the request Host, so emulator tokens (via `10.0.2.2`) carry a different issuer and fail
with `IDX10205`, seen as a bare 401. These are accepted in addition to the metadata issuer. Set
`Microsoft.AspNetCore.Authentication` to `Debug` to see why a 401 happened.
