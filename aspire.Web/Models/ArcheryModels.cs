namespace aspire.Web;

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
    public DateOnly DateOfBirth { get; set; }
    public string FullName => $"{FirstName} {LastName}";
}

public class BulkFeeRequest
{
    public int Year { get; set; }
    public decimal Amount { get; set; }
    public DateOnly DueDate { get; set; }
    public int? MinAge { get; set; }
    public string? AgeOp { get; set; }
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
    public string? Personnummer { get; set; }
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

public class DashboardData
{
    public DashboardMemberStats Members { get; set; } = new();
    public DashboardFeeStats Fees { get; set; } = new();
    public DashboardCompetitionStats Competitions { get; set; } = new();
    public List<DashboardTopScorer> TopScorers { get; set; } = [];
    public List<DashboardRecentCompetition> RecentCompetitions { get; set; } = [];
}

public class DashboardMemberStats
{
    public int TotalActive { get; set; }
    public int TotalInactive { get; set; }
    public int NewThisYear { get; set; }
}

public class DashboardFeeStats
{
    public int Paid { get; set; }
    public int Unpaid { get; set; }
    public int Partial { get; set; }
    public int NoFee { get; set; }
    public decimal TotalCollected { get; set; }
    public decimal TotalOutstanding { get; set; }
    public int CollectionRatePct { get; set; }
}

public class DashboardCompetitionStats
{
    public int TotalThisYear { get; set; }
    public int UpcomingCount { get; set; }
    public DashboardNextCompetition? NextCompetition { get; set; }
}

public class DashboardNextCompetition
{
    public string Name { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public string Location { get; set; } = string.Empty;
}

public class DashboardTopScorer
{
    public string MemberName { get; set; } = string.Empty;
    public BowClass BowClass { get; set; }
    public int Score { get; set; }
    public string CompetitionName { get; set; } = string.Empty;
}

public class DashboardRecentCompetition
{
    public string Name { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public string Location { get; set; } = string.Empty;
    public CompetitionType Type { get; set; }
    public int ParticipantCount { get; set; }
}

public class TrainingSessionDetail
{
    public Guid? SessionId { get; set; }
    public DateOnly Date { get; set; }
    public string? Notes { get; set; }
    public List<TrainingAttendeeInfo> Attendees { get; set; } = [];
}

public class TrainingAttendeeInfo
{
    public Guid AttendanceId { get; set; }
    public Guid? MemberId { get; set; }
    public string? MemberName { get; set; }
    public Guid? ExternalParticipantId { get; set; }
    public string? ExternalParticipantName { get; set; }
    public string ParticipantName => MemberName ?? ExternalParticipantName ?? "Unknown";
}

public class SaveTrainingAttendanceRequest
{
    public string? Notes { get; set; }
    public List<Guid> MemberIds { get; set; } = [];
    public List<Guid> ExternalParticipantIds { get; set; } = [];
}
