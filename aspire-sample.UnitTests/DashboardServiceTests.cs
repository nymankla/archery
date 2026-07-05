using aspire_sample.ApiService.Data;
using aspire_sample.ApiService.Models;
using aspire_sample.ApiService.Services;
using Microsoft.EntityFrameworkCore;

namespace aspire_sample.UnitTests;

public class DashboardServiceTests
{
    static ArcheryDbContext CreateDb() => new(
        new DbContextOptionsBuilder<ArcheryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    static Member ActiveMember(string first, string last, DateOnly? joinDate = null) => new()
    {
        Id = Guid.NewGuid(), FirstName = first, LastName = last,
        IsActive = true, DateOfBirth = new DateOnly(1990, 1, 1),
        JoinDate = joinDate ?? new DateOnly(2020, 1, 1)
    };

    static MembershipFee MakeFee(Guid memberId, int year, FeeStatus status, decimal amount) => new()
    {
        Id = Guid.NewGuid(), MemberId = memberId, Year = year,
        Amount = amount, Status = status, DueDate = new DateOnly(year, 3, 31)
    };

    static Competition MakeCompetition(string name, DateOnly date) => new()
    {
        Id = Guid.NewGuid(), Name = name, Date = date,
        Location = "Range", RoundType = "WA 18m", Type = CompetitionType.Indoor
    };

    // ── Member stats ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Dashboard_MemberStats_CountsActiveAndInactive()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateDb();
        var year = DateTime.Today.Year;

        db.Members.AddRange(
            ActiveMember("A", "B", new DateOnly(year, 1, 1)),
            ActiveMember("C", "D", new DateOnly(year - 1, 6, 1)),
            new Member
            {
                Id = Guid.NewGuid(), FirstName = "E", LastName = "F", IsActive = false,
                DateOfBirth = new DateOnly(1980, 1, 1), JoinDate = new DateOnly(2018, 1, 1)
            });
        await db.SaveChangesAsync(ct);

        var svc = new DashboardService(db);
        var stats = await svc.GetDashboardAsync(ct);

        Assert.Equal(2, stats.Members.TotalActive);
        Assert.Equal(1, stats.Members.TotalInactive);
        Assert.Equal(1, stats.Members.NewThisYear);
    }

    // ── Fee stats ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Dashboard_FeeStats_CountsByStatus()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateDb();
        var year = DateTime.Today.Year;

        var m1 = ActiveMember("A", "B");
        var m2 = ActiveMember("C", "D");
        var m3 = ActiveMember("E", "F");
        var m4 = ActiveMember("G", "H"); // no fee
        db.Members.AddRange(m1, m2, m3, m4);
        db.MembershipFees.AddRange(
            MakeFee(m1.Id, year, FeeStatus.Paid, 500),
            MakeFee(m2.Id, year, FeeStatus.Unpaid, 500),
            MakeFee(m3.Id, year, FeeStatus.Partial, 250));
        await db.SaveChangesAsync(ct);

        var svc = new DashboardService(db);
        var stats = await svc.GetDashboardAsync(ct);

        Assert.Equal(1, stats.Fees.Paid);
        Assert.Equal(1, stats.Fees.Unpaid);
        Assert.Equal(1, stats.Fees.Partial);
        Assert.Equal(1, stats.Fees.NoFee);
    }

    [Fact]
    public async Task Dashboard_FeeStats_SumsCollectedAndOutstanding()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateDb();
        var year = DateTime.Today.Year;

        var m1 = ActiveMember("A", "B");
        var m2 = ActiveMember("C", "D");
        var m3 = ActiveMember("E", "F");
        db.Members.AddRange(m1, m2, m3);
        db.MembershipFees.AddRange(
            MakeFee(m1.Id, year, FeeStatus.Paid, 500),
            MakeFee(m2.Id, year, FeeStatus.Paid, 500),
            MakeFee(m3.Id, year, FeeStatus.Unpaid, 500));
        await db.SaveChangesAsync(ct);

        var svc = new DashboardService(db);
        var stats = await svc.GetDashboardAsync(ct);

        Assert.Equal(1000m, stats.Fees.TotalCollected);
        Assert.Equal(500m, stats.Fees.TotalOutstanding);
    }

    [Fact]
    public async Task Dashboard_FeeStats_CollectionRateIsPercentOfActiveMembers()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateDb();
        var year = DateTime.Today.Year;

        var members = Enumerable.Range(1, 4).Select(i => ActiveMember($"M{i}", "X")).ToList();
        db.Members.AddRange(members);
        db.MembershipFees.AddRange(
            MakeFee(members[0].Id, year, FeeStatus.Paid, 500),
            MakeFee(members[1].Id, year, FeeStatus.Paid, 500),
            MakeFee(members[2].Id, year, FeeStatus.Paid, 500),
            MakeFee(members[3].Id, year, FeeStatus.Unpaid, 500));
        await db.SaveChangesAsync(ct);

        var svc = new DashboardService(db);
        var stats = await svc.GetDashboardAsync(ct);

        Assert.Equal(75, stats.Fees.CollectionRatePct);
    }

    // ── Competition stats ─────────────────────────────────────────────────────

    [Fact]
    public async Task Dashboard_CompetitionStats_SplitsPastAndUpcoming()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateDb();
        var today = DateOnly.FromDateTime(DateTime.Today);
        var year  = today.Year;

        db.Competitions.AddRange(
            MakeCompetition("Past 1",    today.AddDays(-10)),
            MakeCompetition("Past 2",    today.AddDays(-3)),
            MakeCompetition("Future 1",  today.AddDays(7)),
            MakeCompetition("Future 2",  today.AddDays(30)),
            MakeCompetition("Last Year", new DateOnly(year - 1, 6, 1)));
        await db.SaveChangesAsync(ct);

        var svc = new DashboardService(db);
        var stats = await svc.GetDashboardAsync(ct);

        Assert.Equal(4, stats.Competitions.TotalThisYear);
        Assert.Equal(2, stats.Competitions.UpcomingCount);
    }

    // ── Top scorers ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Dashboard_TopScorers_OrderedByScoreDescending()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateDb();
        var year = DateTime.Today.Year;

        var m1 = ActiveMember("High", "Scorer");
        var m2 = ActiveMember("Mid",  "Scorer");
        var m3 = ActiveMember("Low",  "Scorer");
        db.Members.AddRange(m1, m2, m3);

        var comp = MakeCompetition("Test Cup", new DateOnly(year, 5, 1));
        db.Competitions.Add(comp);

        db.CompetitionResults.AddRange(
            new CompetitionResult
            {
                Id = Guid.NewGuid(), CompetitionId = comp.Id, MemberId = m1.Id,
                TotalScore = 900, BowClass = BowClass.Recurve, AgeClass = AgeClass.Senior, Gender = Gender.Male
            },
            new CompetitionResult
            {
                Id = Guid.NewGuid(), CompetitionId = comp.Id, MemberId = m2.Id,
                TotalScore = 820, BowClass = BowClass.Compound, AgeClass = AgeClass.Senior, Gender = Gender.Female
            },
            new CompetitionResult
            {
                Id = Guid.NewGuid(), CompetitionId = comp.Id, MemberId = m3.Id,
                TotalScore = 750, BowClass = BowClass.Barebow, AgeClass = AgeClass.Junior, Gender = Gender.Male
            });
        await db.SaveChangesAsync(ct);

        var svc = new DashboardService(db);
        var stats = await svc.GetDashboardAsync(ct);

        Assert.Equal(3, stats.TopScorers.Count);
        Assert.Equal(900, stats.TopScorers[0].Score);
        Assert.Equal(820, stats.TopScorers[1].Score);
        Assert.Equal(750, stats.TopScorers[2].Score);
    }
}
