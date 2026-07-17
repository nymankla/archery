using aspire.ApiService.Models;

namespace aspire.ApiService.Services;

public interface ITrainingAttendanceService
{
    Task<IReadOnlyList<DateOnly>> GetTrainingDatesAsync(CancellationToken ct = default);
    Task<TrainingSessionDetail?> GetByDateAsync(DateOnly date, CancellationToken ct = default);
    Task<TrainingSessionDetail> SaveAttendanceAsync(DateOnly date, SaveAttendanceRequest request, CancellationToken ct = default);
}

public record TrainingSessionDetail(
    Guid SessionId,
    DateOnly Date,
    string? Notes,
    List<AttendeeInfo> Attendees);

public record AttendeeInfo(
    Guid AttendanceId,
    Guid? MemberId,
    string? MemberName,
    Guid? ExternalParticipantId,
    string? ExternalParticipantName);

public record SaveAttendanceRequest(
    string? Notes,
    List<Guid> MemberIds,
    List<Guid> ExternalParticipantIds);
