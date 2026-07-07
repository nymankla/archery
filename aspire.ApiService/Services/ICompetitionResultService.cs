using aspire.ApiService.Models;

namespace aspire.ApiService.Services;

public interface ICompetitionResultService
{
    Task<IReadOnlyList<CompetitionResult>> GetByCompetitionAsync(Guid competitionId, CancellationToken ct = default);
    Task<CompetitionResult?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<CompetitionResult> CreateAsync(CompetitionResult input, CancellationToken ct = default);
    Task<CompetitionResult?> UpdateAsync(Guid id, CompetitionResult input, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}
