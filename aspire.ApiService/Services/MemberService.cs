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

    public async Task<Result<Member>> CreateAsync(Member member, CancellationToken ct = default)
    {
        var personnummerResult = await NormalizeAndValidatePersonnummerAsync(member.Personnummer, null, ct);
        if (!personnummerResult.IsSuccess)
            return Result<Member>.Failure(personnummerResult.Errors);

        member.Personnummer = personnummerResult.Value;
        member.Id = Guid.NewGuid();
        db.Members.Add(member);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueConstraintViolation())
        {
            return Result<Member>.Failure("A member with this personnummer already exists.");
        }
        return Result<Member>.Success(member);
    }

    public async Task<Result<Member>> UpdateAsync(Guid id, Member input, CancellationToken ct = default)
    {
        var member = await db.Members.FindAsync([id], ct);
        if (member is null) return Result<Member>.Failure("Member not found.");
        member.FirstName         = input.FirstName;
        member.LastName          = input.LastName;
        member.Address           = input.Address;
        member.Phone             = input.Phone;
        member.Email             = input.Email;
        var personnummerResult = await NormalizeAndValidatePersonnummerAsync(input.Personnummer, id, ct);
        if (!personnummerResult.IsSuccess)
            return Result<Member>.Failure(personnummerResult.Errors);

        member.Personnummer      = personnummerResult.Value;
        member.DateOfBirth       = input.DateOfBirth;
        member.JoinDate          = input.JoinDate;
        member.IsActive          = input.IsActive;
        member.PreferredBowClass = input.PreferredBowClass;
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueConstraintViolation())
        {
            return Result<Member>.Failure("A member with this personnummer already exists.");
        }
        return Result<Member>.Success(member);
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
            var personnummerRaw = SpreadsheetParser.NullIfEmpty(row.Col("Personnummer"));
            var joinDate = DateOnly.TryParse(row.Col("JoinDate"), out var jd) ? jd : DateOnly.FromDateTime(DateTime.Today);
            var isActive = bool.TryParse(row.Col("IsActive"), out var ia) ? ia : true;
            var bowClass = Enum.TryParse<BowClass>(row.Col("PreferredBowClass"), ignoreCase: true, out var bc) ? bc : BowClass.Recurve;
            var existing = email is not null ? existingByKey.GetValueOrDefault((email, dob)) : null;
            var normalizedPersonnummer = await NormalizeAndValidatePersonnummerForImportAsync(personnummerRaw, existing?.Id, rowNum, ct);
            if (normalizedPersonnummer == ImportValidationFailed.Value)
                continue;

            if (existing != null)
            {
                existing.FirstName         = firstName;
                existing.LastName          = lastName;
                existing.Phone             = SpreadsheetParser.NullIfEmpty(row.Col("Phone"));
                existing.Address           = SpreadsheetParser.NullIfEmpty(row.Col("Address"));
                existing.Email             = email;
                existing.Personnummer      = normalizedPersonnummer;
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
                    Personnummer      = normalizedPersonnummer,
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

        async Task<string?> NormalizeAndValidatePersonnummerForImportAsync(string? personnummer, Guid? memberId, int rowNumber, CancellationToken cancellationToken)
        {
            var result = await NormalizeAndValidatePersonnummerAsync(personnummer, memberId, cancellationToken);
            if (result.IsSuccess)
                return result.Value;

            foreach (var error in result.Errors)
                errors.Add($"Row {rowNumber}: {error}");

            return ImportValidationFailed.Value;
        }
    }

    public async Task<ExportFile> ExportAsync(ExportFormat format, CancellationToken ct = default)
    {
        var members = await GetAllAsync(ct);

        string[] headers = ["First Name", "Last Name", "Personnummer", "Date of Birth", "Address", "Phone", "Email", "Join Date", "Bow Class", "Status"];
        var rows = members.Select(m => (IReadOnlyList<string?>)[
            m.FirstName,
            m.LastName,
            m.Personnummer,
            m.DateOfBirth.ToString("yyyy-MM-dd"),
            m.Address,
            m.Phone,
            m.Email,
            m.JoinDate.ToString("yyyy-MM-dd"),
            m.PreferredBowClass.ToString(),
            m.IsActive ? "Active" : "Inactive"
        ]);

        return SpreadsheetWriter.Write(format, "members", "Members", headers, rows);
    }

    async Task<Result<string?>> NormalizeAndValidatePersonnummerAsync(string? personnummer, Guid? memberId, CancellationToken ct)
    {
        var normalizedResult = PersonnummerParser.Normalize(personnummer);
        if (!normalizedResult.IsSuccess)
            return Result<string?>.Failure(normalizedResult.Errors);

        var normalized = normalizedResult.Value;
        if (normalized is null)
            return normalizedResult;

        var exists = await db.Members.AnyAsync(m => m.Personnummer == normalized && m.Id != memberId, ct);
        if (exists)
            return Result<string?>.Failure("A member with this personnummer already exists.");

        return normalizedResult;
    }

    private static class ImportValidationFailed
    {
        public const string Value = "__INVALID__";
    }
}
