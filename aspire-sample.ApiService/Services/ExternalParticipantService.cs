using aspire_sample.ApiService.Data;
using aspire_sample.ApiService.Infrastructure;
using aspire_sample.ApiService.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace aspire_sample.ApiService.Services;

public class ExternalParticipantService(ArcheryDbContext db) : IExternalParticipantService
{
    public async Task<IReadOnlyList<ExternalParticipant>> GetAllAsync(CancellationToken ct = default)
        => await db.ExternalParticipants.AsNoTracking()
            .OrderBy(p => p.LastName).ThenBy(p => p.FirstName)
            .ToListAsync(ct);

    public async Task<ExternalParticipant?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.ExternalParticipants.FindAsync([id], ct);

    public async Task<ExternalParticipant> CreateAsync(ExternalParticipant participant, CancellationToken ct = default)
    {
        participant.Id = Guid.NewGuid();
        db.ExternalParticipants.Add(participant);
        await db.SaveChangesAsync(ct);
        return participant;
    }

    public async Task<ExternalParticipant?> UpdateAsync(Guid id, ExternalParticipant input, CancellationToken ct = default)
    {
        var participant = await db.ExternalParticipants.FindAsync([id], ct);
        if (participant is null) return null;
        participant.FirstName       = input.FirstName;
        participant.LastName        = input.LastName;
        participant.Phone           = input.Phone;
        participant.Email           = input.Email;
        participant.ClubAffiliation = input.ClubAffiliation;
        await db.SaveChangesAsync(ct);
        return participant;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var participant = await db.ExternalParticipants.FindAsync([id], ct);
        if (participant is null) return false;
        db.ExternalParticipants.Remove(participant);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<ImportResult> ImportAsync(IFormFile file, CancellationToken ct = default)
    {
        var rows = SpreadsheetParser.Parse(file);

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

            var email    = SpreadsheetParser.NullIfEmpty(row.Col("Email"));
            var existing = email is not null ? existingByEmail.GetValueOrDefault(email) : null;

            if (existing != null)
            {
                existing.FirstName       = firstName;
                existing.LastName        = lastName;
                existing.Phone           = SpreadsheetParser.NullIfEmpty(row.Col("Phone"));
                existing.ClubAffiliation = SpreadsheetParser.NullIfEmpty(row.Col("ClubAffiliation"));
                updated++;
            }
            else
            {
                db.ExternalParticipants.Add(new ExternalParticipant
                {
                    Id              = Guid.NewGuid(),
                    FirstName       = firstName,
                    LastName        = lastName,
                    Email           = email,
                    Phone           = SpreadsheetParser.NullIfEmpty(row.Col("Phone")),
                    ClubAffiliation = SpreadsheetParser.NullIfEmpty(row.Col("ClubAffiliation"))
                });
                imported++;
            }
        }

        await db.SaveChangesAsync(ct);
        return new ImportResult(imported, updated, errors);
    }
}
