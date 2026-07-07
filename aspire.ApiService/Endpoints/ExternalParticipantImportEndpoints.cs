using aspire.ApiService.Services;

namespace aspire.ApiService.Endpoints;

public static class ExternalParticipantImportEndpoints
{
    public static IEndpointRouteBuilder MapExternalParticipantImportEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/external-participants/import", Import)
            .WithTags("ExternalParticipants")
            .DisableAntiforgery()
            .RequireAuthorization();
        return app;
    }

    static async Task<IResult> Import(IFormFile file, IExternalParticipantService svc, CancellationToken ct)
    {
        try
        {
            var result = await svc.ImportAsync(file, ct);
            return Results.Ok(result);
        }
        catch (Exception ex)
        {
            return Results.BadRequest($"Failed to parse file: {ex.Message}");
        }
    }
}
