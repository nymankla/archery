using aspire.ApiService.Infrastructure;

namespace aspire.ApiService.Endpoints;

/// <summary>The one place a service outcome becomes an HTTP status.</summary>
static class EndpointResults
{
    /// <summary>Validation failures use the API's single error shape: <c>{"errors":[...]}</c>.</summary>
    public static IResult Errors(IEnumerable<string> errors) => Results.BadRequest(new { errors });

    public static IResult ToHttpResult<T>(this Result<T> result, Func<T, IResult> onSuccess) => result switch
    {
        { IsSuccess: true } => onSuccess(result.Value!),
        { Kind: ErrorKind.NotFound } => Results.NotFound(),
        _ => Errors(result.Errors)
    };

    public static IResult OkOrNotFound<T>(T? value) where T : class
        => value is null ? Results.NotFound() : Results.Ok(value);

    public static IResult NoContentOrNotFound(bool found)
        => found ? Results.NoContent() : Results.NotFound();
}
