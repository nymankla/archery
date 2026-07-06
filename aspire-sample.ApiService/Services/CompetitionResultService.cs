using aspire_sample.ApiService.Data;
using aspire_sample.ApiService.Infrastructure;
using aspire_sample.ApiService.Models;
using Microsoft.EntityFrameworkCore;

namespace aspire_sample.ApiService.Services;

public class CompetitionResultService(ArcheryDbContext db) : ICompetitionResultService
{
    public async Task<IReadOnlyList<CompetitionResult>> GetByCompetitionAsync(
        Guid competitionId, CancellationToken ct = default)
        => await db.CompetitionResults
            .AsNoTracking()
            .Where(r => r.CompetitionId == competitionId)
            .Include(r => r.Member)
            .Include(r => r.ExternalParticipant)
            .OrderBy(r => r.Placement)
            .ToListAsync(ct);

    public async Task<CompetitionResult?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.CompetitionResults
            .AsNoTracking()
            .Include(r => r.Member)
            .Include(r => r.ExternalParticipant)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<CompetitionResult> CreateAsync(CompetitionResult input, CancellationToken ct = default)
    {
        if ((input.MemberId is null) == (input.ExternalParticipantId is null))
            throw new ArgumentException(
                "Exactly one of MemberId or ExternalParticipantId must be provided.");

        input.Id = Guid.NewGuid();
        db.CompetitionResults.Add(input);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueConstraintViolation())
        {
            throw new ConflictException("A result already exists for this participant in the competition.");
        }
        catch (DbUpdateException ex) when (ex.IsCheckConstraintViolation("CK_CompetitionResult_SingleParticipant"))
        {
            throw new ArgumentException("Exactly one of MemberId or ExternalParticipantId must be provided.");
        }

        return input;
    }

    public async Task<CompetitionResult?> UpdateAsync(Guid id, CompetitionResult input, CancellationToken ct = default)
    {
        if ((input.MemberId is null) == (input.ExternalParticipantId is null))
            throw new ArgumentException(
                "Exactly one of MemberId or ExternalParticipantId must be provided.");

        var result = await db.CompetitionResults.FindAsync([id], ct);
        if (result is null) return null;
        result.BowClass                = input.BowClass;
        result.AgeClass                = input.AgeClass;
        result.Gender                  = input.Gender;
        result.TotalScore              = input.TotalScore;
        result.XCount                  = input.XCount;
        result.Placement               = input.Placement;
        result.IsDisqualified          = input.IsDisqualified;
        result.Notes                   = input.Notes;
        result.MemberId                = input.MemberId;
        result.ExternalParticipantId   = input.ExternalParticipantId;
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueConstraintViolation())
        {
            throw new ConflictException("A result already exists for this participant in the competition.");
        }
        catch (DbUpdateException ex) when (ex.IsCheckConstraintViolation("CK_CompetitionResult_SingleParticipant"))
        {
            throw new ArgumentException("Exactly one of MemberId or ExternalParticipantId must be provided.");
        }

        return result;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var result = await db.CompetitionResults.FindAsync([id], ct);
        if (result is null) return false;
        db.CompetitionResults.Remove(result);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
