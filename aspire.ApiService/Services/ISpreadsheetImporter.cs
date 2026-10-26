using aspire.ApiService.Models;
using Microsoft.AspNetCore.Http;

namespace aspire.ApiService.Services;

/// <summary>A service that can upsert rows from a CSV or .xlsx upload.</summary>
public interface ISpreadsheetImporter
{
    Task<ImportResult> ImportAsync(IFormFile file, CancellationToken ct = default);
}
