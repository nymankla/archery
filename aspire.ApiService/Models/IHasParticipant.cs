namespace aspire.ApiService.Models;

/// <summary>
/// A row that belongs to exactly one participant: either a member or an external guest.
/// </summary>
/// <remarks>
/// Implemented by competition registrations, results and training attendance. The database
/// enforces the rule with a check constraint (see <c>ConfigureSingleParticipant</c> in
/// <c>ArcheryDbContext</c>); services validate it first so the client gets a readable error.
/// </remarks>
public interface IHasParticipant
{
    Guid? MemberId { get; }
    Guid? ExternalParticipantId { get; }
}

public static class Participants
{
    public const string InvalidSelectionError = "Exactly one of MemberId or ExternalParticipantId must be provided.";

    public static bool HasExactlyOneParticipant(this IHasParticipant row)
        => (row.MemberId is null) != (row.ExternalParticipantId is null);
}
