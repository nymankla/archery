namespace aspire_sample.ApiService.Models;

public class CompetitionParticipant
{
    public Guid Id { get; set; }
    public Guid CompetitionId { get; set; }
    public Competition Competition { get; set; } = null!;
    public Guid? MemberId { get; set; }
    public Member? Member { get; set; }
    public Guid? ExternalParticipantId { get; set; }
    public ExternalParticipant? ExternalParticipant { get; set; }
    public BowClass BowClass { get; set; }
    public AgeClass AgeClass { get; set; }
    public Gender Gender { get; set; }
}
