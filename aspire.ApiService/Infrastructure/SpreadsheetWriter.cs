using ClosedXML.Excel;
using CsvHelper;
using System.Globalization;

namespace aspire.ApiService.Infrastructure;

public enum ExportFormat { Csv, Xlsx }

public record ExportFile(string FileName, string ContentType, byte[] Content);

/// <summary>Writes tabular data to CSV or .xlsx bytes. Counterpart to <see cref="SpreadsheetParser"/>.</summary>
public static class SpreadsheetWriter
{
    const string CsvContentType = "text/csv";
    const string XlsxContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    static readonly char[] FormulaTriggerChars = ['=', '+', '-', '@', '\t', '\r'];

    /// <summary>
    /// Neutralizes CSV/formula injection: a cell value starting with a spreadsheet
    /// formula-trigger character is prefixed with an apostrophe so Excel/LibreOffice
    /// render it as text instead of evaluating it as a formula when the export is opened.
    /// </summary>
    static string SanitizeCell(string value)
        => value.Length > 0 && FormulaTriggerChars.Contains(value[0]) ? "'" + value : value;

    /// <summary>Writes headers/rows as CSV or .xlsx depending on <paramref name="format"/>, naming the file from <paramref name="fileNameWithoutExtension"/>.</summary>
    public static ExportFile Write(
        ExportFormat format,
        string fileNameWithoutExtension,
        string sheetName,
        IReadOnlyList<string> headers,
        IEnumerable<IReadOnlyList<string?>> rows)
    {
        var rowList = rows as IReadOnlyList<IReadOnlyList<string?>> ?? rows.ToList();
        return format == ExportFormat.Xlsx
            ? new ExportFile($"{fileNameWithoutExtension}.xlsx", XlsxContentType, WriteXlsx(sheetName, headers, rowList))
            : new ExportFile($"{fileNameWithoutExtension}.csv", CsvContentType, WriteCsv(headers, rowList));
    }

    public static byte[] WriteCsv(IReadOnlyList<string> headers, IEnumerable<IReadOnlyList<string?>> rows)
    {
        using var stream = new MemoryStream();
        using (var writer = new StreamWriter(stream, leaveOpen: true))
        using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
        {
            foreach (var header in headers)
                csv.WriteField(header);
            csv.NextRecord();

            foreach (var row in rows)
            {
                foreach (var cell in row)
                    csv.WriteField(SanitizeCell(cell ?? string.Empty));
                csv.NextRecord();
            }
        }
        return stream.ToArray();
    }

    public static byte[] WriteXlsx(string sheetName, IReadOnlyList<string> headers, IEnumerable<IReadOnlyList<string?>> rows)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add(sheetName);

        for (var col = 0; col < headers.Count; col++)
            sheet.Cell(1, col + 1).Value = headers[col];
        sheet.Row(1).Style.Font.Bold = true;

        var rowIndex = 2;
        foreach (var row in rows)
        {
            for (var col = 0; col < row.Count; col++)
                sheet.Cell(rowIndex, col + 1).Value = SanitizeCell(row[col] ?? string.Empty);
            rowIndex++;
        }

        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
