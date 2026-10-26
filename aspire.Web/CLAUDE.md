# aspire.Web

- Pages under `Components/`: Home/Dashboard, Members, MemberDetail, Competitions, CompetitionDetail,
  FeeOverview, ExternalParticipants.
- `Auth/`: OIDC code flow + cookie session. `TokenRefreshService` refreshes tokens, `MemoryCacheTicketStore`
  keeps cookies small, `WebArcheryTokenProvider` supplies the token to `ArcheryApiClient`. Data-protection keys
  persist to `.dp-keys` so sessions survive restarts.
- Mutating API calls go through `.TryAsync()` (`ApiCalls.cs`) and show the returned `ApiError`; an
  unhandled `ArcheryApiException` kills the circuit. `<ErrorAlert @bind-Error=...>` is the page banner,
  `<ConfirmDialog @ref="confirm" Owner="this" />` + `confirm.Show(msg, action)` the delete prompt.
- Config: `Keycloak:Realm`, `Keycloak:ClientId`, `Keycloak:ClientSecret`;
  `AuthSession:RefreshMinutes` / `IdleTimeoutMinutes` / `CookieExpirationMinutes` (validated on start).
