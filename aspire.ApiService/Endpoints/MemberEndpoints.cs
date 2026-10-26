using aspire.ApiService.Contracts;
using aspire.ApiService.Infrastructure;
using aspire.ApiService.Services;
using Wire = Archery.Client;

namespace aspire.ApiService.Endpoints;

public static class MemberEndpoints
{
    public static IEndpointRouteBuilder MapMemberEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/members").WithTags("Members").RequireAuthorization();

        group.MapGet("/", GetAll);
        group.MapGet("/export", Export);
        group.MapGet("/{id:guid}", GetById);
        group.MapPost("/", Create);
        group.MapPut("/{id:guid}", Update);
        group.MapDelete("/{id:guid}", Delete);

        return app;
    }

    static async Task<IResult> GetAll(IMemberService svc, CancellationToken ct)
        => Results.Ok((await svc.GetAllAsync(ct)).Select(m => m.ToContract()));

    static async Task<IResult> Export(string format, IMemberService svc, CancellationToken ct)
    {
        var file = await svc.ExportAsync(ExportFormats.Parse(format), ct);
        return Results.File(file.Content, file.ContentType, file.FileName);
    }

    static async Task<IResult> GetById(Guid id, IMemberService svc, CancellationToken ct)
        => EndpointResults.OkOrNotFound((await svc.GetByIdAsync(id, ct))?.ToContract());

    static async Task<IResult> Create(Wire.Member member, IMemberService svc, CancellationToken ct)
        => (await svc.CreateAsync(member.ToEntity(), ct))
            .ToHttpResult(created => Results.Created($"/members/{created.Id}", created.ToContract()));

    static async Task<IResult> Update(Guid id, Wire.Member input, IMemberService svc, CancellationToken ct)
        => (await svc.UpdateAsync(id, input.ToEntity(), ct))
            .ToHttpResult(updated => Results.Ok(updated.ToContract()));

    static async Task<IResult> Delete(Guid id, IMemberService svc, CancellationToken ct)
        => EndpointResults.NoContentOrNotFound(await svc.DeleteAsync(id, ct));
}
