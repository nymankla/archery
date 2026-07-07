namespace aspire.ApiService.Models;

public class CompetitionResult
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
    public int TotalScore { get; set; }
    public int XCount { get; set; }
    public int? Placement { get; set; }
    public bool IsDisqualified { get; set; }
    public string? Notes { get; set; }
}
