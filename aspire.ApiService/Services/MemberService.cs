using aspire.ApiService.Data;
using aspire.ApiService.Infrastructure;
using aspire.ApiService.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace aspire.ApiService.Services;

public class MemberService(ArcheryDbContext db) : IMemberService
{
    public async Task<IReadOnlyList<Member>> GetAllAsync(CancellationToken ct = default)
        => await db.Members.AsNoTracking()
            .OrderBy(m => m.LastName).ThenBy(m => m.FirstName)
            .ToListAsync(ct);

    public async Task<Member?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.Members.FindAsync([id], ct);

    public async Task<Member> CreateAsync(Member member, CancellationToken ct = default)
    {
        member.Id = Guid.NewGuid();
        db.Members.Add(member);
        await db.SaveChangesAsync(ct);
        return member;
    }

    public async Task<Member?> UpdateAsync(Guid id, Member input, CancellationToken ct = default)
    {
        var member = await db.Members.FindAsync([id], ct);
        if (member is null) return null;
        member.FirstName         = input.FirstName;
        member.LastName          = input.LastName;
        member.Address           = input.Address;
        member.Phone             = input.Phone;
        member.Email             = input.Email;
        member.DateOfBirth       = input.DateOfBirth;
        member.JoinDate          = input.JoinDate;
        member.IsActive          = input.IsActive;
        member.PreferredBowClass = input.PreferredBowClass;
        await db.SaveChangesAsync(ct);
        return member;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var member = await db.Members.FindAsync([id], ct);
        if (member is null) return false;
        db.Members.Remove(member);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<ImportResult> ImportAsync(IFormFile file, CancellationToken ct = default)
    {
        var rows = SpreadsheetParser.Parse(file);

        var existingByKey = await db.Members
            .Where(m => m.Email != null)
            .ToDictionaryAsync(m => (m.Email, m.DateOfBirth), ct);

        var imported = 0;
        var updated  = 0;
        var errors   = new List<string>();

        for (var i = 0; i < rows.Count; i++)
        {
            var row    = rows[i];
            var rowNum = i + 2;

            var firstName = row.Col("FirstName");
            var lastName  = row.Col("LastName");
            if (string.IsNullOrWhiteSpace(firstName))
            { errors.Add($"Row {rowNum}: FirstName is required."); continue; }
            if (string.IsNullOrWhiteSpace(lastName))
            { errors.Add($"Row {rowNum}: LastName is required."); continue; }
            if (!DateOnly.TryParse(row.Col("DateOfBirth"), out var dob))
            { errors.Add($"Row {rowNum}: Invalid DateOfBirth '{row.Col("DateOfBirth")}'."); continue; }

            var email    = SpreadsheetParser.NullIfEmpty(row.Col("Email"));
            var joinDate = DateOnly.TryParse(row.Col("JoinDate"), out var jd) ? jd : DateOnly.FromDateTime(DateTime.Today);
            var isActive = bool.TryParse(row.Col("IsActive"), out var ia) ? ia : true;
            var bowClass = Enum.TryParse<BowClass>(row.Col("PreferredBowClass"), ignoreCase: true, out var bc) ? bc : BowClass.Recurve;

            var existing = email is not null ? existingByKey.GetValueOrDefault((email, dob)) : null;
            if (existing != null)
            {
                existing.FirstName         = firstName;
                existing.LastName          = lastName;
                existing.Phone             = SpreadsheetParser.NullIfEmpty(row.Col("Phone"));
                existing.Address           = SpreadsheetParser.NullIfEmpty(row.Col("Address"));
                existing.JoinDate          = joinDate;
                existing.IsActive          = isActive;
                existing.PreferredBowClass = bowClass;
                updated++;
            }
            else
            {
                db.Members.Add(new Member
                {
                    Id                = Guid.NewGuid(),
                    FirstName         = firstName,
                    LastName          = lastName,
                    Email             = email,
                    Phone             = SpreadsheetParser.NullIfEmpty(row.Col("Phone")),
                    Address           = SpreadsheetParser.NullIfEmpty(row.Col("Address")),
                    DateOfBirth       = dob,
                    JoinDate          = joinDate,
                    IsActive          = isActive,
                    PreferredBowClass = bowClass
                });
                imported++;
            }
        }

        await db.SaveChangesAsync(ct);
        return new ImportResult(imported, updated, errors);
    }
}
