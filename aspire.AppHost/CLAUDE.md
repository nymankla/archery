# aspire.AppHost

Orchestration only, no business logic. Each resource uses `WithReference(...)` + `WaitFor(...)`.
`apiservice` has `.WithExternalHttpEndpoints()` because the Android app can't use service discovery.

## Adding a service

1. Reference the project from the AppHost.
2. `builder.AddProject<Projects.X>("name").WithHttpHealthCheck("/health")`.
3. `builder.AddServiceDefaults()` in its `Program.cs`.
4. Wire dependents with `.WithReference(...)` + `.WaitFor(...)`.

## Keycloak

- HTTPS on host 8080 (container 8443, self-signed). Port 8081 exposes plain HTTP for the Android emulator,
  added only outside publish mode.
- `.WithRealmImport(...)` runs only against an **empty** store and `keycloakdata` is persistent, so editing
  `keycloak/archery-realm.json` does nothing to an existing environment. Apply changes via the admin console
  (`http://localhost:8081`, realm `master`, user `admin`, password = user secret `Parameters:keycloak-password`),
  or wipe: `docker rm -f archery-keycloak && docker volume rm keycloakdata`, then restart.
- A client with `optionalClientScopes` **must** also list `defaultClientScopes`, or `openid profile email`
  fails with `invalid_scope`.
- Users need `"realmRoles": ["default-roles-archery"]`; otherwise `offline_access` (mobile refresh token)
  fails with "Offline tokens not allowed for the user or client".
