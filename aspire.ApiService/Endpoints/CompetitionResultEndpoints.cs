using aspire.ApiService.Models;
using aspire.ApiService.Services;
using aspire.ApiService.Infrastructure;

namespace aspire.ApiService.Endpoints;

public static class CompetitionResultEndpoints
{
    public static IEndpointRouteBuilder MapCompetitionResultEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/competition-results").WithTags("CompetitionResults").RequireAuthorization();

        group.MapGet("/competition/{competitionId:guid}", GetByCompetition);
        group.MapGet("/{id:guid}", GetById);
        group.MapPost("/", Create);
        group.MapPut("/{id:guid}", Update);
        group.MapDelete("/{id:guid}", Delete);

        return app;
    }

    static async Task<IResult> GetByCompetition(
        Guid competitionId, ICompetitionResultService svc, CancellationToken ct)
        => Results.Ok(await svc.GetByCompetitionAsync(competitionId, ct));

    static async Task<IResult> GetById(Guid id, ICompetitionResultService svc, CancellationToken ct)
        => await svc.GetByIdAsync(id, ct) is { } r ? Results.Ok(r) : Results.NotFound();

    static async Task<IResult> Create(CompetitionResult input, ICompetitionResultService svc, CancellationToken ct)
    {
        var result = await svc.CreateAsync(input, ct);
        if (result.IsSuccess)
        {
            var created = result.Value!;
            return Results.Created($"/competition-results/{created.Id}", created);
        }

        return Results.BadRequest(new { errors = result.Errors });
    }

    static async Task<IResult> Update(Guid id, CompetitionResult input, ICompetitionResultService svc, CancellationToken ct)
    {
        var result = await svc.UpdateAsync(id, input, ct);
        if (result.IsSuccess)
        {
            return Results.Ok(result.Value!);
        }

        return result.Errors.Contains("Competition result not found.")
            ? Results.NotFound()
            : Results.BadRequest(new { errors = result.Errors });
    }

    static async Task<IResult> Delete(Guid id, ICompetitionResultService svc, CancellationToken ct)
        => await svc.DeleteAsync(id, ct) ? Results.NoContent() : Results.NotFound();
}
