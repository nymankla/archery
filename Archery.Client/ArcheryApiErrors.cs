using System.Text.Json;

namespace Archery.Client;

/// <summary>A non-success API response, reduced to a status code and displayable messages.</summary>
public sealed record ApiError(int StatusCode, IReadOnlyList<string> Messages)
{
    public string Summary => Messages.Count > 0
        ? string.Join(" ", Messages)
        : $"Request failed ({StatusCode}).";
}

/// <summary>
/// Normalises the API's error responses into one shape.
/// </summary>
/// <remarks>
/// The API emits four different error envelopes and this is the only place that knows it:
/// validation failures return <c>{"errors":["..."]}</c>, unhandled exceptions return
/// ProblemDetails (where <c>errors</c> is a dictionary, not an array), the import endpoints
/// return a bare JSON string, and some paths return <c>{"message":"..."}</c>.
///
/// The body is read once into a string and probed with <see cref="JsonDocument"/>. Reading
/// once matters: the three copies this replaced called <c>ReadFromJsonAsync</c> up to three
/// times on the same response, which only worked because HttpClient buffers by default, and
/// threw on ProblemDetails (dictionary vs. array) — reporting a parse failure instead of the
/// actual server error. Using JsonDocument rather than a typed deserialise also keeps this
/// trim- and AOT-safe.
/// </remarks>
public static class ArcheryApiErrors
{
    public static async Task<ApiError> ReadAsync(HttpResponseMessage response, CancellationToken ct = default)
    {
        var status = (int)response.StatusCode;

        string body;
        try
        {
            body = await response.Content.ReadAsStringAsync(ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new ApiError(status, [FallbackFor(status)]);
        }

        var messages = Parse(body);
        return new ApiError(status, messages.Count > 0 ? messages : [FallbackFor(status)]);
    }

    static List<string> Parse(string body)
    {
        var result = new List<string>();
        if (string.IsNullOrWhiteSpace(body))
            return result;

        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(body);
        }
        catch (JsonException)
        {
            // Not JSON at all — surface the raw text, which is better than nothing.
            result.Add(body.Trim());
            return result;
        }

        using (doc)
        {
            var root = doc.RootElement;

            // The import endpoints return a bare JSON string on parse failure.
            if (root.ValueKind == JsonValueKind.String)
            {
                if (root.GetString() is { } single && !string.IsNullOrWhiteSpace(single))
                    result.Add(single);
                return result;
            }

            if (root.ValueKind != JsonValueKind.Object)
                return result;

            if (TryGetProperty(root, "errors", out var errors))
            {
                if (errors.ValueKind == JsonValueKind.Array)
                {
                    // Our own validation shape: {"errors":["...","..."]}
                    AddStrings(errors, result);
                }
                else if (errors.ValueKind == JsonValueKind.Object)
                {
                    // ProblemDetails shape: {"errors":{"Field":["..."]}}
                    foreach (var field in errors.EnumerateObject())
                        AddStrings(field.Value, result);
                }
            }

            if (result.Count > 0)
                return result;

            // {"message":...}, then ProblemDetails' detail/title.
            foreach (var key in new[] { "message", "detail", "title" })
            {
                if (TryGetProperty(root, key, out var value)
                    && value.ValueKind == JsonValueKind.String
                    && value.GetString() is { } text
                    && !string.IsNullOrWhiteSpace(text))
                {
                    result.Add(text);
                    return result;
                }
            }
        }

        return result;
    }

    static void AddStrings(JsonElement element, List<string> into)
    {
        if (element.ValueKind != JsonValueKind.Array)
            return;

        foreach (var item in element.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String
                && item.GetString() is { } text
                && !string.IsNullOrWhiteSpace(text))
            {
                into.Add(text);
            }
        }
    }

    /// <summary>Property lookup that tolerates camelCase or PascalCase from either serializer.</summary>
    static bool TryGetProperty(JsonElement root, string name, out JsonElement value)
    {
        foreach (var property in root.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    static string FallbackFor(int status) => status switch
    {
        400 => "The server rejected the request.",
        401 => "Your session has expired. Please sign in again.",
        403 => "You are not permitted to do that.",
        404 => "Not found.",
        409 => "That conflicts with existing data.",
        >= 500 => "The server had a problem. Please try again.",
        _ => $"Request failed ({status})."
    };
}
