namespace Archery.Client;

/// <summary>
/// Supplies the bearer token for outgoing API calls. Each host implements this its own way:
/// the Blazor server reads it from the auth cookie's saved tokens, a mobile app from its
/// secure token store, refreshing ahead of expiry.
/// </summary>
/// <remarks>
/// Named <c>IArcheryTokenProvider</c> rather than <c>IAccessTokenProvider</c> on purpose:
/// aspire.Web already has a concrete <c>AccessTokenProvider</c> that is *not* an
/// implementation of this, and both names end up in scope together there.
/// </remarks>
public interface IArcheryTokenProvider
{
    ValueTask<string?> GetAccessTokenAsync(CancellationToken ct = default);
}
