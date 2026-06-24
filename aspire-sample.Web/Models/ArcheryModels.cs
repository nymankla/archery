namespace aspire_sample.Web;

public class MemberFeeOverview
{
    public Guid MemberId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public Guid? FeeId { get; set; }
    public FeeStatus? Status { get; set; }
    public decimal? Amount { get; set; }
    public DateOnly? DueDate { get; set; }
    public DateOnly? PaidDate { get; set; }
    public string FullName => $"{FirstName} {LastName}";
}

public class BulkFeeRequest
{
    public int Year { get; set; }
    public decimal Amount { get; set; }
    public DateOnly DueDate { get; set; }
}

public class ImportResult
{
    public int Imported { get; set; }
    public int Updated { get; set; }
    public List<string> Errors { get; set; } = [];
}

public enum BowClass { Recurve, Compound, Barebow, Traditional }
public enum AgeClass { Cadet, Junior, Senior, Master }
public enum Gender { Male, Female, NonBinary, Unknown }
public enum FeeStatus { Unpaid, Paid, Partial }
public enum CompetitionType { Indoor, Outdoor, ThreeD, Field }

public class Member
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public DateOnly DateOfBirth { get; set; }
    public DateOnly JoinDate { get; set; }
    public bool IsActive { get; set; } = true;
    public BowClass PreferredBowClass { get; set; }
    public string FullName => $"{FirstName} {LastName}";
}

public class MembershipFee
{
    public Guid Id { get; set; }
    public Guid MemberId { get; set; }
    public int Year { get; set; } = DateTime.Today.Year;
    public decimal Amount { get; set; }
    public DateOnly DueDate { get; set; }
    public DateOnly? PaidDate { get; set; }
    public FeeStatus Status { get; set; }
}

public class Competition
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public string Location { get; set; } = string.Empty;
    public string RoundType { get; set; } = string.Empty;
    public CompetitionType Type { get; set; }
    public string? Description { get; set; }
}

public class ExternalParticipant
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? ClubAffiliation { get; set; }
    public string FullName => $"{FirstName} {LastName}";
}

public class CompetitionParticipant
{
    public Guid Id { get; set; }
    public Guid CompetitionId { get; set; }
    public Guid? MemberId { get; set; }
    public Member? Member { get; set; }
    public Guid? ExternalParticipantId { get; set; }
    public ExternalParticipant? ExternalParticipant { get; set; }
    public BowClass BowClass { get; set; }
    public AgeClass AgeClass { get; set; }
    public Gender Gender { get; set; }
    public string ParticipantName => Member?.FullName ?? ExternalParticipant?.FullName ?? "Unknown";
}

public class CompetitionResult
{
    public Guid Id { get; set; }
    public Guid CompetitionId { get; set; }
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
    public string ParticipantName => Member?.FullName ?? ExternalParticipant?.FullName ?? "Unknown";
}
