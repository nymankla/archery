using aspire.ApiService.Models;
using Microsoft.AspNetCore.Http;

namespace aspire.ApiService.Services;

public interface ICompetitionService
{
    Task<IReadOnlyList<Competition>> GetAllAsync(CancellationToken ct = default);
    Task<Competition?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Competition> CreateAsync(Competition competition, CancellationToken ct = default);
    Task<Competition?> UpdateAsync(Guid id, Competition input, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<ImportResult> ImportAsync(IFormFile file, CancellationToken ct = default);
}
