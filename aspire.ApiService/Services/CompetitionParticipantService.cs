using aspire.ApiService.Data;
using aspire.ApiService.Infrastructure;
using aspire.ApiService.Models;
using Microsoft.EntityFrameworkCore;

namespace aspire.ApiService.Services;

public class CompetitionParticipantService(ArcheryDbContext db) : ICompetitionParticipantService
{
    const string InvalidParticipantSelectionError = "Exactly one of MemberId or ExternalParticipantId must be provided.";
    const string DuplicateRegistrationError = "This participant is already registered for the competition.";

    public async Task<IReadOnlyList<CompetitionParticipant>> GetByCompetitionAsync(
        Guid competitionId, CancellationToken ct = default)
        => await db.CompetitionParticipants
            .AsNoTracking()
            .Where(p => p.CompetitionId == competitionId)
            .Include(p => p.Member)
            .Include(p => p.ExternalParticipant)
            .ToListAsync(ct);

    public async Task<Result<CompetitionParticipant>> RegisterAsync(
        CompetitionParticipant input, CancellationToken ct = default)
    {
        if ((input.MemberId is null) == (input.ExternalParticipantId is null))
            return Result<CompetitionParticipant>.Failure(InvalidParticipantSelectionError);

        input.Id = Guid.NewGuid();
        db.CompetitionParticipants.Add(input);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueConstraintViolation())
        {
            return Result<CompetitionParticipant>.Failure(DuplicateRegistrationError);
        }
        catch (DbUpdateException ex) when (ex.IsCheckConstraintViolation("CK_CompetitionParticipant_SingleParticipant"))
        {
            return Result<CompetitionParticipant>.Failure(InvalidParticipantSelectionError);
        }

        return Result<CompetitionParticipant>.Success(input);
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
