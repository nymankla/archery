using aspire_sample.ApiService.Models;

namespace aspire_sample.ApiService.Services;

public record MemberFeeOverviewItem(
    Guid MemberId, string FirstName, string LastName, string? Email,
    Guid? FeeId, FeeStatus? Status, decimal? Amount, DateOnly? DueDate, DateOnly? PaidDate);

public interface IMembershipFeeService
{
    Task<IReadOnlyList<MembershipFee>> GetAllAsync(Guid? memberId = null, CancellationToken ct = default);
    Task<MembershipFee?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<MembershipFee>> GetByMemberAsync(Guid memberId, CancellationToken ct = default);
    Task<IReadOnlyList<MemberFeeOverviewItem>> GetOverviewAsync(int year, CancellationToken ct = default);
    Task<MembershipFee> CreateAsync(MembershipFee fee, CancellationToken ct = default);
    Task<int> BulkCreateAsync(int year, decimal amount, DateOnly dueDate, CancellationToken ct = default);
    Task<MembershipFee?> UpdateAsync(Guid id, MembershipFee input, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}
