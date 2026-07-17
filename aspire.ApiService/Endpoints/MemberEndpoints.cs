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
        try
        {
            var created = await svc.CreateAsync(member, ct);
            return Results.Created($"/members/{created.Id}", created);
        }
        catch (ConflictException ex)
        {
            return Results.Conflict(new { message = ex.Message });
        }
    }

    static async Task<IResult> Update(Guid id, Member input, IMemberService svc, CancellationToken ct)
    {
        try
        {
            return await svc.UpdateAsync(id, input, ct) is { } m ? Results.Ok(m) : Results.NotFound();
        }
        catch (ConflictException ex)
        {
            return Results.Conflict(new { message = ex.Message });
        }
    }

    static async Task<IResult> Delete(Guid id, IMemberService svc, CancellationToken ct)
        => await svc.DeleteAsync(id, ct) ? Results.NoContent() : Results.NotFound();
}
