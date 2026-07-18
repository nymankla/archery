using aspire.ApiService.Models;
using aspire.ApiService.Infrastructure;

namespace aspire.ApiService.Services;

public interface ICompetitionParticipantService
{
    Task<IReadOnlyList<CompetitionParticipant>> GetByCompetitionAsync(Guid competitionId, CancellationToken ct = default);
    Task<Result<CompetitionParticipant>> RegisterAsync(CompetitionParticipant input, CancellationToken ct = default);
    Task<bool> RemoveAsync(Guid id, CancellationToken ct = default);
}
