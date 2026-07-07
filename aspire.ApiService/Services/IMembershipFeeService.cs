using aspire.ApiService.Models;

namespace aspire.ApiService.Services;

public record MemberFeeOverviewItem(
    Guid MemberId, string FirstName, string LastName, string? Email,
    Guid? FeeId, FeeStatus? Status, decimal? Amount, DateOnly? DueDate, DateOnly? PaidDate,
    DateOnly DateOfBirth);

public interface IMembershipFeeService
{
    Task<IReadOnlyList<MembershipFee>> GetAllAsync(Guid? memberId = null, CancellationToken ct = default);
    Task<MembershipFee?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<MembershipFee>> GetByMemberAsync(Guid memberId, CancellationToken ct = default);
    Task<IReadOnlyList<MemberFeeOverviewItem>> GetOverviewAsync(int year, CancellationToken ct = default);
    Task<MembershipFee> CreateAsync(MembershipFee fee, CancellationToken ct = default);
    Task<int> BulkCreateAsync(int year, decimal amount, DateOnly dueDate, int? minAge = null, string? ageOp = null, CancellationToken ct = default);
    Task<MembershipFee?> UpdateAsync(Guid id, MembershipFee input, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}
