using aspire.ApiService.Contracts;
using aspire.ApiService.Services;
using Wire = Archery.Client;

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
        => Results.Ok((await svc.GetByCompetitionAsync(competitionId, ct)).Select(r => r.ToContract()));

    static async Task<IResult> GetById(Guid id, ICompetitionResultService svc, CancellationToken ct)
        => EndpointResults.OkOrNotFound((await svc.GetByIdAsync(id, ct))?.ToContract());

    static async Task<IResult> Create(Wire.CompetitionResult input, ICompetitionResultService svc, CancellationToken ct)
        => (await svc.CreateAsync(input.ToEntity(), ct))
            .ToHttpResult(created => Results.Created($"/competition-results/{created.Id}", created.ToContract()));

    static async Task<IResult> Update(Guid id, Wire.CompetitionResult input, ICompetitionResultService svc, CancellationToken ct)
        => (await svc.UpdateAsync(id, input.ToEntity(), ct))
            .ToHttpResult(updated => Results.Ok(updated.ToContract()));

    static async Task<IResult> Delete(Guid id, ICompetitionResultService svc, CancellationToken ct)
        => EndpointResults.NoContentOrNotFound(await svc.DeleteAsync(id, ct));
}
