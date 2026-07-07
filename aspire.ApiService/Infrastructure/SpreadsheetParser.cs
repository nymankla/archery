using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;

namespace aspire.ApiService.Infrastructure;

public static class SpreadsheetParser
{
    /// <summary>Parses a CSV or .xlsx upload into rows. Each row is a case-insensitive column→value map.</summary>
    public static List<Dictionary<string, string>> Parse(IFormFile file)
    {
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        using var stream = file.OpenReadStream();
        return ext == ".xlsx" ? ParseXlsx(stream) : ParseCsv(stream);
    }

    static List<Dictionary<string, string>> ParseCsv(Stream stream)
    {
        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HeaderValidated = null,
            MissingFieldFound = null
        });

        csv.Read();
        csv.ReadHeader();
        var headers = csv.HeaderRecord ?? [];

        var rows = new List<Dictionary<string, string>>();
        while (csv.Read())
        {
            var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var header in headers)
                row[header.Trim()] = csv.GetField(header)?.Trim() ?? string.Empty;
            rows.Add(row);
        }
        return rows;
    }

    static List<Dictionary<string, string>> ParseXlsx(Stream stream)
    {
        using var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheets.First();
        var headers = sheet.Row(1).CellsUsed()
            .Select(c => c.GetValue<string>().Trim())
            .ToList();

        return sheet.RowsUsed().Skip(1).Select(row =>
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < headers.Count; i++)
                dict[headers[i]] = row.Cell(i + 1).GetValue<string>().Trim();
            return dict;
        }).ToList();
    }

    /// <summary>Gets a column value, returning empty string when the column is absent.</summary>
    public static string Col(this Dictionary<string, string> row, string key)
        => row.TryGetValue(key, out var v) ? v : string.Empty;

    /// <summary>Returns null for whitespace-only strings, otherwise the trimmed value.</summary>
    public static string? NullIfEmpty(string? s)
        => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
