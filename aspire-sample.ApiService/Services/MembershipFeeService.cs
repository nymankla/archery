using aspire_sample.ApiService.Data;
using aspire_sample.ApiService.Models;
using Microsoft.EntityFrameworkCore;

namespace aspire_sample.ApiService.Services;

public class MembershipFeeService(ArcheryDbContext db) : IMembershipFeeService
{
    public async Task<IReadOnlyList<MembershipFee>> GetAllAsync(Guid? memberId = null, CancellationToken ct = default)
    {
        var query = db.MembershipFees.AsNoTracking();
        if (memberId.HasValue) query = query.Where(f => f.MemberId == memberId.Value);
        return await query.ToListAsync(ct);
    }

    public async Task<MembershipFee?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.MembershipFees.FindAsync([id], ct);

    public async Task<IReadOnlyList<MembershipFee>> GetByMemberAsync(Guid memberId, CancellationToken ct = default)
        => await db.MembershipFees.AsNoTracking()
            .Where(f => f.MemberId == memberId)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<MemberFeeOverviewItem>> GetOverviewAsync(int year, CancellationToken ct = default)
    {
        var members = await db.Members
            .AsNoTracking()
            .Where(m => m.IsActive)
            .OrderBy(m => m.LastName).ThenBy(m => m.FirstName)
            .ToListAsync(ct);

        var memberIds = members.Select(m => m.Id).ToList();
        var fees = await db.MembershipFees
            .AsNoTracking()
            .Where(f => f.Year == year && memberIds.Contains(f.MemberId))
            .ToListAsync(ct);

        var feeByMember = fees.ToDictionary(f => f.MemberId);

        return members.Select(m =>
        {
            feeByMember.TryGetValue(m.Id, out var fee);
            return new MemberFeeOverviewItem(
                m.Id, m.FirstName, m.LastName, m.Email,
                fee?.Id, fee?.Status, fee?.Amount, fee?.DueDate, fee?.PaidDate,
                m.DateOfBirth);
        }).ToList();
    }

    public async Task<MembershipFee> CreateAsync(MembershipFee fee, CancellationToken ct = default)
    {
        fee.Id = Guid.NewGuid();
        db.MembershipFees.Add(fee);
        await db.SaveChangesAsync(ct);
        return fee;
    }

    public async Task<int> BulkCreateAsync(int year, decimal amount, DateOnly dueDate, int? minAge = null, string? ageOp = null, CancellationToken ct = default)
    {
        var existingMemberIds = await db.MembershipFees
            .Where(f => f.Year == year)
            .Select(f => f.MemberId)
            .ToHashSetAsync(ct);

        var query = db.Members.Where(m => m.IsActive && !existingMemberIds.Contains(m.Id));

        if (minAge.HasValue)
        {
            // Age = year - DateOfBirth.Year (archery club uses calendar-year age classes)
            // ">=" means age >= minAge → birth year <= year - minAge
            // ">"  means age >  minAge → birth year <  year - minAge
            var cutoff = year - minAge.Value;
            query = ageOp == ">" ? query.Where(m => m.DateOfBirth.Year < cutoff)
                                 : query.Where(m => m.DateOfBirth.Year <= cutoff);
        }

        var memberIds = await query.Select(m => m.Id).ToListAsync(ct);

        foreach (var memberId in memberIds)
            db.MembershipFees.Add(new MembershipFee
            {
                Id       = Guid.NewGuid(),
                MemberId = memberId,
                Year     = year,
                Amount   = amount,
                DueDate  = dueDate,
                Status   = FeeStatus.Unpaid
            });

        await db.SaveChangesAsync(ct);
        return memberIds.Count;
    }

    public async Task<MembershipFee?> UpdateAsync(Guid id, MembershipFee input, CancellationToken ct = default)
    {
        var fee = await db.MembershipFees.FindAsync([id], ct);
        if (fee is null) return null;
        fee.Year     = input.Year;
        fee.Amount   = input.Amount;
        fee.DueDate  = input.DueDate;
        fee.PaidDate = input.PaidDate;
        fee.Status   = input.Status;
        await db.SaveChangesAsync(ct);
        return fee;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var fee = await db.MembershipFees.FindAsync([id], ct);
        if (fee is null) return false;
        db.MembershipFees.Remove(fee);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
