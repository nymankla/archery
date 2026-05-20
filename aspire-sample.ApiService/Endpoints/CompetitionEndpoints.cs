using aspire_sample.ApiService.Data;
using aspire_sample.ApiService.Models;
using Microsoft.EntityFrameworkCore;

namespace aspire_sample.ApiService.Endpoints;

public static class CompetitionEndpoints
{
    public static IEndpointRouteBuilder MapCompetitionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/competitions").WithTags("Competitions");

        group.MapGet("/", GetAllCompetitions);
        group.MapGet("/{id:guid}", GetCompetitionById);
        group.MapPost("/", CreateCompetition);
        group.MapPut("/{id:guid}", UpdateCompetition);
        group.MapDelete("/{id:guid}", DeleteCompetition);

        return app;
    }

    static async Task<IResult> GetAllCompetitions(ArcheryDbContext db, CancellationToken ct)
        => Results.Ok(await db.Competitions.AsNoTracking().ToListAsync(ct));

    static async Task<IResult> GetCompetitionById(Guid id, ArcheryDbContext db, CancellationToken ct)
        => await db.Competitions.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, ct) is { } c
            ? Results.Ok(c)
            : Results.NotFound();

    static async Task<IResult> CreateCompetition(Competition competition, ArcheryDbContext db, CancellationToken ct)
    {
        competition.Id = Guid.NewGuid();
        db.Competitions.Add(competition);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/competitions/{competition.Id}", competition);
    }

    static async Task<IResult> UpdateCompetition(Guid id, Competition input, ArcheryDbContext db, CancellationToken ct)
    {
        var competition = await db.Competitions.FindAsync([id], ct);
        if (competition is null) return Results.NotFound();
        competition.Name = input.Name;
        competition.Date = input.Date;
        competition.Location = input.Location;
        competition.RoundType = input.RoundType;
        competition.Type = input.Type;
        competition.Description = input.Description;
        await db.SaveChangesAsync(ct);
        return Results.Ok(competition);
    }

    static async Task<IResult> DeleteCompetition(Guid id, ArcheryDbContext db, CancellationToken ct)
    {
        var competition = await db.Competitions.FindAsync([id], ct);
        if (competition is null) return Results.NotFound();
        db.Competitions.Remove(competition);
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }
}
