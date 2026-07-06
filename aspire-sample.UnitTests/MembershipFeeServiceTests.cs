using aspire_sample.ApiService.Data;
using aspire_sample.ApiService.Models;
using aspire_sample.ApiService.Services;
using Microsoft.EntityFrameworkCore;

namespace aspire_sample.UnitTests;

public class MembershipFeeServiceTests
{
    static ArcheryDbContext CreateDb() => new(
        new DbContextOptionsBuilder<ArcheryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    static Member ActiveMember(string first, string last) => new()
    {
        Id = Guid.NewGuid(), FirstName = first, LastName = last,
        IsActive = true,
        DateOfBirth = new DateOnly(1990, 1, 1),
        JoinDate = new DateOnly(2020, 1, 1)
    };

    // ── GetOverviewAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task Overview_ExcludesInactiveMembers()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateDb();
        db.Members.Add(ActiveMember("Alice", "Active"));
        db.Members.Add(new Member
        {
            Id = Guid.NewGuid(), FirstName = "Bob", LastName = "Inactive",
            IsActive = false, DateOfBirth = new DateOnly(1985, 5, 1), JoinDate = new DateOnly(2019, 1, 1)
        });
        await db.SaveChangesAsync(ct);

        var svc = new MembershipFeeService(db);
        var result = await svc.GetOverviewAsync(DateTime.Today.Year, ct);

        Assert.Single(result);
        Assert.Equal("Alice", result[0].FirstName);
    }

    [Fact]
    public async Task Overview_MemberWithFee_ReturnsFeeData()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateDb();
        var year = DateTime.Today.Year;
        var member = ActiveMember("Anna", "Smith");
        db.Members.Add(member);
        db.MembershipFees.Add(new MembershipFee
        {
            Id = Guid.NewGuid(), MemberId = member.Id, Year = year,
            Amount = 500, DueDate = new DateOnly(year, 3, 31),
            Status = FeeStatus.Paid, PaidDate = new DateOnly(year, 2, 1)
        });
        await db.SaveChangesAsync(ct);

        var svc = new MembershipFeeService(db);
        var result = await svc.GetOverviewAsync(year, ct);

        Assert.Single(result);
        Assert.Equal(FeeStatus.Paid, result[0].Status);
        Assert.Equal(500m, result[0].Amount);
    }

    [Fact]
    public async Task Overview_MemberWithoutFee_HasNullFeeFields()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateDb();
        db.Members.Add(ActiveMember("Erik", "Nilsson"));
        await db.SaveChangesAsync(ct);

        var svc = new MembershipFeeService(db);
        var result = await svc.GetOverviewAsync(DateTime.Today.Year, ct);

        Assert.Single(result);
        Assert.Null(result[0].FeeId);
        Assert.Null(result[0].Status);
    }

    [Fact]
    public async Task Overview_OrdersByLastNameThenFirstName()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateDb();
        db.Members.AddRange(
            ActiveMember("Zara", "Andersson"),
            ActiveMember("Anna", "Andersson"),
            ActiveMember("Lars", "Björk"));
        await db.SaveChangesAsync(ct);

        var svc = new MembershipFeeService(db);
        var result = await svc.GetOverviewAsync(DateTime.Today.Year, ct);

        Assert.Equal(["Anna", "Zara", "Lars"], result.Select(r => r.FirstName).ToArray());
    }

    // ── BulkCreateAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task BulkCreate_SkipsMembersWhoAlreadyHaveFee()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateDb();
        var year = DateTime.Today.Year;
        var withFee    = ActiveMember("Maja", "Berg");
        var withoutFee = ActiveMember("Klas", "Holm");
        db.Members.AddRange(withFee, withoutFee);
        db.MembershipFees.Add(new MembershipFee
        {
            Id = Guid.NewGuid(), MemberId = withFee.Id, Year = year,
            Amount = 400, DueDate = new DateOnly(year, 3, 31), Status = FeeStatus.Unpaid
        });
        await db.SaveChangesAsync(ct);

        var svc = new MembershipFeeService(db);
        var created = await svc.BulkCreateAsync(year, 500, new DateOnly(year, 3, 31), ct: ct);

        Assert.Equal(1, created);
        Assert.Equal(2, await db.MembershipFees.CountAsync(ct));
    }

    [Fact]
    public async Task BulkCreate_IgnoresInactiveMembers()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateDb();
        var year = DateTime.Today.Year;
        db.Members.Add(ActiveMember("Active", "Member"));
        db.Members.Add(new Member
        {
            Id = Guid.NewGuid(), FirstName = "Inactive", LastName = "Member",
            IsActive = false, DateOfBirth = new DateOnly(1980, 1, 1), JoinDate = new DateOnly(2015, 1, 1)
        });
        await db.SaveChangesAsync(ct);

        var svc = new MembershipFeeService(db);
        var created = await svc.BulkCreateAsync(year, 500, new DateOnly(year, 3, 31), ct: ct);

        Assert.Equal(1, created);
    }

    [Fact]
    public async Task BulkCreate_CreatesFeesWithCorrectData()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateDb();
        var year    = DateTime.Today.Year;
        var dueDate = new DateOnly(year, 3, 31);
        db.Members.Add(ActiveMember("Test", "User"));
        await db.SaveChangesAsync(ct);

        var svc = new MembershipFeeService(db);
        await svc.BulkCreateAsync(year, 600, dueDate, ct: ct);

        var fee = await db.MembershipFees.SingleAsync(ct);
        Assert.Equal(year, fee.Year);
        Assert.Equal(600m, fee.Amount);
        Assert.Equal(FeeStatus.Unpaid, fee.Status);
        Assert.Equal(dueDate, fee.DueDate);
        Assert.Null(fee.PaidDate);
    }
}
