using aspire_sample.ApiService.Data;
using aspire_sample.ApiService.Models;
using Microsoft.EntityFrameworkCore;

namespace aspire_sample.ApiService.Endpoints;

public static class MembershipFeeEndpoints
{
    public static IEndpointRouteBuilder MapMembershipFeeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/membership-fees").WithTags("MembershipFees").RequireAuthorization();

        group.MapGet("/", GetAllFees);
        group.MapGet("/overview", GetOverview);
        group.MapGet("/{id:guid}", GetFeeById);
        group.MapGet("/member/{memberId:guid}", GetFeesByMember);
        group.MapPost("/", CreateFee);
        group.MapPost("/bulk", BulkCreate);
        group.MapPut("/{id:guid}", UpdateFee);
        group.MapDelete("/{id:guid}", DeleteFee);

        return app;
    }

    static async Task<IResult> GetAllFees(ArcheryDbContext db, Guid? memberId, CancellationToken ct)
    {
        var query = db.MembershipFees.AsNoTracking();
        if (memberId.HasValue) query = query.Where(f => f.MemberId == memberId.Value);
        return Results.Ok(await query.ToListAsync(ct));
    }

    static async Task<IResult> GetFeeById(Guid id, ArcheryDbContext db, CancellationToken ct)
        => await db.MembershipFees.FindAsync([id], ct) is { } f
            ? Results.Ok(f)
            : Results.NotFound();

    static async Task<IResult> GetFeesByMember(Guid memberId, ArcheryDbContext db, CancellationToken ct)
        => Results.Ok(await db.MembershipFees.AsNoTracking()
            .Where(f => f.MemberId == memberId)
            .ToListAsync(ct));

    static async Task<IResult> CreateFee(MembershipFee fee, ArcheryDbContext db, CancellationToken ct)
    {
        fee.Id = Guid.NewGuid();
        db.MembershipFees.Add(fee);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/membership-fees/{fee.Id}", fee);
    }

    static async Task<IResult> UpdateFee(Guid id, MembershipFee input, ArcheryDbContext db, CancellationToken ct)
    {
        var fee = await db.MembershipFees.FindAsync([id], ct);
        if (fee is null) return Results.NotFound();
        fee.Year = input.Year;
        fee.Amount = input.Amount;
        fee.DueDate = input.DueDate;
        fee.PaidDate = input.PaidDate;
        fee.Status = input.Status;
        await db.SaveChangesAsync(ct);
        return Results.Ok(fee);
    }

    static async Task<IResult> DeleteFee(Guid id, ArcheryDbContext db, CancellationToken ct)
    {
        var fee = await db.MembershipFees.FindAsync([id], ct);
        if (fee is null) return Results.NotFound();
        db.MembershipFees.Remove(fee);
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    static async Task<IResult> GetOverview(int? year, ArcheryDbContext db, CancellationToken ct)
    {
        var targetYear = year ?? DateTime.Today.Year;

        var members = await db.Members
            .AsNoTracking()
            .Where(m => m.IsActive)
            .OrderBy(m => m.LastName).ThenBy(m => m.FirstName)
            .ToListAsync(ct);

        var memberIds = members.Select(m => m.Id).ToList();

        var fees = await db.MembershipFees
            .AsNoTracking()
            .Where(f => f.Year == targetYear && memberIds.Contains(f.MemberId))
            .ToListAsync(ct);

        var feeByMember = fees.ToDictionary(f => f.MemberId);

        var result = members.Select(m =>
        {
            feeByMember.TryGetValue(m.Id, out var fee);
            return new MemberFeeOverviewDto(
                m.Id, m.FirstName, m.LastName, m.Email,
                fee?.Id, fee?.Status, fee?.Amount, fee?.DueDate, fee?.PaidDate);
        });

        return Results.Ok(result);
    }

    static async Task<IResult> BulkCreate(BulkFeeRequest req, ArcheryDbContext db, CancellationToken ct)
    {
        var existingMemberIds = await db.MembershipFees
            .Where(f => f.Year == req.Year)
            .Select(f => f.MemberId)
            .ToHashSetAsync(ct);

        var memberIds = await db.Members
            .Where(m => m.IsActive && !existingMemberIds.Contains(m.Id))
            .Select(m => m.Id)
            .ToListAsync(ct);

        foreach (var memberId in memberIds)
            db.MembershipFees.Add(new MembershipFee
            {
                Id = Guid.NewGuid(),
                MemberId = memberId,
                Year = req.Year,
                Amount = req.Amount,
                DueDate = req.DueDate,
                Status = FeeStatus.Unpaid
            });

        await db.SaveChangesAsync(ct);
        return Results.Ok(new { Created = memberIds.Count });
    }

    private record MemberFeeOverviewDto(
        Guid MemberId, string FirstName, string LastName, string? Email,
        Guid? FeeId, FeeStatus? Status, decimal? Amount, DateOnly? DueDate, DateOnly? PaidDate);

    private record BulkFeeRequest(int Year, decimal Amount, DateOnly DueDate);
}
