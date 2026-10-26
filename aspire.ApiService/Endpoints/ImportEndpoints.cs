using aspire.ApiService.Infrastructure;
using aspire.ApiService.Services;

namespace aspire.ApiService.Endpoints;

public static partial class ImportEndpoints
{
    public static IEndpointRouteBuilder MapImportEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapImport<IMemberService>("/members/import", "Members");
        app.MapImport<ICompetitionService>("/competitions/import", "Competitions");
        app.MapImport<IExternalParticipantService>("/external-participants/import", "ExternalParticipants");
        return app;
    }

    static void MapImport<TService>(this IEndpointRouteBuilder app, string pattern, string tag)
        where TService : ISpreadsheetImporter
        => app.MapPost(pattern, Import<TService>)
            .WithTags(tag)
            .DisableAntiforgery()
            .RequireAuthorization();

    static async Task<IResult> Import<TService>(
        IFormFile file, TService svc, ILogger<TService> logger, CancellationToken ct)
        where TService : ISpreadsheetImporter
    {
        try
        {
            return Results.Ok(await svc.ImportAsync(file, ct));
        }
        catch (SpreadsheetFormatException ex)
        {
            // The parser's message describes library internals; log it, tell the user what to fix.
            LogUnreadableFile(logger, ex, file.FileName);
            return EndpointResults.Errors(["The file could not be read. Upload a CSV or .xlsx file with a header row."]);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Rejected unreadable import file {FileName}")]
    static partial void LogUnreadableFile(ILogger logger, Exception exception, string fileName);
}
