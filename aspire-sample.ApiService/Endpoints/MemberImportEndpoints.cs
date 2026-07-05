using aspire_sample.ApiService.Services;

namespace aspire_sample.ApiService.Endpoints;

public static class MemberImportEndpoints
{
    public static IEndpointRouteBuilder MapMemberImportEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/members/import", Import)
            .WithTags("Members")
            .DisableAntiforgery()
            .RequireAuthorization();
        return app;
    }

    static async Task<IResult> Import(IFormFile file, IMemberService svc, CancellationToken ct)
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
