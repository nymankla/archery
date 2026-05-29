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
        group.MapGet("/{id:guid}", GetFeeById);
        group.MapGet("/member/{memberId:guid}", GetFeesByMember);
        group.MapPost("/", CreateFee);
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
}
