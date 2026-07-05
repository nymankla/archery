using aspire_sample.ApiService.Models;
using aspire_sample.ApiService.Services;

namespace aspire_sample.ApiService.Endpoints;

public static class ExternalParticipantEndpoints
{
    public static IEndpointRouteBuilder MapExternalParticipantEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/external-participants").WithTags("ExternalParticipants").RequireAuthorization();

        group.MapGet("/", GetAll);
        group.MapGet("/{id:guid}", GetById);
        group.MapPost("/", Create);
        group.MapPut("/{id:guid}", Update);
        group.MapDelete("/{id:guid}", Delete);

        return app;
    }

    static async Task<IResult> GetAll(IExternalParticipantService svc, CancellationToken ct)
        => Results.Ok(await svc.GetAllAsync(ct));

    static async Task<IResult> GetById(Guid id, IExternalParticipantService svc, CancellationToken ct)
        => await svc.GetByIdAsync(id, ct) is { } p ? Results.Ok(p) : Results.NotFound();

    static async Task<IResult> Create(ExternalParticipant participant, IExternalParticipantService svc, CancellationToken ct)
    {
        var created = await svc.CreateAsync(participant, ct);
        return Results.Created($"/external-participants/{created.Id}", created);
    }

    static async Task<IResult> Update(Guid id, ExternalParticipant input, IExternalParticipantService svc, CancellationToken ct)
        => await svc.UpdateAsync(id, input, ct) is { } p ? Results.Ok(p) : Results.NotFound();

    static async Task<IResult> Delete(Guid id, IExternalParticipantService svc, CancellationToken ct)
        => await svc.DeleteAsync(id, ct) ? Results.NoContent() : Results.NotFound();
}
