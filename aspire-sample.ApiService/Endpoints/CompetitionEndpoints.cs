using aspire_sample.ApiService.Models;
using aspire_sample.ApiService.Services;

namespace aspire_sample.ApiService.Endpoints;

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
        => Results.Ok(await svc.GetAllAsync(ct));

    static async Task<IResult> GetById(Guid id, ICompetitionService svc, CancellationToken ct)
        => await svc.GetByIdAsync(id, ct) is { } c ? Results.Ok(c) : Results.NotFound();

    static async Task<IResult> Create(Competition competition, ICompetitionService svc, CancellationToken ct)
    {
        var created = await svc.CreateAsync(competition, ct);
        return Results.Created($"/competitions/{created.Id}", created);
    }

    static async Task<IResult> Update(Guid id, Competition input, ICompetitionService svc, CancellationToken ct)
        => await svc.UpdateAsync(id, input, ct) is { } c ? Results.Ok(c) : Results.NotFound();

    static async Task<IResult> Delete(Guid id, ICompetitionService svc, CancellationToken ct)
        => await svc.DeleteAsync(id, ct) ? Results.NoContent() : Results.NotFound();
}
