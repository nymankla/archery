using aspire.ApiService.Models;
using aspire.ApiService.Services;
using aspire.ApiService.Infrastructure;

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
        => Results.Ok(await svc.GetByCompetitionAsync(competitionId, ct));

    static async Task<IResult> Register(
        CompetitionParticipant input, ICompetitionParticipantService svc, CancellationToken ct)
    {
        try
        {
            var created = await svc.RegisterAsync(input, ct);
            return Results.Created($"/competition-participants/{created.Id}", created);
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(ex.Message);
        }
        catch (ConflictException ex)
        {
            return Results.Conflict(new { message = ex.Message });
        }
    }

    static async Task<IResult> Remove(Guid id, ICompetitionParticipantService svc, CancellationToken ct)
        => await svc.RemoveAsync(id, ct) ? Results.NoContent() : Results.NotFound();
}
