using aspire.ApiService.Contracts;
using aspire.ApiService.Services;
using Wire = Archery.Client;

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
        => Results.Ok((await svc.GetAllAsync(memberId, ct)).Select(f => f.ToContract()));

    static async Task<IResult> GetById(Guid id, IMembershipFeeService svc, CancellationToken ct)
        => EndpointResults.OkOrNotFound((await svc.GetByIdAsync(id, ct))?.ToContract());

    static async Task<IResult> GetByMember(Guid memberId, IMembershipFeeService svc, CancellationToken ct)
        => Results.Ok((await svc.GetByMemberAsync(memberId, ct)).Select(f => f.ToContract()));

    static async Task<IResult> GetOverview(int? year, IMembershipFeeService svc, CancellationToken ct)
        => Results.Ok(await svc.GetOverviewAsync(year ?? DateTime.Today.Year, ct));

    static async Task<IResult> Create(Wire.MembershipFee fee, IMembershipFeeService svc, CancellationToken ct)
        => (await svc.CreateAsync(fee.ToEntity(), ct))
            .ToHttpResult(created => Results.Created($"/membership-fees/{created.Id}", created.ToContract()));

    static async Task<IResult> BulkCreate(Wire.BulkFeeRequest req, IMembershipFeeService svc, CancellationToken ct)
        => (await svc.BulkCreateAsync(req.Year, req.Amount, req.DueDate, req.MinAge, req.AgeOp, ct))
            .ToHttpResult(count => Results.Ok(new { Created = count }));

    static async Task<IResult> Update(Guid id, Wire.MembershipFee input, IMembershipFeeService svc, CancellationToken ct)
        => (await svc.UpdateAsync(id, input.ToEntity(), ct))
            .ToHttpResult(updated => Results.Ok(updated.ToContract()));

    static async Task<IResult> Delete(Guid id, IMembershipFeeService svc, CancellationToken ct)
        => EndpointResults.NoContentOrNotFound(await svc.DeleteAsync(id, ct));
}
