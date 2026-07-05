using aspire_sample.ApiService.Models;
using aspire_sample.ApiService.Services;

namespace aspire_sample.ApiService.Endpoints;

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
        try
        {
            var created = await svc.CreateAsync(input, ct);
            return Results.Created($"/competition-results/{created.Id}", created);
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(ex.Message);
        }
    }

    static async Task<IResult> Update(Guid id, CompetitionResult input, ICompetitionResultService svc, CancellationToken ct)
    {
        try
        {
            return await svc.UpdateAsync(id, input, ct) is { } r ? Results.Ok(r) : Results.NotFound();
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(ex.Message);
        }
    }

    static async Task<IResult> Delete(Guid id, ICompetitionResultService svc, CancellationToken ct)
        => await svc.DeleteAsync(id, ct) ? Results.NoContent() : Results.NotFound();
}
