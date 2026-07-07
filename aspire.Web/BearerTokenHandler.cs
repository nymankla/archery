using System.Net.Http.Headers;

namespace aspire.Web;

public class BearerTokenHandler(AccessTokenProvider tokenProvider) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        if (tokenProvider.AccessToken is { } token)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return base.SendAsync(request, ct);
    }
}
