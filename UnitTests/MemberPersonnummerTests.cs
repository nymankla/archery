using aspire.ApiService.Data;
using aspire.ApiService.Infrastructure;
using aspire.ApiService.Models;
using aspire.ApiService.Services;
using Microsoft.EntityFrameworkCore;

namespace UnitTests;

public class MemberPersonnummerTests
{
    static ArcheryDbContext CreateDb() => new(
        new DbContextOptionsBuilder<ArcheryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    static Member NewMember(string first, string last, string? personnummer = null) => new()
    {
        Id = Guid.NewGuid(),
        FirstName = first,
        LastName = last,
        Personnummer = personnummer,
        IsActive = true,
        DateOfBirth = new DateOnly(1990, 1, 1),
        JoinDate = new DateOnly(2020, 1, 1)
    };

    [Theory]
    [InlineData("19900101-0017", "199001010017")]
    [InlineData("9001010017", "199001010017")]
    [InlineData("900101-0017", "199001010017")]
    public void PersonnummerParser_NormalizesValidFormats(string input, string expected)
    {
        var ok = PersonnummerParser.TryNormalize(input, out var normalized);

        Assert.True(ok);
        Assert.Equal(expected, normalized);
    }

    [Theory]
    [InlineData("19900101-0018")]
    [InlineData("19901301-0017")]
    [InlineData("not-a-number")]
    public void PersonnummerParser_RejectsInvalidValues(string input)
    {
        var ok = PersonnummerParser.TryNormalize(input, out var normalized);

        Assert.False(ok);
        Assert.Null(normalized);
    }

    [Fact]
    public async Task CreateAsync_AllowsMissingPersonnummer()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateDb();
        var svc = new MemberService(db);

        var member = await svc.CreateAsync(NewMember("Anna", "Optional"), ct);

        Assert.Null(member.Personnummer);
    }

    [Fact]
    public async Task CreateAsync_NormalizesPersonnummerBeforeSaving()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateDb();
        var svc = new MemberService(db);

        var member = await svc.CreateAsync(NewMember("Anna", "Normalized", "900101-0017"), ct);

        Assert.Equal("199001010017", member.Personnummer);
    }

    [Fact]
    public async Task CreateAsync_ThrowsConflict_ForDuplicatePersonnummer()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateDb();
        var svc = new MemberService(db);
        await svc.CreateAsync(NewMember("Anna", "First", "900101-0017"), ct);

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            svc.CreateAsync(NewMember("Bertil", "Second", "199001010017"), ct));

        Assert.Equal("A member with this personnummer already exists.", ex.Message);
    }

    [Fact]
    public async Task UpdateAsync_AllowsExistingMemberToKeepSamePersonnummer()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateDb();
        var svc = new MemberService(db);
        var created = await svc.CreateAsync(NewMember("Anna", "Keep", "900101-0017"), ct);

        created.Phone = "123";
        created.Personnummer = "19900101-0017";

        var updated = await svc.UpdateAsync(created.Id, created, ct);

        Assert.NotNull(updated);
        Assert.Equal("199001010017", updated!.Personnummer);
        Assert.Equal("123", updated.Phone);
    }
}
