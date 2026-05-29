using aspire_sample.ApiService.Data;
using aspire_sample.ApiService.Infrastructure;
using aspire_sample.ApiService.Models;
using Microsoft.EntityFrameworkCore;

namespace aspire_sample.ApiService.Endpoints;

public static class ExternalParticipantImportEndpoints
{
    public static IEndpointRouteBuilder MapExternalParticipantImportEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/external-participants/import", Import).WithTags("ExternalParticipants").DisableAntiforgery().RequireAuthorization();
        return app;
    }

    static async Task<IResult> Import(IFormFile file, ArcheryDbContext db, CancellationToken ct)
    {
        List<Dictionary<string, string>> rows;
        try { rows = SpreadsheetParser.Parse(file); }
        catch (Exception ex) { return Results.BadRequest($"Failed to parse file: {ex.Message}"); }

        // Merge key: Email. Participants without email are always inserted.
        var existingByEmail = await db.ExternalParticipants
            .Where(p => p.Email != null)
            .ToDictionaryAsync(p => p.Email!, ct);

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

            var email            = SpreadsheetParser.NullIfEmpty(row.Col("Email"));
            var existing         = email is not null ? existingByEmail.GetValueOrDefault(email) : null;

            if (existing != null)
            {
                existing.FirstName        = firstName;
                existing.LastName         = lastName;
                existing.Phone            = SpreadsheetParser.NullIfEmpty(row.Col("Phone"));
                existing.ClubAffiliation  = SpreadsheetParser.NullIfEmpty(row.Col("ClubAffiliation"));
                updated++;
            }
            else
            {
                db.ExternalParticipants.Add(new ExternalParticipant
                {
                    Id               = Guid.NewGuid(),
                    FirstName        = firstName,
                    LastName         = lastName,
                    Email            = email,
                    Phone            = SpreadsheetParser.NullIfEmpty(row.Col("Phone")),
                    ClubAffiliation  = SpreadsheetParser.NullIfEmpty(row.Col("ClubAffiliation"))
                });
                imported++;
            }
        }

        await db.SaveChangesAsync(ct);
        return Results.Ok(new { imported, updated, errors });
    }
}
