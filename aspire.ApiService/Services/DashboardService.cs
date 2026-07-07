using aspire.ApiService.Data;
using aspire.ApiService.Models;
using Microsoft.EntityFrameworkCore;

namespace aspire.ApiService.Services;

public class DashboardService(ArcheryDbContext db) : IDashboardService
{
    public async Task<DashboardStats> GetDashboardAsync(CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var year  = today.Year;

        var totalActive   = await db.Members.CountAsync(m => m.IsActive, ct);
        var totalInactive = await db.Members.CountAsync(m => !m.IsActive, ct);
        var newThisYear   = await db.Members.CountAsync(m => m.IsActive && m.JoinDate.Year == year, ct);

        var feeStats = await db.MembershipFees
            .AsNoTracking()
            .Where(f => f.Year == year)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Paid = g.Count(f => f.Status == FeeStatus.Paid),
                Unpaid = g.Count(f => f.Status == FeeStatus.Unpaid),
                Partial = g.Count(f => f.Status == FeeStatus.Partial),
                TotalCollected = g.Where(f => f.Status == FeeStatus.Paid).Sum(f => f.Amount),
                TotalOutstanding = g.Where(f => f.Status != FeeStatus.Paid).Sum(f => f.Amount)
            })
            .SingleOrDefaultAsync(ct);

        var paid             = feeStats?.Paid ?? 0;
        var unpaid           = feeStats?.Unpaid ?? 0;
        var partial          = feeStats?.Partial ?? 0;
        var noFee            = Math.Max(totalActive - (feeStats?.Total ?? 0), 0);
        var totalCollected   = feeStats?.TotalCollected ?? 0;
        var totalOutstanding = feeStats?.TotalOutstanding ?? 0;
        var collectionRatePct = totalActive > 0
            ? (int)Math.Round(paid * 100.0 / totalActive)
            : 0;

        var thisYearCount = await db.Competitions
            .AsNoTracking()
            .CountAsync(c => c.Date.Year == year, ct);

        var upcoming = await db.Competitions
            .AsNoTracking()
            .Where(c => c.Date > today)
            .OrderBy(c => c.Date)
            .Select(c => new { c.Name, c.Date, c.Location })
            .ToListAsync(ct);
        var nextComp = upcoming.FirstOrDefault();

        var recentComps = await db.Competitions
            .AsNoTracking()
            .Where(c => c.Date <= today)
            .OrderByDescending(c => c.Date)
            .Take(3)
            .Select(c => new { c.Id, c.Name, c.Date, c.Location, c.Type })
            .ToListAsync(ct);

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
