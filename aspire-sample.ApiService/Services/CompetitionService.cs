using aspire_sample.ApiService.Data;
using aspire_sample.ApiService.Models;
using Microsoft.EntityFrameworkCore;

namespace aspire_sample.ApiService.Services;

public class CompetitionService(ArcheryDbContext db) : ICompetitionService
{
    public async Task<IReadOnlyList<Competition>> GetAllAsync(CancellationToken ct = default)
        => await db.Competitions.AsNoTracking().ToListAsync(ct);

    public async Task<Competition?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.Competitions.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<Competition> CreateAsync(Competition competition, CancellationToken ct = default)
    {
        competition.Id = Guid.NewGuid();
        db.Competitions.Add(competition);
        await db.SaveChangesAsync(ct);
        return competition;
    }

    public async Task<Competition?> UpdateAsync(Guid id, Competition input, CancellationToken ct = default)
    {
        var competition = await db.Competitions.FindAsync([id], ct);
        if (competition is null) return null;
        competition.Name        = input.Name;
        competition.Date        = input.Date;
        competition.Location    = input.Location;
        competition.RoundType   = input.RoundType;
        competition.Type        = input.Type;
        competition.Description = input.Description;
        await db.SaveChangesAsync(ct);
        return competition;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var competition = await db.Competitions.FindAsync([id], ct);
        if (competition is null) return false;
        db.Competitions.Remove(competition);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
