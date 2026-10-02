namespace Archery.Mobile.Core.Abstractions;

/// <summary>
/// Signs the user in and out against Keycloak and keeps the access token current.
/// </summary>
/// <remarks>
/// The implementation lives in the MAUI head because it drives a system browser and the
/// platform's secure storage. View models only ever see this interface.
/// </remarks>
public interface IAuthService
{
    bool IsSignedIn { get; }

    /// <summary>Display name from the id token, once signed in.</summary>
    string? DisplayName { get; }

    /// <summary>Opens the system browser for an authorization-code + PKCE sign-in.</summary>
    Task<bool> SignInAsync(CancellationToken ct = default);

    Task SignOutAsync(CancellationToken ct = default);

    /// <summary>
    /// Restores a previous session from secure storage on launch, refreshing the access token
    /// if it has expired. Returns false when there is nothing usable to restore.
    /// </summary>
    Task<bool> TryRestoreSessionAsync(CancellationToken ct = default);

    /// <summary>Raised when the session ends for a reason the user did not ask for.</summary>
    event EventHandler? SessionEnded;
}
