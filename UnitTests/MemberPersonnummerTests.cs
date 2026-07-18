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
        var result = PersonnummerParser.Normalize(input);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value);
    }

    [Theory]
    [InlineData("19900101-0018")]
    [InlineData("19901301-0017")]
    [InlineData("not-a-number")]
    public void PersonnummerParser_RejectsInvalidValues(string input)
    {
        var result = PersonnummerParser.Normalize(input);

        Assert.False(result.IsSuccess);
        Assert.Contains("Personnummer is invalid.", result.Errors);
    }

    [Fact]
    public async Task CreateAsync_AllowsMissingPersonnummer()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateDb();
        var svc = new MemberService(db);

        var member = await svc.CreateAsync(NewMember("Anna", "Optional"), ct);

        Assert.True(member.IsSuccess);
        Assert.Null(member.Value!.Personnummer);
    }

    [Fact]
    public async Task CreateAsync_NormalizesPersonnummerBeforeSaving()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateDb();
        var svc = new MemberService(db);

        var member = await svc.CreateAsync(NewMember("Anna", "Normalized", "900101-0017"), ct);

        Assert.True(member.IsSuccess);
        Assert.Equal("199001010017", member.Value!.Personnummer);
    }

    [Fact]
    public async Task CreateAsync_ReturnsValidationError_ForDuplicatePersonnummer()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateDb();
        var svc = new MemberService(db);
        await svc.CreateAsync(NewMember("Anna", "First", "900101-0017"), ct);

        var result = await svc.CreateAsync(NewMember("Bertil", "Second", "199001010017"), ct);

        Assert.False(result.IsSuccess);
        Assert.Contains("A member with this personnummer already exists.", result.Errors);
    }

    [Fact]
    public async Task UpdateAsync_AllowsExistingMemberToKeepSamePersonnummer()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateDb();
        var svc = new MemberService(db);
        var created = await svc.CreateAsync(NewMember("Anna", "Keep", "900101-0017"), ct);

        Assert.True(created.IsSuccess);
        created.Value!.Phone = "123";
        created.Value.Personnummer = "19900101-0017";

        var updated = await svc.UpdateAsync(created.Value.Id, created.Value, ct);

        Assert.True(updated.IsSuccess);
        Assert.Equal("199001010017", updated.Value!.Personnummer);
        Assert.Equal("123", updated.Value.Phone);
    }
}
