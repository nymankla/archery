using aspire.ApiService.Services;

namespace aspire.ApiService.Endpoints;

public static class TrainingAttendanceEndpoints
{
    public static IEndpointRouteBuilder MapTrainingAttendanceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/training-attendance").WithTags("TrainingAttendance").RequireAuthorization();

        group.MapGet("/dates", GetDates);
        group.MapGet("/by-date", GetByDate);
        group.MapPut("/by-date", SaveAttendance);

        return app;
    }

    static async Task<IResult> GetDates(ITrainingAttendanceService svc, CancellationToken ct)
        => Results.Ok(await svc.GetTrainingDatesAsync(ct));

    static async Task<IResult> GetByDate(DateOnly date, ITrainingAttendanceService svc, CancellationToken ct)
        => await svc.GetByDateAsync(date, ct) is { } detail
            ? Results.Ok(detail)
            : Results.Ok(new { sessionId = (Guid?)null, date, notes = (string?)null, attendees = new List<object>() });

    static async Task<IResult> SaveAttendance(DateOnly date, SaveAttendanceRequest request, ITrainingAttendanceService svc, CancellationToken ct)
    {
        var result = await svc.SaveAttendanceAsync(date, request, ct);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(new { errors = result.Errors });
    }
}
