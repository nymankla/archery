namespace Archery.Mobile.Core.Abstractions;

/// <summary>Tokens as persisted between launches.</summary>
public sealed record StoredTokens(
    string AccessToken,
    string? RefreshToken,
    string? IdToken,
    DateTimeOffset ExpiresAt);

/// <summary>
/// Persists the session across launches. Backed by platform secure storage (Android Keystore),
/// never ordinary preferences.
/// </summary>
public interface ISecureTokenStore
{
    /// <summary>Returns null when nothing is stored, or when the store cannot be read.</summary>
    Task<StoredTokens?> LoadAsync(CancellationToken ct = default);

    Task SaveAsync(StoredTokens tokens, CancellationToken ct = default);

    Task ClearAsync(CancellationToken ct = default);
}
