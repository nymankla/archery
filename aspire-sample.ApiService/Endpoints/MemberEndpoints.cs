using aspire_sample.ApiService.Data;
using aspire_sample.ApiService.Models;
using Microsoft.EntityFrameworkCore;

namespace aspire_sample.ApiService.Endpoints;

public static class MemberEndpoints
{
    public static IEndpointRouteBuilder MapMemberEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/members").WithTags("Members").RequireAuthorization();

        group.MapGet("/", GetAllMembers);
        group.MapGet("/{id:guid}", GetMemberById);
        group.MapPost("/", CreateMember);
        group.MapPut("/{id:guid}", UpdateMember);
        group.MapDelete("/{id:guid}", DeleteMember);

        return app;
    }

    static async Task<IResult> GetAllMembers(ArcheryDbContext db, CancellationToken ct)
        => Results.Ok(await db.Members.AsNoTracking().OrderBy(m => m.LastName).ThenBy(m => m.FirstName).ToListAsync(ct));

    static async Task<IResult> GetMemberById(Guid id, ArcheryDbContext db, CancellationToken ct)
        => await db.Members.FindAsync([id], ct) is { } m
            ? Results.Ok(m)
            : Results.NotFound();

    static async Task<IResult> CreateMember(Member member, ArcheryDbContext db, CancellationToken ct)
    {
        member.Id = Guid.NewGuid();
        db.Members.Add(member);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/members/{member.Id}", member);
    }

    static async Task<IResult> UpdateMember(Guid id, Member input, ArcheryDbContext db, CancellationToken ct)
    {
        var member = await db.Members.FindAsync([id], ct);
        if (member is null) return Results.NotFound();
        member.FirstName = input.FirstName;
        member.LastName = input.LastName;
        member.Address = input.Address;
        member.Phone = input.Phone;
        member.Email = input.Email;
        member.DateOfBirth = input.DateOfBirth;
        member.JoinDate = input.JoinDate;
        member.IsActive = input.IsActive;
        member.PreferredBowClass = input.PreferredBowClass;
        await db.SaveChangesAsync(ct);
        return Results.Ok(member);
    }

    static async Task<IResult> DeleteMember(Guid id, ArcheryDbContext db, CancellationToken ct)
    {
        var member = await db.Members.FindAsync([id], ct);
        if (member is null) return Results.NotFound();
        db.Members.Remove(member);
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }
}
