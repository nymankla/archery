using aspire_sample.ApiService.Data;
using aspire_sample.ApiService.Infrastructure;
using aspire_sample.ApiService.Models;
using Microsoft.EntityFrameworkCore;

namespace aspire_sample.ApiService.Endpoints;

public static class MemberImportEndpoints
{
    public static IEndpointRouteBuilder MapMemberImportEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/members/import", Import).WithTags("Members").DisableAntiforgery().RequireAuthorization();
        return app;
    }

    static async Task<IResult> Import(IFormFile file, ArcheryDbContext db, CancellationToken ct)
    {
        List<Dictionary<string, string>> rows;
        try { rows = SpreadsheetParser.Parse(file); }
        catch (Exception ex) { return Results.BadRequest($"Failed to parse file: {ex.Message}"); }

        var existingByKey = await db.Members
            .Where(m => m.Email != null)
            .ToDictionaryAsync(m => (m.Email, m.DateOfBirth), ct);

        var imported = 0;
        var updated = 0;
        var errors = new List<string>();

        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
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
                existing.FirstName        = firstName;
                existing.LastName         = lastName;
                existing.Phone            = SpreadsheetParser.NullIfEmpty(row.Col("Phone"));
                existing.Address          = SpreadsheetParser.NullIfEmpty(row.Col("Address"));
                existing.JoinDate         = joinDate;
                existing.IsActive         = isActive;
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
        return Results.Ok(new { imported, updated, errors });
    }
}
