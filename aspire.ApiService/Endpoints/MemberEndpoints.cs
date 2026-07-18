using aspire.ApiService.Models;
using aspire.ApiService.Services;
using aspire.ApiService.Infrastructure;

namespace aspire.ApiService.Endpoints;

public static class MemberEndpoints
{
    public static IEndpointRouteBuilder MapMemberEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/members").WithTags("Members").RequireAuthorization();

        group.MapGet("/", GetAll);
        group.MapGet("/{id:guid}", GetById);
        group.MapPost("/", Create);
        group.MapPut("/{id:guid}", Update);
        group.MapDelete("/{id:guid}", Delete);

        return app;
    }

    static async Task<IResult> GetAll(IMemberService svc, CancellationToken ct)
        => Results.Ok(await svc.GetAllAsync(ct));

    static async Task<IResult> GetById(Guid id, IMemberService svc, CancellationToken ct)
        => await svc.GetByIdAsync(id, ct) is { } m ? Results.Ok(m) : Results.NotFound();

    static async Task<IResult> Create(Member member, IMemberService svc, CancellationToken ct)
    {
        var result = await svc.CreateAsync(member, ct);
        if (result.IsSuccess)
        {
            var created = result.Value!;
            return Results.Created($"/members/{created.Id}", created);
        }

        return Results.BadRequest(new { errors = result.Errors });
    }

    static async Task<IResult> Update(Guid id, Member input, IMemberService svc, CancellationToken ct)
    {
        var result = await svc.UpdateAsync(id, input, ct);
        if (result.IsSuccess)
        {
            return Results.Ok(result.Value!);
        }

        return result.Errors.Contains("Member not found.")
            ? Results.NotFound()
            : Results.BadRequest(new { errors = result.Errors });
    }

    static async Task<IResult> Delete(Guid id, IMemberService svc, CancellationToken ct)
        => await svc.DeleteAsync(id, ct) ? Results.NoContent() : Results.NotFound();
}
