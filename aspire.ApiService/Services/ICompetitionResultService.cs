using aspire.ApiService.Models;
using aspire.ApiService.Infrastructure;

namespace aspire.ApiService.Services;

public interface ICompetitionResultService
{
    Task<IReadOnlyList<CompetitionResult>> GetByCompetitionAsync(Guid competitionId, CancellationToken ct = default);
    Task<CompetitionResult?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<CompetitionResult>> CreateAsync(CompetitionResult input, CancellationToken ct = default);
    Task<Result<CompetitionResult>> UpdateAsync(Guid id, CompetitionResult input, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}
