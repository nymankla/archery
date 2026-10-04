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
/// </remarks>
public interface IArcheryApiClient
{
    Task<ImportResult?> ImportMembersAsync(byte[] content, string fileName, CancellationToken ct = default);
    Task<DashboardData?> GetDashboardAsync(CancellationToken ct = default);
    Task<Member[]?> GetMembersAsync(CancellationToken ct = default);
    Task<Member?> GetMemberAsync(Guid id, CancellationToken ct = default);
    Task<HttpResponseMessage> CreateMemberAsync(Member member, CancellationToken ct = default);
    Task<HttpResponseMessage> UpdateMemberAsync(Guid id, Member member, CancellationToken ct = default);
    Task<HttpResponseMessage> DeleteMemberAsync(Guid id, CancellationToken ct = default);
    Task<ExportedFile?> ExportMembersAsync(string format, CancellationToken ct = default);
    Task<MembershipFee[]?> GetFeesByMemberAsync(Guid memberId, CancellationToken ct = default);
    Task<MemberFeeOverview[]?> GetFeeOverviewAsync(int year, CancellationToken ct = default);
    Task<HttpResponseMessage> BulkCreateFeesAsync(BulkFeeRequest req, CancellationToken ct = default);
    Task<HttpResponseMessage> CreateFeeAsync(MembershipFee fee, CancellationToken ct = default);
    Task<HttpResponseMessage> UpdateFeeAsync(Guid id, MembershipFee fee, CancellationToken ct = default);
    Task<HttpResponseMessage> DeleteFeeAsync(Guid id, CancellationToken ct = default);
    Task<Competition[]?> GetCompetitionsAsync(CancellationToken ct = default);
    Task<Competition?> GetCompetitionAsync(Guid id, CancellationToken ct = default);
    Task<HttpResponseMessage> CreateCompetitionAsync(Competition competition, CancellationToken ct = default);
    Task<HttpResponseMessage> UpdateCompetitionAsync(Guid id, Competition competition, CancellationToken ct = default);
    Task<HttpResponseMessage> DeleteCompetitionAsync(Guid id, CancellationToken ct = default);
    Task<ImportResult?> ImportCompetitionsAsync(byte[] content, string fileName, CancellationToken ct = default);
    Task<ImportResult?> ImportExternalParticipantsAsync(byte[] content, string fileName, CancellationToken ct = default);
    Task<ExternalParticipant[]?> GetExternalParticipantsAsync(CancellationToken ct = default);
    Task<HttpResponseMessage> CreateExternalParticipantAsync(ExternalParticipant p, CancellationToken ct = default);
    Task<HttpResponseMessage> UpdateExternalParticipantAsync(Guid id, ExternalParticipant p, CancellationToken ct = default);
    Task<HttpResponseMessage> DeleteExternalParticipantAsync(Guid id, CancellationToken ct = default);
    Task<CompetitionParticipant[]?> GetParticipantsByCompetitionAsync(Guid competitionId, CancellationToken ct = default);
    Task<HttpResponseMessage> RegisterParticipantAsync(CompetitionParticipant participant, CancellationToken ct = default);
    Task<HttpResponseMessage> RemoveParticipantAsync(Guid id, CancellationToken ct = default);
    Task<CompetitionResult[]?> GetResultsByCompetitionAsync(Guid competitionId, CancellationToken ct = default);
    Task<HttpResponseMessage> CreateResultAsync(CompetitionResult result, CancellationToken ct = default);
    Task<HttpResponseMessage> UpdateResultAsync(Guid id, CompetitionResult result, CancellationToken ct = default);
    Task<HttpResponseMessage> DeleteResultAsync(Guid id, CancellationToken ct = default);
    Task<DateOnly[]?> GetTrainingDatesAsync(CancellationToken ct = default);
    Task<TrainingSessionDetail?> GetTrainingAttendanceByDateAsync(DateOnly date, CancellationToken ct = default);
    Task<HttpResponseMessage> SaveTrainingAttendanceAsync(DateOnly date, SaveTrainingAttendanceRequest request, CancellationToken ct = default);
    Task<ExportedFile?> ExportTrainingAttendanceAsync(DateOnly date, string format, CancellationToken ct = default);
}
