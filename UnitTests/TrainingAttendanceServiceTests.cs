using aspire.ApiService.Data;
using aspire.ApiService.Models;
using aspire.ApiService.Services;
using Microsoft.EntityFrameworkCore;

namespace UnitTests;

public class TrainingAttendanceServiceTests
{
    static ArcheryDbContext CreateDb() => new(
        new DbContextOptionsBuilder<ArcheryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    static Member ActiveMember(string first, string last, bool isActive = true) => new()
    {
        Id = Guid.NewGuid(),
        FirstName = first,
        LastName = last,
        IsActive = isActive,
        DateOfBirth = new DateOnly(1990, 1, 1),
        JoinDate = new DateOnly(2020, 1, 1)
    };

    static ExternalParticipant External(string first, string last, string? club = null) => new()
    {
        Id = Guid.NewGuid(),
        FirstName = first,
        LastName = last,
        ClubAffiliation = club
    };

    [Fact]
    public async Task SaveAttendance_CreatesSessionAndStoresBothParticipantTypes()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateDb();
        var member = ActiveMember("Anna", "Archer");
        var external = External("Erik", "Guest", "Other Club");
        db.Members.Add(member);
        db.ExternalParticipants.Add(external);
        await db.SaveChangesAsync(ct);

        var svc = new TrainingAttendanceService(db);
        var date = new DateOnly(2026, 7, 17);

        var result = await svc.SaveAttendanceAsync(date, new SaveAttendanceRequest(
            "Evening practice",
            [member.Id],
            [external.Id]), ct);

        Assert.True(result.IsSuccess);
        Assert.Equal(date, result.Value!.Date);
        Assert.Equal("Evening practice", result.Value.Notes);
        Assert.Equal(2, result.Value.Attendees.Count);
        Assert.Single(await db.TrainingSessions.ToListAsync(ct));
        Assert.Equal(2, await db.TrainingAttendances.CountAsync(ct));
    }

    [Fact]
    public async Task SaveAttendance_ReplacesExistingAttendanceForDate()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateDb();
        var first = ActiveMember("Anna", "Archer");
        var second = ActiveMember("Bertil", "Bow");
        var external = External("Carla", "Guest");
        db.Members.AddRange(first, second);
        db.ExternalParticipants.Add(external);
        await db.SaveChangesAsync(ct);

        var svc = new TrainingAttendanceService(db);
        var date = new DateOnly(2026, 7, 18);

        await svc.SaveAttendanceAsync(date, new SaveAttendanceRequest(null, [first.Id], []), ct);
        var updatedResult = await svc.SaveAttendanceAsync(date, new SaveAttendanceRequest(
            "Updated",
            [second.Id],
            [external.Id]), ct);

        Assert.True(updatedResult.IsSuccess);
        var updated = updatedResult.Value!;
        Assert.Single(await db.TrainingSessions.ToListAsync(ct));
        Assert.Equal(2, await db.TrainingAttendances.CountAsync(ct));
        Assert.DoesNotContain(updated.Attendees, a => a.MemberId == first.Id);
        Assert.Contains(updated.Attendees, a => a.MemberId == second.Id);
        Assert.Contains(updated.Attendees, a => a.ExternalParticipantId == external.Id);
        Assert.Equal("Updated", updated.Notes);
    }

    [Fact]
    public async Task GetByDate_ReturnsNullWhenNoSessionExists()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateDb();
        var svc = new TrainingAttendanceService(db);

        var result = await svc.GetByDateAsync(new DateOnly(2026, 7, 19), ct);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetTrainingDates_ReturnsDatesDescending()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateDb();
        db.TrainingSessions.AddRange(
            new TrainingSession { Id = Guid.NewGuid(), Date = new DateOnly(2026, 7, 1) },
            new TrainingSession { Id = Guid.NewGuid(), Date = new DateOnly(2026, 7, 20) },
            new TrainingSession { Id = Guid.NewGuid(), Date = new DateOnly(2026, 7, 10) });
        await db.SaveChangesAsync(ct);

        var svc = new TrainingAttendanceService(db);
        var dates = await svc.GetTrainingDatesAsync(ct);

        Assert.Equal([
            new DateOnly(2026, 7, 20),
            new DateOnly(2026, 7, 10),
            new DateOnly(2026, 7, 1)
        ], dates);
    }
}
