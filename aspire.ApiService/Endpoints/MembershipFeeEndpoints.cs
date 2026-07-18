using aspire.ApiService.Models;
using aspire.ApiService.Services;
using aspire.ApiService.Infrastructure;

namespace aspire.ApiService.Endpoints;

public static class MembershipFeeEndpoints
{
    public static IEndpointRouteBuilder MapMembershipFeeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/membership-fees").WithTags("MembershipFees").RequireAuthorization();

        group.MapGet("/", GetAll);
        group.MapGet("/overview", GetOverview);
        group.MapGet("/{id:guid}", GetById);
        group.MapGet("/member/{memberId:guid}", GetByMember);
        group.MapPost("/", Create);
        group.MapPost("/bulk", BulkCreate);
        group.MapPut("/{id:guid}", Update);
        group.MapDelete("/{id:guid}", Delete);

        return app;
    }

    static async Task<IResult> GetAll(IMembershipFeeService svc, Guid? memberId, CancellationToken ct)
        => Results.Ok(await svc.GetAllAsync(memberId, ct));

    static async Task<IResult> GetById(Guid id, IMembershipFeeService svc, CancellationToken ct)
        => await svc.GetByIdAsync(id, ct) is { } f ? Results.Ok(f) : Results.NotFound();

    static async Task<IResult> GetByMember(Guid memberId, IMembershipFeeService svc, CancellationToken ct)
        => Results.Ok(await svc.GetByMemberAsync(memberId, ct));

    static async Task<IResult> GetOverview(int? year, IMembershipFeeService svc, CancellationToken ct)
        => Results.Ok(await svc.GetOverviewAsync(year ?? DateTime.Today.Year, ct));

    static async Task<IResult> Create(MembershipFee fee, IMembershipFeeService svc, CancellationToken ct)
    {
        var result = await svc.CreateAsync(fee, ct);
        if (result.IsSuccess)
        {
            var created = result.Value!;
            return Results.Created($"/membership-fees/{created.Id}", created);
        }

        return Results.BadRequest(new { errors = result.Errors });
    }

    static async Task<IResult> BulkCreate(BulkFeeRequest req, IMembershipFeeService svc, CancellationToken ct)
    {
        var result = await svc.BulkCreateAsync(req.Year, req.Amount, req.DueDate, req.MinAge, req.AgeOp, ct);
        if (result.IsSuccess)
        {
            var count = result.Value;
            return Results.Ok(new { Created = count });
        }

        return Results.BadRequest(new { errors = result.Errors });
    }

    static async Task<IResult> Update(Guid id, MembershipFee input, IMembershipFeeService svc, CancellationToken ct)
    {
        var result = await svc.UpdateAsync(id, input, ct);
        if (result.IsSuccess)
        {
            return Results.Ok(result.Value!);
        }

        return result.Errors.Contains("Membership fee not found.")
            ? Results.NotFound()
            : Results.BadRequest(new { errors = result.Errors });
    }

    static async Task<IResult> Delete(Guid id, IMembershipFeeService svc, CancellationToken ct)
        => await svc.DeleteAsync(id, ct) ? Results.NoContent() : Results.NotFound();

    private record BulkFeeRequest(int Year, decimal Amount, DateOnly DueDate, int? MinAge = null, string? AgeOp = null);
}
