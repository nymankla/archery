using aspire_sample.ApiService.Services;

namespace aspire_sample.ApiService.Endpoints;

public static class CompetitionImportEndpoints
{
    public static IEndpointRouteBuilder MapCompetitionImportEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/competitions/import", Import)
            .WithTags("Competitions")
            .DisableAntiforgery()
            .RequireAuthorization();
        return app;
    }

    static async Task<IResult> Import(IFormFile file, ICompetitionService svc, CancellationToken ct)
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
