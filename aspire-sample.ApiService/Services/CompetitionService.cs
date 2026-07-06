using aspire_sample.ApiService.Data;
using aspire_sample.ApiService.Infrastructure;
using aspire_sample.ApiService.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace aspire_sample.ApiService.Services;

public class CompetitionService(ArcheryDbContext db) : ICompetitionService
{
    public async Task<IReadOnlyList<Competition>> GetAllAsync(CancellationToken ct = default)
        => await db.Competitions.AsNoTracking().ToListAsync(ct);

    public async Task<Competition?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.Competitions.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<Competition> CreateAsync(Competition competition, CancellationToken ct = default)
    {
        competition.Id = Guid.NewGuid();
        db.Competitions.Add(competition);
        await db.SaveChangesAsync(ct);
        return competition;
    }

    public async Task<Competition?> UpdateAsync(Guid id, Competition input, CancellationToken ct = default)
    {
        var competition = await db.Competitions.FindAsync([id], ct);
        if (competition is null) return null;
        competition.Name        = input.Name;
        competition.Date        = input.Date;
        competition.Location    = input.Location;
        competition.RoundType   = input.RoundType;
        competition.Type        = input.Type;
        competition.Description = input.Description;
        await db.SaveChangesAsync(ct);
        return competition;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var competition = await db.Competitions.FindAsync([id], ct);
        if (competition is null) return false;
        db.Competitions.Remove(competition);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<ImportResult> ImportAsync(IFormFile file, CancellationToken ct = default)
    {
        var rows = SpreadsheetParser.Parse(file);

        var existing = await db.Competitions.ToListAsync(ct);
        var lookup = existing.ToDictionary(c => (c.Name, c.Date));

        var errors = new List<string>();
        int imported = 0, updated = 0;

        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var rowNum = i + 1;

            var name = row.Col("Name");
            var dateStr = row.Col("Date");
            var location = row.Col("Location");
            var roundType = row.Col("RoundType");
            var typeStr = row.Col("Type");
            var description = SpreadsheetParser.NullIfEmpty(row.Col("Description"));

            if (string.IsNullOrWhiteSpace(name))  { errors.Add($"Row {rowNum}: Name is required.");     continue; }
            if (string.IsNullOrWhiteSpace(location)) { errors.Add($"Row {rowNum}: Location is required."); continue; }
            if (string.IsNullOrWhiteSpace(roundType)) { errors.Add($"Row {rowNum}: RoundType is required."); continue; }
            if (!DateOnly.TryParse(dateStr, out var date)) { errors.Add($"Row {rowNum}: Date is required."); continue; }

            var type = Enum.TryParse<CompetitionType>(typeStr, ignoreCase: true, out var parsedType)
                ? parsedType
                : CompetitionType.Indoor;

            if (lookup.TryGetValue((name, date), out var comp))
            {
                comp.Location    = location;
                comp.RoundType   = roundType;
                comp.Type        = type;
                comp.Description = description;
                updated++;
            }
            else
            {
                var newComp = new Competition
                {
                    Id          = Guid.NewGuid(),
                    Name        = name,
                    Date        = date,
                    Location    = location,
                    RoundType   = roundType,
                    Type        = type,
                    Description = description
                };
                db.Competitions.Add(newComp);
                imported++;
            }
        }

        await db.SaveChangesAsync(ct);
        return new ImportResult(imported, updated, errors);
    }
}
