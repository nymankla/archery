namespace aspire_sample.ApiService.Models;

public record ImportResult(int Imported, int Updated, IReadOnlyList<string> Errors);
