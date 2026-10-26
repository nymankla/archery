using aspire.ApiService.Contracts;
using aspire.ApiService.Services;
using Wire = Archery.Client;

namespace aspire.ApiService.Endpoints;

public static class CompetitionEndpoints
{
    public static IEndpointRouteBuilder MapCompetitionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/competitions").WithTags("Competitions").RequireAuthorization();

        group.MapGet("/", GetAll);
        group.MapGet("/{id:guid}", GetById);
        group.MapPost("/", Create);
        group.MapPut("/{id:guid}", Update);
        group.MapDelete("/{id:guid}", Delete);

        return app;
    }

    static async Task<IResult> GetAll(ICompetitionService svc, CancellationToken ct)
        => Results.Ok((await svc.GetAllAsync(ct)).Select(c => c.ToContract()));

    static async Task<IResult> GetById(Guid id, ICompetitionService svc, CancellationToken ct)
        => EndpointResults.OkOrNotFound((await svc.GetByIdAsync(id, ct))?.ToContract());

    static async Task<IResult> Create(Wire.Competition competition, ICompetitionService svc, CancellationToken ct)
    {
        var created = await svc.CreateAsync(competition.ToEntity(), ct);
        return Results.Created($"/competitions/{created.Id}", created.ToContract());
    }

    static async Task<IResult> Update(Guid id, Wire.Competition input, ICompetitionService svc, CancellationToken ct)
        => EndpointResults.OkOrNotFound((await svc.UpdateAsync(id, input.ToEntity(), ct))?.ToContract());

    static async Task<IResult> Delete(Guid id, ICompetitionService svc, CancellationToken ct)
        => EndpointResults.NoContentOrNotFound(await svc.DeleteAsync(id, ct));
}
