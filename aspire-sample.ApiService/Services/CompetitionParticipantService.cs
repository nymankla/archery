using aspire_sample.ApiService.Data;
using aspire_sample.ApiService.Infrastructure;
using aspire_sample.ApiService.Models;
using Microsoft.EntityFrameworkCore;

namespace aspire_sample.ApiService.Services;

public class CompetitionParticipantService(ArcheryDbContext db) : ICompetitionParticipantService
{
    public async Task<IReadOnlyList<CompetitionParticipant>> GetByCompetitionAsync(
        Guid competitionId, CancellationToken ct = default)
        => await db.CompetitionParticipants
            .AsNoTracking()
            .Where(p => p.CompetitionId == competitionId)
            .Include(p => p.Member)
            .Include(p => p.ExternalParticipant)
            .ToListAsync(ct);

    public async Task<CompetitionParticipant> RegisterAsync(
        CompetitionParticipant input, CancellationToken ct = default)
    {
        if ((input.MemberId is null) == (input.ExternalParticipantId is null))
            throw new ArgumentException(
                "Exactly one of MemberId or ExternalParticipantId must be provided.");

        input.Id = Guid.NewGuid();
        db.CompetitionParticipants.Add(input);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueConstraintViolation())
        {
            throw new ConflictException("This participant is already registered for the competition.");
        }
        catch (DbUpdateException ex) when (ex.IsCheckConstraintViolation("CK_CompetitionParticipant_SingleParticipant"))
        {
            throw new ArgumentException("Exactly one of MemberId or ExternalParticipantId must be provided.");
        }

        return input;
    }

    public async Task<bool> RemoveAsync(Guid id, CancellationToken ct = default)
    {
        var participant = await db.CompetitionParticipants.FindAsync([id], ct);
        if (participant is null) return false;
        db.CompetitionParticipants.Remove(participant);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
