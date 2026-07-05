namespace aspire_sample.ApiService.Services;

public interface IDashboardService
{
    Task<DashboardStats> GetDashboardAsync(CancellationToken ct = default);
}
