namespace aspire.ApiService.Models;

public class TrainingAttendance
{
    public Guid Id { get; set; }
    public Guid TrainingSessionId { get; set; }
    public TrainingSession TrainingSession { get; set; } = null!;

    public Guid? MemberId { get; set; }
    public Member? Member { get; set; }

    public Guid? ExternalParticipantId { get; set; }
    public ExternalParticipant? ExternalParticipant { get; set; }
}
