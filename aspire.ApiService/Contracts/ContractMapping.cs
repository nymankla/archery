using Archery.Data.Models;
using Wire = Archery.Client;

namespace aspire.ApiService.Contracts;

/// <summary>
/// Maps between the EF entities and the wire types in <c>Archery.Client</c>.
/// </summary>
/// <remarks>
/// Endpoints never bind or return entities. Binding an entity let a request smuggle in a
/// navigation graph — a member posted with <c>membershipFees</c>, or a result posted with a
/// whole <c>member</c> object — which EF then inserted alongside it. <c>ToEntity</c> copies
/// scalar fields only, so the request body can describe exactly one row.
/// </remarks>
public static class ContractMapping
{
    public static Member ToEntity(this Wire.Member m) => new()
    {
        Id = m.Id,
        FirstName = m.FirstName,
        LastName = m.LastName,
        Address = m.Address,
        Phone = m.Phone,
        Email = m.Email,
        Personnummer = m.Personnummer,
        DateOfBirth = m.DateOfBirth,
        JoinDate = m.JoinDate,
        IsActive = m.IsActive,
        PreferredBowClass = m.PreferredBowClass
    };

    public static Wire.Member ToContract(this Member m) => new()
    {
        Id = m.Id,
        FirstName = m.FirstName,
        LastName = m.LastName,
        Address = m.Address,
        Phone = m.Phone,
        Email = m.Email,
        Personnummer = m.Personnummer,
        DateOfBirth = m.DateOfBirth,
        JoinDate = m.JoinDate,
        IsActive = m.IsActive,
        PreferredBowClass = m.PreferredBowClass
    };

    public static MembershipFee ToEntity(this Wire.MembershipFee f) => new()
    {
        Id = f.Id,
        MemberId = f.MemberId,
        Year = f.Year,
        Amount = f.Amount,
        DueDate = f.DueDate,
        PaidDate = f.PaidDate,
        Status = f.Status
    };

    public static Wire.MembershipFee ToContract(this MembershipFee f) => new()
    {
        Id = f.Id,
        MemberId = f.MemberId,
        Year = f.Year,
        Amount = f.Amount,
        DueDate = f.DueDate,
        PaidDate = f.PaidDate,
        Status = f.Status
    };

    public static Competition ToEntity(this Wire.Competition c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        Date = c.Date,
        Location = c.Location,
        RoundType = c.RoundType,
        Type = c.Type,
        Description = c.Description
    };

    public static Wire.Competition ToContract(this Competition c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        Date = c.Date,
        Location = c.Location,
        RoundType = c.RoundType,
        Type = c.Type,
        Description = c.Description
    };

    public static ExternalParticipant ToEntity(this Wire.ExternalParticipant p) => new()
    {
        Id = p.Id,
        FirstName = p.FirstName,
        LastName = p.LastName,
        Phone = p.Phone,
        Email = p.Email,
        ClubAffiliation = p.ClubAffiliation
    };

    public static Wire.ExternalParticipant ToContract(this ExternalParticipant p) => new()
    {
        Id = p.Id,
        FirstName = p.FirstName,
        LastName = p.LastName,
        Phone = p.Phone,
        Email = p.Email,
        ClubAffiliation = p.ClubAffiliation
    };

    public static CompetitionParticipant ToEntity(this Wire.CompetitionParticipant p) => new()
    {
        Id = p.Id,
        CompetitionId = p.CompetitionId,
        MemberId = p.MemberId,
        ExternalParticipantId = p.ExternalParticipantId,
        BowClass = p.BowClass,
        AgeClass = p.AgeClass,
        Gender = p.Gender
    };

    public static Wire.CompetitionParticipant ToContract(this CompetitionParticipant p) => new()
    {
        Id = p.Id,
        CompetitionId = p.CompetitionId,
        MemberId = p.MemberId,
        Member = p.Member?.ToContract(),
        ExternalParticipantId = p.ExternalParticipantId,
        ExternalParticipant = p.ExternalParticipant?.ToContract(),
        BowClass = p.BowClass,
        AgeClass = p.AgeClass,
        Gender = p.Gender
    };

    public static CompetitionResult ToEntity(this Wire.CompetitionResult r) => new()
    {
        Id = r.Id,
        CompetitionId = r.CompetitionId,
        MemberId = r.MemberId,
        ExternalParticipantId = r.ExternalParticipantId,
        BowClass = r.BowClass,
        AgeClass = r.AgeClass,
        Gender = r.Gender,
        TotalScore = r.TotalScore,
        XCount = r.XCount,
        Placement = r.Placement,
        IsDisqualified = r.IsDisqualified,
        Notes = r.Notes
    };

    public static Wire.CompetitionResult ToContract(this CompetitionResult r) => new()
    {
        Id = r.Id,
        CompetitionId = r.CompetitionId,
        MemberId = r.MemberId,
        Member = r.Member?.ToContract(),
        ExternalParticipantId = r.ExternalParticipantId,
        ExternalParticipant = r.ExternalParticipant?.ToContract(),
        BowClass = r.BowClass,
        AgeClass = r.AgeClass,
        Gender = r.Gender,
        TotalScore = r.TotalScore,
        XCount = r.XCount,
        Placement = r.Placement,
        IsDisqualified = r.IsDisqualified,
        Notes = r.Notes
    };
}
