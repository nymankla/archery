namespace Archery.Mobile.Configuration;

/// <summary>
/// Endpoints the app talks to. Bound from the embedded appsettings.json, because MAUI has no
/// Aspire service discovery: the device cannot resolve "https://apiservice".
/// </summary>
public sealed class ArcheryOptions
{
    public string Locale { get; set; } = "sv-SE";

    public ApiOptions Api { get; set; } = new();

    public KeycloakOptions Keycloak { get; set; } = new();
}

public sealed class ApiOptions
{
    /// <summary>
    /// 10.0.2.2 is the Android emulator's alias for the host loopback. Use the machine's LAN
    /// address instead when running on a physical device, and add the matching issuer to the
    /// API's Keycloak:AdditionalValidIssuers.
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;
}

public sealed class KeycloakOptions
{
    public string Authority { get; set; } = string.Empty;

    public string ClientId { get; set; } = string.Empty;

    public string RedirectUri { get; set; } = string.Empty;

    public string PostLogoutRedirectUri { get; set; } = string.Empty;

    /// <summary>offline_access is what yields a refresh token; the web client never asks for it.</summary>
    public string Scope { get; set; } = "openid profile email offline_access";
}
