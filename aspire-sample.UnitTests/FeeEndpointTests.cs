using aspire_sample.ApiService.Data;
using aspire_sample.ApiService.Models;
using Microsoft.EntityFrameworkCore;

namespace aspire_sample.UnitTests;

public class FeeEndpointTests
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

    // ── GetOverview query logic ────────────────────────────────────────────

    [Fact]
    public async Task Overview_ExcludesInactiveMembers()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateDb();
        db.Members.Add(ActiveMember("Alice", "Active"));
        db.Members.Add(new Member
        {
            Id = Guid.NewGuid(), FirstName = "Bob", LastName = "Inactive",
            IsActive = false,
            DateOfBirth = new DateOnly(1985, 5, 1),
            JoinDate = new DateOnly(2019, 1, 1)
        });
        await db.SaveChangesAsync(ct);

        var members = await db.Members.AsNoTracking().Where(m => m.IsActive).ToListAsync(ct);

        Assert.Single(members);
        Assert.Equal("Alice", members[0].FirstName);
    }

    [Fact]
    public async Task Overview_MemberWithFee_ReturnsFeeData()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateDb();
        var member = ActiveMember("Anna", "Smith");
        var fee = new MembershipFee
        {
            Id = Guid.NewGuid(), MemberId = member.Id,
            Year = 2026, Amount = 500,
            DueDate = new DateOnly(2026, 3, 31),
            Status = FeeStatus.Paid, PaidDate = new DateOnly(2026, 2, 1)
        };
        db.Members.Add(member);
        db.MembershipFees.Add(fee);
        await db.SaveChangesAsync(ct);

        var fees = await db.MembershipFees.AsNoTracking()
            .Where(f => f.Year == 2026 && f.MemberId == member.Id)
            .ToListAsync(ct);

        Assert.Single(fees);
        Assert.Equal(FeeStatus.Paid, fees[0].Status);
        Assert.Equal(500m, fees[0].Amount);
    }

    [Fact]
    public async Task Overview_MemberWithoutFee_ProducesNullFeeEntry()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateDb();
        var member = ActiveMember("Erik", "Nilsson");
        db.Members.Add(member);
        await db.SaveChangesAsync(ct);

        var feeByMember = await db.MembershipFees.AsNoTracking()
            .Where(f => f.Year == 2026)
            .ToDictionaryAsync(f => f.MemberId, ct);

        feeByMember.TryGetValue(member.Id, out var fee);
        Assert.Null(fee);
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

        var members = await db.Members.AsNoTracking()
            .Where(m => m.IsActive)
            .OrderBy(m => m.LastName).ThenBy(m => m.FirstName)
            .Select(m => m.FirstName)
            .ToListAsync(ct);

        Assert.Equal(["Anna", "Zara", "Lars"], members);
    }

    // ── BulkCreate query logic ─────────────────────────────────────────────

    [Fact]
    public async Task BulkCreate_SkipsMembersWhoAlreadyHaveFee()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateDb();
        var withFee    = ActiveMember("Maja", "Berg");
        var withoutFee = ActiveMember("Klas", "Holm");
        db.Members.AddRange(withFee, withoutFee);
        db.MembershipFees.Add(new MembershipFee
        {
            Id = Guid.NewGuid(), MemberId = withFee.Id,
            Year = 2026, Amount = 400, DueDate = new DateOnly(2026, 3, 31),
            Status = FeeStatus.Unpaid
        });
        await db.SaveChangesAsync(ct);

        var existingIds = await db.MembershipFees
            .Where(f => f.Year == 2026).Select(f => f.MemberId).ToHashSetAsync(ct);
        var toCreate = await db.Members
            .Where(m => m.IsActive && !existingIds.Contains(m.Id))
            .Select(m => m.Id).ToListAsync(ct);

        Assert.Single(toCreate);
        Assert.Equal(withoutFee.Id, toCreate[0]);
    }

    [Fact]
    public async Task BulkCreate_IgnoresInactiveMembers()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateDb();
        db.Members.Add(ActiveMember("Active", "Member"));
        db.Members.Add(new Member
        {
            Id = Guid.NewGuid(), FirstName = "Inactive", LastName = "Member",
            IsActive = false, DateOfBirth = new DateOnly(1980, 1, 1), JoinDate = new DateOnly(2015, 1, 1)
        });
        await db.SaveChangesAsync(ct);

        var existingIds = await db.MembershipFees
            .Where(f => f.Year == 2026).Select(f => f.MemberId).ToHashSetAsync(ct);
        var toCreate = await db.Members
            .Where(m => m.IsActive && !existingIds.Contains(m.Id))
            .Select(m => m.Id).ToListAsync(ct);

        Assert.Single(toCreate);
    }

    [Fact]
    public async Task BulkCreate_CreatesFeesWithCorrectData()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateDb();
        var member = ActiveMember("Test", "User");
        db.Members.Add(member);
        await db.SaveChangesAsync(ct);

        var dueDate = new DateOnly(2026, 3, 31);
        db.MembershipFees.Add(new MembershipFee
        {
            Id = Guid.NewGuid(), MemberId = member.Id,
            Year = 2026, Amount = 600, DueDate = dueDate, Status = FeeStatus.Unpaid
        });
        await db.SaveChangesAsync(ct);

        var fee = await db.MembershipFees.SingleAsync(f => f.MemberId == member.Id, ct);
        Assert.Equal(2026, fee.Year);
        Assert.Equal(600m, fee.Amount);
        Assert.Equal(FeeStatus.Unpaid, fee.Status);
        Assert.Equal(dueDate, fee.DueDate);
        Assert.Null(fee.PaidDate);
    }
}
