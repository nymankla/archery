using System.Net;

namespace Archery.Client;

/// <summary>
/// Thrown when the API returns a non-success status, carrying the normalised
/// <see cref="ApiError"/> so callers can show the server's own message.
/// </summary>
/// <remarks>
/// Derives from <see cref="HttpRequestException"/> deliberately. The client's GET path used
/// to call <c>EnsureSuccessStatusCode()</c>, and aspire.Web relies on that throwing an
/// <see cref="HttpRequestException"/>; deriving keeps the swap behaviour-compatible there
/// while giving mobile a typed error to branch on.
/// </remarks>
public class ArcheryApiException : HttpRequestException
{
    public ArcheryApiException(ApiError error)
        : base(error.Summary, null, (HttpStatusCode)error.StatusCode)
        => Error = error;

    public ApiError Error { get; }

    /// <summary>The session is gone or was never valid — hosts use this to force re-authentication.</summary>
    public bool IsUnauthorized => Error.StatusCode is 401 or 403;
}
