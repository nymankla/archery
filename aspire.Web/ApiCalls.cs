namespace aspire.Web;

/// <summary>
/// Turns the client's exception-based failures into a value a page can show.
/// </summary>
/// <remarks>
/// Every <see cref="ArcheryApiClient"/> call throws <see cref="ArcheryApiException"/> on a
/// non-success status. An unhandled one tears down the Blazor circuit, so mutating calls from a
/// page go through <see cref="TryAsync(Task)"/> and render the returned error instead.
/// </remarks>
public static class ApiCalls
{
    /// <summary>Awaits <paramref name="call"/>; returns null on success, else the server's error.</summary>
    public static async Task<ApiError?> TryAsync(this Task call)
    {
        try
        {
            await call;
            return null;
        }
        catch (ArcheryApiException ex)
        {
            return ex.Error;
        }
    }

    /// <summary>As <see cref="TryAsync(Task)"/>, keeping the call's result on success.</summary>
    public static async Task<(T? Value, ApiError? Error)> TryAsync<T>(this Task<T> call)
    {
        try
        {
            return (await call, null);
        }
        catch (ArcheryApiException ex)
        {
            return (default, ex.Error);
        }
    }
}
