using System.Globalization;
using Archery.Mobile.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace Archery.Mobile.Auth;

/// <summary>
/// Persists the session in platform secure storage (Android Keystore + EncryptedSharedPreferences).
/// </summary>
/// <remarks>
/// Every read is defensive. Android auto-backup can restore the encrypted preferences onto a
/// device whose Keystore no longer holds the matching key, after which decryption throws on every
/// launch. The manifest sets android:allowBackup="false" to prevent that, but a failure here is
/// still treated as "not signed in" rather than being allowed to crash the app.
/// </remarks>
public sealed class SecureStorageTokenStore(ILogger<SecureStorageTokenStore> logger) : ISecureTokenStore
{
    const string AccessKey = "archery.access_token";
    const string RefreshKey = "archery.refresh_token";
    const string IdKey = "archery.id_token";
    const string ExpiresKey = "archery.expires_at";

    public async Task<StoredTokens?> LoadAsync(CancellationToken ct = default)
    {
        try
        {
            var access = await SecureStorage.Default.GetAsync(AccessKey);
            var expiresRaw = await SecureStorage.Default.GetAsync(ExpiresKey);

            if (string.IsNullOrEmpty(access) || string.IsNullOrEmpty(expiresRaw))
                return null;

            // Round-tripped with InvariantCulture on purpose: the app runs under sv-SE.
            if (!DateTimeOffset.TryParse(expiresRaw, CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind, out var expiresAt))
                return null;

            return new StoredTokens(
                access,
                await SecureStorage.Default.GetAsync(RefreshKey),
                await SecureStorage.Default.GetAsync(IdKey),
                expiresAt);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not read the secure token store; treating as signed out");
            await ClearAsync(ct);
            return null;
        }
    }

    public async Task SaveAsync(StoredTokens tokens, CancellationToken ct = default)
    {
        try
        {
            await SecureStorage.Default.SetAsync(AccessKey, tokens.AccessToken);
            await SecureStorage.Default.SetAsync(ExpiresKey,
                tokens.ExpiresAt.ToString("O", CultureInfo.InvariantCulture));

            if (tokens.RefreshToken is not null)
                await SecureStorage.Default.SetAsync(RefreshKey, tokens.RefreshToken);

            if (tokens.IdToken is not null)
                await SecureStorage.Default.SetAsync(IdKey, tokens.IdToken);
        }
        catch (Exception ex)
        {
            // Not fatal: the session still works until the app is closed.
            logger.LogWarning(ex, "Could not persist tokens; the session will not survive a restart");
        }
    }

    public Task ClearAsync(CancellationToken ct = default)
    {
        foreach (var key in new[] { AccessKey, RefreshKey, IdKey, ExpiresKey })
        {
            try
            {
                SecureStorage.Default.Remove(key);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not remove {Key} from secure storage", key);
            }
        }

        return Task.CompletedTask;
    }
}
