namespace Archery.Mobile.Core;

/// <summary>
/// Turns the client's mutating calls into exception-based flow.
/// </summary>
/// <remarks>
/// <see cref="IArcheryApiClient"/>'s GET members already throw <see cref="ArcheryApiException"/>,
/// but the create/update/delete members return the raw <see cref="HttpResponseMessage"/> because
/// the Blazor host inspects it directly. View models should never see one, so they write
/// <c>await Api.CreateMemberAsync(m, ct).EnsureArcherySuccessAsync(ct)</c> and let
/// <c>BaseViewModel.RunAsync</c> turn any failure into a message.
/// </remarks>
public static class ApiResponseExtensions
{
    public static async Task EnsureArcherySuccessAsync(
        this Task<HttpResponseMessage> call, CancellationToken ct = default)
    {
        using var response = await call;

        if (!response.IsSuccessStatusCode)
            throw new ArcheryApiException(await ArcheryApiErrors.ReadAsync(response, ct));
    }
}
