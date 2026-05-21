using aspire_sample.ApiService.Data;
using aspire_sample.ApiService.Models;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace aspire_sample.ApiService.Endpoints;

public static class MemberImportEndpoints
{
    public static IEndpointRouteBuilder MapMemberImportEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/members/import", Import).WithTags("Members").DisableAntiforgery();
        return app;
    }

    static async Task<IResult> Import(IFormFile file, ArcheryDbContext db, CancellationToken ct)
    {
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();

        List<MemberRow> rows;
        try
        {
            using var stream = file.OpenReadStream();
            rows = ext == ".xlsx" ? ParseXlsx(stream) : ParseCsv(stream);
        }
        catch (Exception ex)
        {
            return Results.BadRequest($"Failed to parse file: {ex.Message}");
        }

        // Load existing members with email for merge lookup — tracked so EF picks up changes
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

            if (string.IsNullOrWhiteSpace(row.FirstName))
            { errors.Add($"Row {rowNum}: FirstName is required."); continue; }
            if (string.IsNullOrWhiteSpace(row.LastName))
            { errors.Add($"Row {rowNum}: LastName is required."); continue; }
            if (!DateOnly.TryParse(row.DateOfBirth, out var dob))
            { errors.Add($"Row {rowNum}: Invalid DateOfBirth '{row.DateOfBirth}'."); continue; }

            var email = NullIfEmpty(row.Email);
            var joinDate = DateOnly.TryParse(row.JoinDate, out var jd) ? jd : DateOnly.FromDateTime(DateTime.Today);
            var isActive = bool.TryParse(row.IsActive, out var ia) ? ia : true;
            var bowClass = Enum.TryParse<BowClass>(row.PreferredBowClass, true, out var bc) ? bc : BowClass.Recurve;

            var existing = email != null && existingByKey.TryGetValue((email, dob), out var m) ? m : null;

            if (existing != null)
            {
                existing.FirstName = row.FirstName.Trim();
                existing.LastName = row.LastName.Trim();
                existing.Phone = NullIfEmpty(row.Phone);
                existing.Address = NullIfEmpty(row.Address);
                existing.JoinDate = joinDate;
                existing.IsActive = isActive;
                existing.PreferredBowClass = bowClass;
                updated++;
            }
            else
            {
                db.Members.Add(new Member
                {
                    Id = Guid.NewGuid(),
                    FirstName = row.FirstName.Trim(),
                    LastName = row.LastName.Trim(),
                    Email = email,
                    Phone = NullIfEmpty(row.Phone),
                    Address = NullIfEmpty(row.Address),
                    DateOfBirth = dob,
                    JoinDate = joinDate,
                    IsActive = isActive,
                    PreferredBowClass = bowClass
                });
                imported++;
            }
        }

        await db.SaveChangesAsync(ct);
        return Results.Ok(new { imported, updated, errors });
    }

    static List<MemberRow> ParseCsv(Stream stream)
    {
        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HeaderValidated = null,
            MissingFieldFound = null
        });
        return csv.GetRecords<MemberRow>().ToList();
    }

    static List<MemberRow> ParseXlsx(Stream stream)
    {
        using var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheets.First();
        var headers = sheet.Row(1).CellsUsed()
            .Select(c => c.GetValue<string>().Trim())
            .ToList();

        string Get(IXLRow row, string name)
        {
            var idx = headers.IndexOf(name);
            return idx >= 0 ? row.Cell(idx + 1).GetValue<string>().Trim() : string.Empty;
        }

        return sheet.RowsUsed().Skip(1).Select(row => new MemberRow
        {
            FirstName = Get(row, "FirstName"),
            LastName = Get(row, "LastName"),
            Email = Get(row, "Email"),
            Phone = Get(row, "Phone"),
            Address = Get(row, "Address"),
            DateOfBirth = Get(row, "DateOfBirth"),
            JoinDate = Get(row, "JoinDate"),
            IsActive = Get(row, "IsActive"),
            PreferredBowClass = Get(row, "PreferredBowClass")
        }).ToList();
    }

    static string? NullIfEmpty(string? s) => string.IsNullOrWhiteSpace(s) ? null : s;

    class MemberRow
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string DateOfBirth { get; set; } = string.Empty;
        public string? JoinDate { get; set; }
        public string? IsActive { get; set; }
        public string? PreferredBowClass { get; set; }
    }
}
