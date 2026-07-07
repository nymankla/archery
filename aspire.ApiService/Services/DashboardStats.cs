using aspire.ApiService.Models;

namespace aspire.ApiService.Services;

public record DashboardStats(
    DashboardMemberStats Members,
    DashboardFeeStats Fees,
    DashboardCompetitionStats Competitions,
    IReadOnlyList<DashboardTopScorer> TopScorers,
    IReadOnlyList<DashboardRecentCompetition> RecentCompetitions);

public record DashboardMemberStats(int TotalActive, int TotalInactive, int NewThisYear);

public record DashboardFeeStats(
    int Paid, int Unpaid, int Partial, int NoFee,
    decimal TotalCollected, decimal TotalOutstanding, int CollectionRatePct);

public record DashboardCompetitionStats(
    int TotalThisYear, int UpcomingCount, DashboardNextCompetition? NextCompetition);

public record DashboardNextCompetition(string Name, DateOnly Date, string Location);

public record DashboardTopScorer(
    string MemberName, BowClass BowClass, int Score, string CompetitionName);

public record DashboardRecentCompetition(
    string Name, DateOnly Date, string Location, CompetitionType Type, int ParticipantCount);
