using aspire.ApiService.Contracts;
using aspire.ApiService.Services;
using Wire = Archery.Client;

namespace aspire.ApiService.Endpoints;

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
        => Results.Ok((await svc.GetAllAsync(ct)).Select(p => p.ToContract()));

    static async Task<IResult> GetById(Guid id, IExternalParticipantService svc, CancellationToken ct)
        => EndpointResults.OkOrNotFound((await svc.GetByIdAsync(id, ct))?.ToContract());

    static async Task<IResult> Create(Wire.ExternalParticipant participant, IExternalParticipantService svc, CancellationToken ct)
    {
        var created = await svc.CreateAsync(participant.ToEntity(), ct);
        return Results.Created($"/external-participants/{created.Id}", created.ToContract());
    }

    static async Task<IResult> Update(Guid id, Wire.ExternalParticipant input, IExternalParticipantService svc, CancellationToken ct)
        => EndpointResults.OkOrNotFound((await svc.UpdateAsync(id, input.ToEntity(), ct))?.ToContract());

    static async Task<IResult> Delete(Guid id, IExternalParticipantService svc, CancellationToken ct)
        => EndpointResults.NoContentOrNotFound(await svc.DeleteAsync(id, ct));
}
