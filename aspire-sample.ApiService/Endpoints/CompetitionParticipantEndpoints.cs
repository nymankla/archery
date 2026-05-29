using aspire_sample.ApiService.Data;
using aspire_sample.ApiService.Models;
using Microsoft.EntityFrameworkCore;

namespace aspire_sample.ApiService.Endpoints;

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

    static async Task<IResult> GetByCompetition(Guid competitionId, ArcheryDbContext db, CancellationToken ct)
    {
        var participants = await db.CompetitionParticipants
            .AsNoTracking()
            .Where(p => p.CompetitionId == competitionId)
            .Include(p => p.Member)
            .Include(p => p.ExternalParticipant)
            .ToListAsync(ct);
        return Results.Ok(participants);
    }

    static async Task<IResult> Register(CompetitionParticipant input, ArcheryDbContext db, CancellationToken ct)
    {
        if ((input.MemberId is null) == (input.ExternalParticipantId is null))
            return Results.BadRequest("Exactly one of MemberId or ExternalParticipantId must be provided.");

        input.Id = Guid.NewGuid();
        db.CompetitionParticipants.Add(input);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/competition-participants/{input.Id}", input);
    }

    static async Task<IResult> Remove(Guid id, ArcheryDbContext db, CancellationToken ct)
    {
        var participant = await db.CompetitionParticipants.FindAsync([id], ct);
        if (participant is null) return Results.NotFound();
        db.CompetitionParticipants.Remove(participant);
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }
}
