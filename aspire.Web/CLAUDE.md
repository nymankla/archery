# aspire.Web

- Pages under `Components/`: Home/Dashboard, Members, MemberDetail, Competitions, CompetitionDetail,
  FeeOverview, ExternalParticipants.
- `Auth/`: OIDC code flow + cookie session. `TokenRefreshService` refreshes tokens, `MemoryCacheTicketStore`
  keeps cookies small, `BearerTokenHandler` attaches the token to `ArcheryApiClient`. Data-protection keys
  persist to `.dp-keys` so sessions survive restarts.
- Config: `Keycloak:Realm`, `Keycloak:ClientId`, `Keycloak:ClientSecret`;
  `AuthSession:RefreshMinutes` / `IdleTimeoutMinutes` / `CookieExpirationMinutes` (validated on start).
