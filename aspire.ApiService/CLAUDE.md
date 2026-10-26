# aspire.ApiService

- Entities, `ArcheryDbContext` and migrations live in `Archery.Data` (see its CLAUDE.md). The API never
  migrates; the `migrations` resource does, and the API starts only after it completed.
- Endpoints **never bind or return entities** (over-posting: EF inserts any posted navigation graph).
  Bind `Archery.Client` types and map with `Contracts/ContractMapping.cs` (`ToEntity` copies scalars only).
  A new entity field needs adding there too.
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
