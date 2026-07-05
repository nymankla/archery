using aspire_sample.ApiService.Models;
using Microsoft.AspNetCore.Http;

namespace aspire_sample.ApiService.Services;

public interface IMemberService
{
    Task<IReadOnlyList<Member>> GetAllAsync(CancellationToken ct = default);
    Task<Member?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Member> CreateAsync(Member member, CancellationToken ct = default);
    Task<Member?> UpdateAsync(Guid id, Member input, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<ImportResult> ImportAsync(IFormFile file, CancellationToken ct = default);
}
