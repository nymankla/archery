namespace aspire.ApiService.Models;

public record ImportResult(int Imported, int Updated, IReadOnlyList<string> Errors);
