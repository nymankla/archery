using aspire.ApiService.Models;
using Microsoft.EntityFrameworkCore;

namespace aspire.ApiService.Data;

public class ArcheryDbContext(DbContextOptions<ArcheryDbContext> options) : DbContext(options)
{
    public DbSet<Member> Members => Set<Member>();
    public DbSet<MembershipFee> MembershipFees => Set<MembershipFee>();
    public DbSet<Competition> Competitions => Set<Competition>();
    public DbSet<ExternalParticipant> ExternalParticipants => Set<ExternalParticipant>();
    public DbSet<CompetitionResult> CompetitionResults => Set<CompetitionResult>();
    public DbSet<CompetitionParticipant> CompetitionParticipants => Set<CompetitionParticipant>();
    public DbSet<TrainingSession> TrainingSessions => Set<TrainingSession>();
    public DbSet<TrainingAttendance> TrainingAttendances => Set<TrainingAttendance>();

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
            e.Property(m => m.Personnummer).HasMaxLength(12);
            e.Property(m => m.PreferredBowClass).HasConversion<string>().HasMaxLength(20);
            e.HasIndex(m => m.Email);
            e.HasIndex(m => m.Personnummer).IsUnique().HasFilter("\"Personnummer\" IS NOT NULL");
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
            e.HasIndex(f => f.Year);
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
            e.HasIndex(c => c.Date);
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
            e.HasIndex(r => r.CompetitionId);

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

        modelBuilder.Entity<CompetitionParticipant>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.BowClass).HasConversion<string>().HasMaxLength(20);
            e.Property(p => p.AgeClass).HasConversion<string>().HasMaxLength(20);
            e.Property(p => p.Gender).HasConversion<string>().HasMaxLength(20);
            e.HasIndex(p => p.CompetitionId);

            e.HasOne(p => p.Competition)
             .WithMany()
             .HasForeignKey(p => p.CompetitionId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(p => p.Member)
             .WithMany()
             .HasForeignKey(p => p.MemberId)
             .OnDelete(DeleteBehavior.SetNull)
             .IsRequired(false);

            e.HasOne(p => p.ExternalParticipant)
             .WithMany()
             .HasForeignKey(p => p.ExternalParticipantId)
             .OnDelete(DeleteBehavior.SetNull)
             .IsRequired(false);

            e.ToTable(t => t.HasCheckConstraint(
                "CK_CompetitionParticipant_SingleParticipant",
                "(\"MemberId\" IS NOT NULL AND \"ExternalParticipantId\" IS NULL) OR " +
                "(\"MemberId\" IS NULL AND \"ExternalParticipantId\" IS NOT NULL)"));

            e.HasIndex(p => new { p.CompetitionId, p.MemberId }).IsUnique()
             .HasFilter("\"MemberId\" IS NOT NULL");
            e.HasIndex(p => new { p.CompetitionId, p.ExternalParticipantId }).IsUnique()
             .HasFilter("\"ExternalParticipantId\" IS NOT NULL");
        });

        modelBuilder.Entity<TrainingSession>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.Notes).HasMaxLength(1000);
            e.HasIndex(s => s.Date);
        });

        modelBuilder.Entity<TrainingAttendance>(e =>
        {
            e.HasKey(a => a.Id);
            e.HasIndex(a => a.TrainingSessionId);

            e.HasOne(a => a.TrainingSession)
             .WithMany(s => s.Attendances)
             .HasForeignKey(a => a.TrainingSessionId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(a => a.Member)
             .WithMany()
             .HasForeignKey(a => a.MemberId)
             .OnDelete(DeleteBehavior.SetNull)
             .IsRequired(false);

            e.HasOne(a => a.ExternalParticipant)
             .WithMany()
             .HasForeignKey(a => a.ExternalParticipantId)
             .OnDelete(DeleteBehavior.SetNull)
             .IsRequired(false);

            e.ToTable(t => t.HasCheckConstraint(
                "CK_TrainingAttendance_SingleParticipant",
                "(\"MemberId\" IS NOT NULL AND \"ExternalParticipantId\" IS NULL) OR " +
                "(\"MemberId\" IS NULL AND \"ExternalParticipantId\" IS NOT NULL)"));

            e.HasIndex(a => new { a.TrainingSessionId, a.MemberId }).IsUnique()
             .HasFilter("\"MemberId\" IS NOT NULL");
            e.HasIndex(a => new { a.TrainingSessionId, a.ExternalParticipantId }).IsUnique()
             .HasFilter("\"ExternalParticipantId\" IS NOT NULL");
        });
    }
}
