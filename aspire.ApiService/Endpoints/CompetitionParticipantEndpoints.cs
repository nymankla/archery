using aspire.ApiService.Contracts;
using aspire.ApiService.Services;
using Wire = Archery.Client;

namespace aspire.ApiService.Endpoints;

public static class CompetitionParticipantEndpoints
{
    public static IEndpointRouteBuilder MapCompetitionParticipantEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/competition-participants").WithTags("CompetitionParticipants").RequireAuthorization();

        group.MapGet("/competition/{competitionId:guid}", GetByCompetition);
        group.MapPost("/", Register);
        group.MapDelete("/{id:guid}", Remove);

        return app;
    }

    static async Task<IResult> GetByCompetition(
        Guid competitionId, ICompetitionParticipantService svc, CancellationToken ct)
        => Results.Ok((await svc.GetByCompetitionAsync(competitionId, ct)).Select(p => p.ToContract()));

    static async Task<IResult> Register(
        Wire.CompetitionParticipant input, ICompetitionParticipantService svc, CancellationToken ct)
        => (await svc.RegisterAsync(input.ToEntity(), ct))
            .ToHttpResult(created => Results.Created($"/competition-participants/{created.Id}", created.ToContract()));

    static async Task<IResult> Remove(Guid id, ICompetitionParticipantService svc, CancellationToken ct)
        => EndpointResults.NoContentOrNotFound(await svc.RemoveAsync(id, ct));
}
