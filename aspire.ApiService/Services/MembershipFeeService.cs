using aspire.ApiService.Data;
using aspire.ApiService.Infrastructure;
using aspire.ApiService.Models;
using Microsoft.EntityFrameworkCore;

namespace aspire.ApiService.Services;

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
        return await (
            from member in db.Members.AsNoTracking()
            where member.IsActive
            join fee in db.MembershipFees.AsNoTracking().Where(f => f.Year == year)
                on member.Id equals fee.MemberId into feeGroup
            from fee in feeGroup.DefaultIfEmpty()
            orderby member.LastName, member.FirstName
            select new MemberFeeOverviewItem(
                member.Id,
                member.FirstName,
                member.LastName,
                member.Email,
                fee != null ? fee.Id : null,
                fee != null ? fee.Status : null,
                fee != null ? fee.Amount : null,
                fee != null ? fee.DueDate : null,
                fee != null ? fee.PaidDate : null,
                member.DateOfBirth))
            .ToListAsync(ct);
    }

    public async Task<MembershipFee> CreateAsync(MembershipFee fee, CancellationToken ct = default)
    {
        fee.Id = Guid.NewGuid();
        db.MembershipFees.Add(fee);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueConstraintViolation())
        {
            throw new ConflictException("A membership fee already exists for this member and year.");
        }

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

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueConstraintViolation())
        {
            throw new ConflictException("One or more selected members already have a membership fee for this year.");
        }

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
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueConstraintViolation())
        {
            throw new ConflictException("A membership fee already exists for this member and year.");
        }

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
