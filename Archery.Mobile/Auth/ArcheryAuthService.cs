using System.Text.Json;
using Archery.Mobile.Configuration;
using Archery.Mobile.Core.Abstractions;
using Duende.IdentityModel.Client;
using Duende.IdentityModel.OidcClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Archery.Mobile.Auth;

/// <summary>
/// Owns the Keycloak session: interactive sign-in, silent refresh, sign-out, and handing the
/// current access token to <see cref="ArcheryApiClient"/> through <see cref="IArcheryTokenProvider"/>.
/// </summary>
/// <remarks>
/// Refresh is proactive rather than a retry-on-401 inside a DelegatingHandler. Retrying a 401
/// means resending a request whose content has already been consumed, which requires cloning
/// the HttpRequestMessage — a subtle thing to get wrong in the auth path. Checking expiry before
/// each call, with a skew window, covers it without that risk.
/// </remarks>
public sealed class ArcheryAuthService : IAuthService, IArcheryTokenProvider, IDisposable
{
    // Matches the web app's AuthSession:RefreshMinutes, so both ends renew on the same cadence.
    static readonly TimeSpan RefreshSkew = TimeSpan.FromMinutes(5);

    readonly ISecureTokenStore _store;
    readonly ILogger<ArcheryAuthService> _logger;
    readonly OidcClient _oidc;
    readonly SemaphoreSlim _gate = new(1, 1);

    string? _accessToken;
    string? _refreshToken;
    string? _idToken;
    DateTimeOffset _expiresAt = DateTimeOffset.MinValue;

    public ArcheryAuthService(
        IOptions<ArcheryOptions> options,
        ISecureTokenStore store,
        ILogger<ArcheryAuthService> logger)
    {
        _store = store;
        _logger = logger;

        var keycloak = options.Value.Keycloak;

        // Discovery refuses a plain-HTTP authority by default. DiscoveryPolicy.AllowHttpOnLoopback
        // exempts localhost and 127.0.0.1, but the emulator reaches the host as 10.0.2.2, which is
        // not loopback from the device's point of view - so the relaxation has to be explicit.
        // Scoped to http authorities, which only ever occur in development; a real deployment uses
        // https and keeps the default.
        var isPlainHttp = Uri.TryCreate(keycloak.Authority, UriKind.Absolute, out var authorityUri)
            && authorityUri.Scheme == Uri.UriSchemeHttp;

        if (isPlainHttp)
            logger.LogWarning("Keycloak authority {Authority} is plain HTTP; discovery HTTPS "
                + "enforcement is disabled. This is only valid for local development.", keycloak.Authority);

        _oidc = new OidcClient(new OidcClientOptions
        {
            Authority = keycloak.Authority,
            Policy = new Policy
            {
                Discovery = new DiscoveryPolicy { RequireHttps = !isPlainHttp }
            },
            ClientId = keycloak.ClientId,
            // No ClientSecret: this is a public client. An APK cannot keep one.
            Scope = keycloak.Scope,
            RedirectUri = keycloak.RedirectUri,
            PostLogoutRedirectUri = keycloak.PostLogoutRedirectUri,
            Browser = new MauiOidcBrowser()
            // OidcClient defaults to authorization code with PKCE S256; the Keycloak client
            // requires S256, so this must not be overridden.
        });
    }

    public bool IsSignedIn => _accessToken is not null;

    public string? DisplayName { get; private set; }

    public event EventHandler? SessionEnded;

    public async Task<bool> SignInAsync(CancellationToken ct = default)
    {
        var result = await _oidc.LoginAsync(new LoginRequest(), ct);

        if (result.IsError)
        {
            if (string.Equals(result.Error, "UserCancel", StringComparison.OrdinalIgnoreCase))
                return false;

            _logger.LogError("Sign-in failed: {Error} {Description}", result.Error, result.ErrorDescription);
            throw new InvalidOperationException(result.ErrorDescription ?? result.Error);
        }

        await ApplyAsync(result.AccessToken, result.RefreshToken, result.IdentityToken,
            result.AccessTokenExpiration, ct);

        DisplayName = result.User?.FindFirst("name")?.Value
            ?? result.User?.FindFirst("preferred_username")?.Value;

        return true;
    }

    public async Task<bool> TryRestoreSessionAsync(CancellationToken ct = default)
    {
        var stored = await _store.LoadAsync(ct);
        if (stored is null)
            return false;

        _accessToken = stored.AccessToken;
        _refreshToken = stored.RefreshToken;
        _idToken = stored.IdToken;
        _expiresAt = stored.ExpiresAt;
        DisplayName = ReadNameClaim(stored.IdToken ?? stored.AccessToken);

        // Still valid, or refreshable.
        if (_expiresAt - DateTimeOffset.UtcNow > RefreshSkew)
            return true;

        return await GetAccessTokenAsync(ct) is not null;
    }

    public async ValueTask<string?> GetAccessTokenAsync(CancellationToken ct = default)
    {
        // Serialised so several screens loading at once trigger at most one refresh.
        await _gate.WaitAsync(ct);
        try
        {
            if (_accessToken is not null && _expiresAt - DateTimeOffset.UtcNow > RefreshSkew)
                return _accessToken;

            if (_refreshToken is null)
            {
                if (_accessToken is not null)
                    await EndSessionAsync(ct);
                return null;
            }

            var refreshed = await _oidc.RefreshTokenAsync(_refreshToken, cancellationToken: ct);
            if (refreshed.IsError)
            {
                _logger.LogWarning("Token refresh failed: {Error}", refreshed.Error);
                await EndSessionAsync(ct);
                return null;
            }

            await ApplyAsync(refreshed.AccessToken, refreshed.RefreshToken, refreshed.IdentityToken,
                refreshed.AccessTokenExpiration, ct);

            return _accessToken;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Offline, most likely. Hand back what we have and let the API call report the failure.
            _logger.LogWarning(ex, "Could not refresh the access token");
            return _accessToken;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SignOutAsync(CancellationToken ct = default)
    {
        try
        {
            if (_idToken is not null)
            {
                // Keycloak requires id_token_hint (or client_id) for RP-initiated logout, which
                // is why the id token is persisted alongside the others.
                await _oidc.LogoutAsync(new LogoutRequest { IdTokenHint = _idToken }, ct);
            }
        }
        catch (Exception ex)
        {
            // Local sign-out must succeed even if the browser round trip does not.
            _logger.LogWarning(ex, "Remote sign-out failed; clearing the local session anyway");
        }
        finally
        {
            await ClearAsync(ct);
        }
    }

    async Task ApplyAsync(string accessToken, string? refreshToken, string? idToken,
        DateTimeOffset expiresAt, CancellationToken ct)
    {
        _accessToken = accessToken;
        _idToken = idToken ?? _idToken;

        // Keycloak rotates refresh tokens; keep the previous one if none came back.
        _refreshToken = refreshToken ?? _refreshToken;
        _expiresAt = expiresAt;

        await _store.SaveAsync(new StoredTokens(accessToken, _refreshToken, _idToken, expiresAt), ct);
    }

    async Task EndSessionAsync(CancellationToken ct)
    {
        await ClearAsync(ct);
        SessionEnded?.Invoke(this, EventArgs.Empty);
    }

    async Task ClearAsync(CancellationToken ct)
    {
        _accessToken = null;
        _refreshToken = null;
        _idToken = null;
        _expiresAt = DateTimeOffset.MinValue;
        DisplayName = null;
        await _store.ClearAsync(ct);
    }

    /// <summary>Reads the display name out of a JWT payload.</summary>
    /// <remarks>
    /// Decoded by hand rather than with JwtSecurityTokenHandler, to avoid taking a dependency on
    /// System.IdentityModel.Tokens.Jwt purely for a display string. OidcClient already validated
    /// the token's signature during the exchange; this only reads a claim for the UI and never
    /// informs an authorisation decision.
    /// </remarks>
    string? ReadNameClaim(string jwt)
    {
        try
        {
            var parts = jwt.Split('.');
            if (parts.Length < 2)
                return null;

            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            var json = Convert.FromBase64String(payload.PadRight(
                payload.Length + (4 - payload.Length % 4) % 4, '='));

            using var doc = JsonDocument.Parse(json);
            foreach (var claim in new[] { "name", "preferred_username" })
            {
                if (doc.RootElement.TryGetProperty(claim, out var value)
                    && value.ValueKind == JsonValueKind.String)
                {
                    return value.GetString();
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not read the name claim");
            return null;
        }
    }

    public void Dispose() => _gate.Dispose();
}
