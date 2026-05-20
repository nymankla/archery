namespace aspire_sample.ApiService.Models;

public class ExternalParticipant
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? ClubAffiliation { get; set; }

    public ICollection<CompetitionResult> CompetitionResults { get; set; } = [];
}
