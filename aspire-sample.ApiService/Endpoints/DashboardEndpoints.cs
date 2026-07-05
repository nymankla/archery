using aspire_sample.ApiService.Services;

namespace aspire_sample.ApiService.Endpoints;

public static class DashboardEndpoints
{
    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/dashboard", GetDashboard)
            .WithTags("Dashboard")
            .RequireAuthorization();
        return app;
    }

    static async Task<IResult> GetDashboard(IDashboardService svc, CancellationToken ct)
        => Results.Ok(await svc.GetDashboardAsync(ct));
}
