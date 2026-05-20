using aspire_sample.ApiService.Models;
using Microsoft.EntityFrameworkCore;

namespace aspire_sample.ApiService.Data;

public class ArcheryDbContext(DbContextOptions<ArcheryDbContext> options) : DbContext(options)
{
    public DbSet<Member> Members => Set<Member>();
    public DbSet<MembershipFee> MembershipFees => Set<MembershipFee>();
    public DbSet<Competition> Competitions => Set<Competition>();
    public DbSet<ExternalParticipant> ExternalParticipants => Set<ExternalParticipant>();
    public DbSet<CompetitionResult> CompetitionResults => Set<CompetitionResult>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Member>(e =>
        {
            e.HasKey(m => m.Id);
            e.Property(m => m.FirstName).IsRequired().HasMaxLength(100);
            e.Property(m => m.LastName).IsRequired().HasMaxLength(100);
            e.Property(m => m.Address).HasMaxLength(300);
            e.Property(m => m.Phone).HasMaxLength(30);
            e.Property(m => m.Email).HasMaxLength(200);
            e.Property(m => m.PreferredBowClass).HasConversion<string>().HasMaxLength(20);
            e.HasIndex(m => m.Email);
        });

        modelBuilder.Entity<MembershipFee>(e =>
        {
            e.HasKey(f => f.Id);
            e.Property(f => f.Amount).HasColumnType("numeric(10,2)");
            e.Property(f => f.Status).HasConversion<string>().HasMaxLength(20);
            e.HasOne(f => f.Member)
             .WithMany(m => m.MembershipFees)
             .HasForeignKey(f => f.MemberId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(f => new { f.MemberId, f.Year }).IsUnique();
        });

        modelBuilder.Entity<Competition>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.Name).IsRequired().HasMaxLength(200);
            e.Property(c => c.Location).IsRequired().HasMaxLength(300);
            e.Property(c => c.RoundType).IsRequired().HasMaxLength(50);
            e.Property(c => c.Type).HasConversion<string>().HasMaxLength(20);
            e.Property(c => c.Description).HasMaxLength(1000);
        });

        modelBuilder.Entity<ExternalParticipant>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.FirstName).IsRequired().HasMaxLength(100);
            e.Property(p => p.LastName).IsRequired().HasMaxLength(100);
            e.Property(p => p.Phone).HasMaxLength(30);
            e.Property(p => p.Email).HasMaxLength(200);
            e.Property(p => p.ClubAffiliation).HasMaxLength(200);
        });

        modelBuilder.Entity<CompetitionResult>(e =>
        {
            e.HasKey(r => r.Id);
            e.Property(r => r.BowClass).HasConversion<string>().HasMaxLength(20);
            e.Property(r => r.AgeClass).HasConversion<string>().HasMaxLength(20);
            e.Property(r => r.Gender).HasConversion<string>().HasMaxLength(20);
            e.Property(r => r.Notes).HasMaxLength(500);

            e.HasOne(r => r.Competition)
             .WithMany(c => c.Results)
             .HasForeignKey(r => r.CompetitionId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(r => r.Member)
             .WithMany(m => m.CompetitionResults)
             .HasForeignKey(r => r.MemberId)
             .OnDelete(DeleteBehavior.SetNull)
             .IsRequired(false);

            e.HasOne(r => r.ExternalParticipant)
             .WithMany(p => p.CompetitionResults)
             .HasForeignKey(r => r.ExternalParticipantId)
             .OnDelete(DeleteBehavior.SetNull)
             .IsRequired(false);

            e.ToTable(t => t.HasCheckConstraint(
                "CK_CompetitionResult_SingleParticipant",
                "(\"MemberId\" IS NOT NULL AND \"ExternalParticipantId\" IS NULL) OR " +
                "(\"MemberId\" IS NULL AND \"ExternalParticipantId\" IS NOT NULL)"));

            e.HasIndex(r => new { r.CompetitionId, r.MemberId }).IsUnique()
             .HasFilter("\"MemberId\" IS NOT NULL");
            e.HasIndex(r => new { r.CompetitionId, r.ExternalParticipantId }).IsUnique()
             .HasFilter("\"ExternalParticipantId\" IS NOT NULL");
        });
    }
}
