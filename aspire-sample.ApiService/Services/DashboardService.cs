using aspire_sample.ApiService.Data;
using aspire_sample.ApiService.Models;
using Microsoft.EntityFrameworkCore;

namespace aspire_sample.ApiService.Services;

public class DashboardService(ArcheryDbContext db) : IDashboardService
{
    public async Task<DashboardStats> GetDashboardAsync(CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var year  = today.Year;

        var totalActive   = await db.Members.CountAsync(m => m.IsActive, ct);
        var totalInactive = await db.Members.CountAsync(m => !m.IsActive, ct);
        var newThisYear   = await db.Members.CountAsync(m => m.IsActive && m.JoinDate.Year == year, ct);

        var yearFees = await db.MembershipFees
            .AsNoTracking()
            .Where(f => f.Year == year)
            .Select(f => new { f.Status, f.Amount })
            .ToListAsync(ct);

        var paid             = yearFees.Count(f => f.Status == FeeStatus.Paid);
        var unpaid           = yearFees.Count(f => f.Status == FeeStatus.Unpaid);
        var partial          = yearFees.Count(f => f.Status == FeeStatus.Partial);
        var noFee            = Math.Max(totalActive - yearFees.Count, 0);
        var totalCollected   = yearFees.Where(f => f.Status == FeeStatus.Paid).Sum(f => f.Amount);
        var totalOutstanding = yearFees.Where(f => f.Status != FeeStatus.Paid).Sum(f => f.Amount);
        var collectionRatePct = totalActive > 0
            ? (int)Math.Round(paid * 100.0 / totalActive)
            : 0;

        var allComps = await db.Competitions
            .AsNoTracking()
            .Select(c => new { c.Id, c.Name, c.Date, c.Location, c.Type })
            .ToListAsync(ct);

        var thisYearCount = allComps.Count(c => c.Date.Year == year);
        var upcoming      = allComps.Where(c => c.Date > today).OrderBy(c => c.Date).ToList();
        var nextComp      = upcoming.FirstOrDefault();

        var recentComps = allComps
            .Where(c => c.Date <= today)
            .OrderByDescending(c => c.Date)
            .Take(3)
            .ToList();

        Dictionary<Guid, int> participantCounts = [];
        if (recentComps.Count > 0)
        {
            var ids = recentComps.Select(c => c.Id).ToList();
            participantCounts = await db.CompetitionParticipants
                .AsNoTracking()
                .Where(p => ids.Contains(p.CompetitionId))
                .GroupBy(p => p.CompetitionId)
                .Select(g => new { CompId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.CompId, x => x.Count, ct);
        }

        var topScorers = await db.CompetitionResults
            .AsNoTracking()
            .Where(r => r.MemberId != null
                     && !r.IsDisqualified
                     && r.Competition.Date.Year == year)
            .OrderByDescending(r => r.TotalScore)
            .Take(5)
            .Select(r => new DashboardTopScorer(
                r.Member!.FirstName + " " + r.Member.LastName,
                r.BowClass,
                r.TotalScore,
                r.Competition.Name))
            .ToListAsync(ct);

        return new DashboardStats(
            new DashboardMemberStats(totalActive, totalInactive, newThisYear),
            new DashboardFeeStats(paid, unpaid, partial, noFee,
                totalCollected, totalOutstanding, collectionRatePct),
            new DashboardCompetitionStats(
                thisYearCount,
                upcoming.Count,
                nextComp is null ? null : new DashboardNextCompetition(nextComp.Name, nextComp.Date, nextComp.Location)),
            topScorers,
            recentComps.Select(c => new DashboardRecentCompetition(
                c.Name, c.Date, c.Location, c.Type,
                participantCounts.GetValueOrDefault(c.Id, 0))).ToList());
    }
}
