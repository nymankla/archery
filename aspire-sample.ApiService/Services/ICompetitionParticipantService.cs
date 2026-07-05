using aspire_sample.ApiService.Models;

namespace aspire_sample.ApiService.Services;

public interface ICompetitionParticipantService
{
    Task<IReadOnlyList<CompetitionParticipant>> GetByCompetitionAsync(Guid competitionId, CancellationToken ct = default);
    Task<CompetitionParticipant> RegisterAsync(CompetitionParticipant input, CancellationToken ct = default);
    Task<bool> RemoveAsync(Guid id, CancellationToken ct = default);
}
