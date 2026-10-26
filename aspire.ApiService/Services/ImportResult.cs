namespace aspire.ApiService.Services;

public record ImportResult(int Imported, int Updated, IReadOnlyList<string> Errors);
