using aspire.ApiService.Infrastructure;
using aspire.ApiService.Models;

namespace aspire.ApiService.Services;

public interface ITrainingAttendanceService
{
    Task<IReadOnlyList<DateOnly>> GetTrainingDatesAsync(CancellationToken ct = default);
    Task<TrainingSessionDetail?> GetByDateAsync(DateOnly date, CancellationToken ct = default);
    Task<Result<TrainingSessionDetail>> SaveAttendanceAsync(DateOnly date, SaveAttendanceRequest request, CancellationToken ct = default);
    Task<ExportFile?> ExportAsync(DateOnly date, ExportFormat format, CancellationToken ct = default);
}

public record TrainingSessionDetail(
    Guid SessionId,
    DateOnly Date,
    string? Notes,
    List<AttendeeInfo> Attendees);

public record AttendeeInfo(
    Guid AttendanceId,
    Guid? MemberId,
    string? MemberFirstName,
    string? MemberLastName,
    string? MemberPersonnummer,
    Guid? ExternalParticipantId,
    string? ExternalParticipantFirstName,
    string? ExternalParticipantLastName);

public record SaveAttendanceRequest(
    string? Notes,
    List<Guid> MemberIds,
    List<Guid> ExternalParticipantIds);
