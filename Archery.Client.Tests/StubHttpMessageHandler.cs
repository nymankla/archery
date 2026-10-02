using System.Net;
using System.Text;

namespace Archery.Client.Tests;

/// <summary>
/// Captures outgoing requests and returns canned responses, so client behaviour can be
/// asserted without a server. Hand-rolled because the repo uses no mocking library.
/// </summary>
public sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> respond)
    : HttpMessageHandler
{
    public List<HttpRequestMessage> Requests { get; } = [];

    /// <summary>Bodies captured at send time — the request's content is disposed afterwards.</summary>
    public List<string> RequestBodies { get; } = [];

    public HttpRequestMessage LastRequest => Requests[^1];
    public string LastBody => RequestBodies[^1];

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        RequestBodies.Add(request.Content is null
            ? string.Empty
            : await request.Content.ReadAsStringAsync(cancellationToken));

        return respond(request);
    }

    public static StubHttpMessageHandler Json(string json, HttpStatusCode status = HttpStatusCode.OK) =>
        new(_ => new HttpResponseMessage(status)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });

    public static StubHttpMessageHandler Status(HttpStatusCode status, string body = "", string contentType = "application/json") =>
        new(_ => new HttpResponseMessage(status)
        {
            Content = new StringContent(body, Encoding.UTF8, contentType)
        });
}

/// <summary>A token provider with a fixed answer.</summary>
public sealed class StubTokenProvider(string? token) : IArcheryTokenProvider
{
    public int CallCount { get; private set; }

    public ValueTask<string?> GetAccessTokenAsync(CancellationToken ct = default)
    {
        CallCount++;
        return ValueTask.FromResult(token);
    }
}
