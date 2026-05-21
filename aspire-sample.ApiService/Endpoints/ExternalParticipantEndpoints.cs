using aspire_sample.ApiService.Data;
using aspire_sample.ApiService.Models;
using Microsoft.EntityFrameworkCore;

namespace aspire_sample.ApiService.Endpoints;

public static class ExternalParticipantEndpoints
{
    public static IEndpointRouteBuilder MapExternalParticipantEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/external-participants").WithTags("ExternalParticipants");

        group.MapGet("/", GetAll);
        group.MapGet("/{id:guid}", GetById);
        group.MapPost("/", Create);
        group.MapPut("/{id:guid}", Update);
        group.MapDelete("/{id:guid}", Delete);

        return app;
    }

    static async Task<IResult> GetAll(ArcheryDbContext db, CancellationToken ct)
        => Results.Ok(await db.ExternalParticipants.AsNoTracking().OrderBy(p => p.LastName).ThenBy(p => p.FirstName).ToListAsync(ct));

    static async Task<IResult> GetById(Guid id, ArcheryDbContext db, CancellationToken ct)
        => await db.ExternalParticipants.FindAsync([id], ct) is { } p
            ? Results.Ok(p)
            : Results.NotFound();

    static async Task<IResult> Create(ExternalParticipant participant, ArcheryDbContext db, CancellationToken ct)
    {
        participant.Id = Guid.NewGuid();
        db.ExternalParticipants.Add(participant);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/external-participants/{participant.Id}", participant);
    }

    static async Task<IResult> Update(Guid id, ExternalParticipant input, ArcheryDbContext db, CancellationToken ct)
    {
        var participant = await db.ExternalParticipants.FindAsync([id], ct);
        if (participant is null) return Results.NotFound();
        participant.FirstName = input.FirstName;
        participant.LastName = input.LastName;
        participant.Phone = input.Phone;
        participant.Email = input.Email;
        participant.ClubAffiliation = input.ClubAffiliation;
        await db.SaveChangesAsync(ct);
        return Results.Ok(participant);
    }

    static async Task<IResult> Delete(Guid id, ArcheryDbContext db, CancellationToken ct)
    {
        var participant = await db.ExternalParticipants.FindAsync([id], ct);
        if (participant is null) return Results.NotFound();
        db.ExternalParticipants.Remove(participant);
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }
}
