# Archery.Mobile

Android-only MAUI head (XAML + Shell). iOS should need only a TFM and `Platforms/iOS/`. Covers members (with
fee view + mark-paid), training attendance/history, competitions (participants, results), external
participants. Imports, exports and the fee matrix/bulk run stay web-only (a UI choice, not a security boundary).

- Every `DataTemplate` needs `x:DataType` (compiled bindings; reflection bindings break under trimming).
  Setting `BindingContext` on an element also re-roots its other bindings; use `RelativeSource` for those.
- Auth (`Auth/`): `Duende.IdentityModel.OidcClient` over `WebAuthenticator` (Custom Tabs; WebView is
  blocked by Google). Code + PKCE against the **public** `archerymobile` client, never `archeryweb`. Tokens
  in `SecureStorage` (never `Preferences`), refreshed with a 5-minute skew. `ArcheryAuthService` implements
  `IAuthService` + `IArcheryTokenProvider`.
- `appsettings.json` is an `EmbeddedResource` (`Api:BaseUrl`, `Keycloak:Authority`, `ClientId`,
  `RedirectUri`); host reached via `10.0.2.2`. Culture forced to `sv-SE` in `MauiProgram`.
- Cleartext HTTP is **Debug-only** and scoped to `10.0.2.2`
  (`Platforms/Android/Resources/xml/network_security_config.xml`). A Debug-only `AndroidManifestOverlay`
  adds `networkSecurityConfig`, and the resource is removed in other configs. `ArcheryReleaseSmokeTest=true`
  re-adds it for local trimmed-build testing only; never ship that.
