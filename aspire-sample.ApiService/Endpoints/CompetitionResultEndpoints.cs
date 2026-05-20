using aspire_sample.ApiService.Data;
using aspire_sample.ApiService.Models;
using Microsoft.EntityFrameworkCore;

namespace aspire_sample.ApiService.Endpoints;

public static class CompetitionResultEndpoints
{
    public static IEndpointRouteBuilder MapCompetitionResultEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/competition-results").WithTags("CompetitionResults");

        group.MapGet("/competition/{competitionId:guid}", GetByCompetition);
        group.MapGet("/{id:guid}", GetById);
        group.MapPost("/", Create);
        group.MapPut("/{id:guid}", Update);
        group.MapDelete("/{id:guid}", Delete);

        return app;
    }

    static async Task<IResult> GetByCompetition(Guid competitionId, ArcheryDbContext db, CancellationToken ct)
    {
        var results = await db.CompetitionResults
            .AsNoTracking()
            .Where(r => r.CompetitionId == competitionId)
            .Include(r => r.Member)
            .Include(r => r.ExternalParticipant)
            .OrderBy(r => r.Placement)
            .ToListAsync(ct);
        return Results.Ok(results);
    }

    static async Task<IResult> GetById(Guid id, ArcheryDbContext db, CancellationToken ct)
    {
        var result = await db.CompetitionResults
            .AsNoTracking()
            .Include(r => r.Member)
            .Include(r => r.ExternalParticipant)
            .FirstOrDefaultAsync(r => r.Id == id, ct);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    static async Task<IResult> Create(CompetitionResult input, ArcheryDbContext db, CancellationToken ct)
    {
        if ((input.MemberId is null) == (input.ExternalParticipantId is null))
            return Results.BadRequest("Exactly one of MemberId or ExternalParticipantId must be provided.");

        input.Id = Guid.NewGuid();
        db.CompetitionResults.Add(input);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/competition-results/{input.Id}", input);
    }

    static async Task<IResult> Update(Guid id, CompetitionResult input, ArcheryDbContext db, CancellationToken ct)
    {
        if ((input.MemberId is null) == (input.ExternalParticipantId is null))
            return Results.BadRequest("Exactly one of MemberId or ExternalParticipantId must be provided.");

        var result = await db.CompetitionResults.FindAsync([id], ct);
        if (result is null) return Results.NotFound();
        result.BowClass = input.BowClass;
        result.AgeClass = input.AgeClass;
        result.Gender = input.Gender;
        result.TotalScore = input.TotalScore;
        result.XCount = input.XCount;
        result.Placement = input.Placement;
        result.IsDisqualified = input.IsDisqualified;
        result.Notes = input.Notes;
        result.MemberId = input.MemberId;
        result.ExternalParticipantId = input.ExternalParticipantId;
        await db.SaveChangesAsync(ct);
        return Results.Ok(result);
    }

    static async Task<IResult> Delete(Guid id, ArcheryDbContext db, CancellationToken ct)
    {
        var result = await db.CompetitionResults.FindAsync([id], ct);
        if (result is null) return Results.NotFound();
        db.CompetitionResults.Remove(result);
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }
}
