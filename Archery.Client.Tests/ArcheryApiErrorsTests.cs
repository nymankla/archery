using System.Net;
using System.Text;

namespace Archery.Client.Tests;

/// <summary>
/// The API emits four different error envelopes. These pin each one, including the
/// ProblemDetails case that the three copied-and-pasted razor helpers got wrong.
/// </summary>
public class ArcheryApiErrorsTests
{
    static HttpResponseMessage Response(HttpStatusCode status, string body,
        string contentType = "application/json") =>
        new(status) { Content = new StringContent(body, Encoding.UTF8, contentType) };

    [Fact]
    public async Task ReadsOurValidationShape()
    {
        using var response = Response(HttpStatusCode.BadRequest,
            """{"errors":["First name is required.","Personnummer is invalid."]}""");

        var error = await ArcheryApiErrors.ReadAsync(response);

        Assert.Equal(400, error.StatusCode);
        Assert.Equal(["First name is required.", "Personnummer is invalid."], error.Messages);
        Assert.Equal("First name is required. Personnummer is invalid.", error.Summary);
    }

    // This is the case the old razor helper could not handle: ProblemDetails puts errors in a
    // dictionary keyed by field, not an array, so deserializing into a List<string> threw and
    // the user saw "Failed to parse server validation response" instead of the real problem.
    [Fact]
    public async Task ReadsProblemDetailsValidationDictionary()
    {
        using var response = Response(HttpStatusCode.BadRequest,
            """
            {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1",
             "title":"One or more validation errors occurred.",
             "status":400,
             "errors":{"FirstName":["The FirstName field is required."],
                       "Year":["The field Year must be between 2000 and 2100."]}}
            """);

        var error = await ArcheryApiErrors.ReadAsync(response);

        Assert.Equal(
            ["The FirstName field is required.", "The field Year must be between 2000 and 2100."],
            error.Messages);
    }

    [Fact]
    public async Task FallsBackToProblemDetailsTitleWhenNoErrorsCollection()
    {
        using var response = Response(HttpStatusCode.InternalServerError,
            """{"title":"An error occurred while processing your request.","status":500}""");

        var error = await ArcheryApiErrors.ReadAsync(response);

        Assert.Equal(["An error occurred while processing your request."], error.Messages);
    }

    [Fact]
    public async Task PrefersDetailOverTitle()
    {
        using var response = Response(HttpStatusCode.BadRequest,
            """{"title":"Bad Request","detail":"The competition date is in the past."}""");

        var error = await ArcheryApiErrors.ReadAsync(response);

        Assert.Equal(["The competition date is in the past."], error.Messages);
    }

    [Fact]
    public async Task ReadsMessageShape()
    {
        using var response = Response(HttpStatusCode.Conflict, """{"message":"Already registered."}""");

        var error = await ArcheryApiErrors.ReadAsync(response);

        Assert.Equal(["Already registered."], error.Messages);
    }

    // The import endpoints return a bare JSON string on a parse failure.
    [Fact]
    public async Task ReadsBareJsonString()
    {
        using var response = Response(HttpStatusCode.BadRequest, "\"Unsupported file type.\"");

        var error = await ArcheryApiErrors.ReadAsync(response);

        Assert.Equal(["Unsupported file type."], error.Messages);
    }

    [Fact]
    public async Task ReadsNonJsonTextAsIs()
    {
        using var response = Response(HttpStatusCode.BadGateway,
            "upstream connect error", "text/plain");

        var error = await ArcheryApiErrors.ReadAsync(response);

        Assert.Equal(["upstream connect error"], error.Messages);
    }

    [Fact]
    public async Task ToleratesPascalCaseKeys()
    {
        using var response = Response(HttpStatusCode.BadRequest, """{"Errors":["Nope."]}""");

        var error = await ArcheryApiErrors.ReadAsync(response);

        Assert.Equal(["Nope."], error.Messages);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, "Your session has expired. Please sign in again.")]
    [InlineData(HttpStatusCode.Forbidden, "You are not permitted to do that.")]
    [InlineData(HttpStatusCode.NotFound, "Not found.")]
    [InlineData(HttpStatusCode.BadRequest, "The server rejected the request.")]
    [InlineData(HttpStatusCode.InternalServerError, "The server had a problem. Please try again.")]
    public async Task FallsBackToStatusMessageForEmptyBody(HttpStatusCode status, string expected)
    {
        using var response = Response(status, string.Empty);

        var error = await ArcheryApiErrors.ReadAsync(response);

        Assert.Equal([expected], error.Messages);
    }

    [Fact]
    public async Task FallsBackWhenJsonIsMalformed()
    {
        using var response = Response(HttpStatusCode.BadRequest, "{\"errors\":[");

        var error = await ArcheryApiErrors.ReadAsync(response);

        // Surfaces the raw text rather than throwing.
        Assert.Single(error.Messages);
    }

    [Fact]
    public async Task EmptyErrorsArrayFallsBackToStatusMessage()
    {
        using var response = Response(HttpStatusCode.BadRequest, """{"errors":[]}""");

        var error = await ArcheryApiErrors.ReadAsync(response);

        Assert.Equal(["The server rejected the request."], error.Messages);
    }

    [Fact]
    public void SummaryIsNeverEmpty()
    {
        var error = new ApiError(418, []);

        Assert.Equal("Request failed (418).", error.Summary);
    }
}
