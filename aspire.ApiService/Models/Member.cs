namespace aspire.ApiService.Models;

public class Member
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Personnummer { get; set; }
    public DateOnly DateOfBirth { get; set; }
    public DateOnly JoinDate { get; set; }
    public bool IsActive { get; set; } = true;
    public BowClass PreferredBowClass { get; set; }

    public ICollection<MembershipFee> MembershipFees { get; set; } = [];
    public ICollection<CompetitionResult> CompetitionResults { get; set; } = [];
}
