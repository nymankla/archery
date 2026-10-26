namespace Archery.Client;

/// <summary>
/// The API surface of <see cref="ArcheryApiClient"/>.
/// </summary>
/// <remarks>
/// Exists so view models can be unit tested without standing up an
/// <see cref="HttpMessageHandler"/>. aspire.Web keeps injecting the concrete class.
///
/// The import and export members stay on the contract because the Blazor host uses them;
/// the mobile client simply never calls them.
///
/// Every member throws <see cref="ArcheryApiException"/> on a non-success status, with the
/// server's messages already parsed; none returns a raw response or null to signal failure.
/// </remarks>
public interface IArcheryApiClient
{
    Task<ImportResult?> ImportMembersAsync(byte[] content, string fileName, CancellationToken ct = default);
    Task<DashboardData?> GetDashboardAsync(CancellationToken ct = default);
    Task<Member[]?> GetMembersAsync(CancellationToken ct = default);
    Task<Member?> GetMemberAsync(Guid id, CancellationToken ct = default);
    Task CreateMemberAsync(Member member, CancellationToken ct = default);
    Task UpdateMemberAsync(Guid id, Member member, CancellationToken ct = default);
    Task DeleteMemberAsync(Guid id, CancellationToken ct = default);
    Task<ExportedFile?> ExportMembersAsync(string format, CancellationToken ct = default);
    Task<MembershipFee[]?> GetFeesByMemberAsync(Guid memberId, CancellationToken ct = default);
    Task<MemberFeeOverview[]?> GetFeeOverviewAsync(int year, CancellationToken ct = default);
    Task BulkCreateFeesAsync(BulkFeeRequest req, CancellationToken ct = default);
    Task CreateFeeAsync(MembershipFee fee, CancellationToken ct = default);
    Task UpdateFeeAsync(Guid id, MembershipFee fee, CancellationToken ct = default);
    Task DeleteFeeAsync(Guid id, CancellationToken ct = default);
    Task<Competition[]?> GetCompetitionsAsync(CancellationToken ct = default);
    Task<Competition?> GetCompetitionAsync(Guid id, CancellationToken ct = default);
    Task CreateCompetitionAsync(Competition competition, CancellationToken ct = default);
    Task UpdateCompetitionAsync(Guid id, Competition competition, CancellationToken ct = default);
    Task DeleteCompetitionAsync(Guid id, CancellationToken ct = default);
    Task<ImportResult?> ImportCompetitionsAsync(byte[] content, string fileName, CancellationToken ct = default);
    Task<ImportResult?> ImportExternalParticipantsAsync(byte[] content, string fileName, CancellationToken ct = default);
    Task<ExternalParticipant[]?> GetExternalParticipantsAsync(CancellationToken ct = default);
    Task CreateExternalParticipantAsync(ExternalParticipant p, CancellationToken ct = default);
    Task UpdateExternalParticipantAsync(Guid id, ExternalParticipant p, CancellationToken ct = default);
    Task DeleteExternalParticipantAsync(Guid id, CancellationToken ct = default);
    Task<CompetitionParticipant[]?> GetParticipantsByCompetitionAsync(Guid competitionId, CancellationToken ct = default);
    Task RegisterParticipantAsync(CompetitionParticipant participant, CancellationToken ct = default);
    Task RemoveParticipantAsync(Guid id, CancellationToken ct = default);
    Task<CompetitionResult[]?> GetResultsByCompetitionAsync(Guid competitionId, CancellationToken ct = default);
    Task CreateResultAsync(CompetitionResult result, CancellationToken ct = default);
    Task UpdateResultAsync(Guid id, CompetitionResult result, CancellationToken ct = default);
    Task DeleteResultAsync(Guid id, CancellationToken ct = default);
    Task<DateOnly[]?> GetTrainingDatesAsync(CancellationToken ct = default);
    Task<TrainingSessionDetail?> GetTrainingAttendanceByDateAsync(DateOnly date, CancellationToken ct = default);
    Task SaveTrainingAttendanceAsync(DateOnly date, SaveTrainingAttendanceRequest request, CancellationToken ct = default);
    Task<ExportedFile?> ExportTrainingAttendanceAsync(DateOnly date, string format, CancellationToken ct = default);
}
