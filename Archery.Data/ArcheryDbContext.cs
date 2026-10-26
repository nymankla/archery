using Archery.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Archery.Data;

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

            // Deleting a member deletes their results with them, for now. That rewrites
            // competition history (placements stay as recorded, but the archer disappears). A more
            // elegant option may come later, e.g. keeping the result and anonymising it, or only
            // allowing members to be deactivated (IsActive) once they have results.
            e.HasOne(r => r.Member)
             .WithMany(m => m.CompetitionResults)
             .HasForeignKey(r => r.MemberId)
             .OnDelete(DeleteBehavior.Cascade)
             .IsRequired(false);

            e.HasOne(r => r.ExternalParticipant)
             .WithMany(p => p.CompetitionResults)
             .HasForeignKey(r => r.ExternalParticipantId)
             .OnDelete(DeleteBehavior.Cascade)
             .IsRequired(false);

            e.ConfigureSingleParticipant("CK_CompetitionResult_SingleParticipant", nameof(CompetitionResult.CompetitionId));
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
             .OnDelete(DeleteBehavior.Cascade)
             .IsRequired(false);

            e.HasOne(p => p.ExternalParticipant)
             .WithMany()
             .HasForeignKey(p => p.ExternalParticipantId)
             .OnDelete(DeleteBehavior.Cascade)
             .IsRequired(false);

            e.ConfigureSingleParticipant("CK_CompetitionParticipant_SingleParticipant", nameof(CompetitionParticipant.CompetitionId));
        });

        modelBuilder.Entity<TrainingSession>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.Notes).HasMaxLength(1000);
            // One session per day: attendance is saved by date and looked up by date.
            e.HasIndex(s => s.Date).IsUnique();
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
             .OnDelete(DeleteBehavior.Cascade)
             .IsRequired(false);

            e.HasOne(a => a.ExternalParticipant)
             .WithMany()
             .HasForeignKey(a => a.ExternalParticipantId)
             .OnDelete(DeleteBehavior.Cascade)
             .IsRequired(false);

            e.ConfigureSingleParticipant("CK_TrainingAttendance_SingleParticipant", nameof(TrainingAttendance.TrainingSessionId));
        });
    }
}

static class SingleParticipantConfiguration
{
    const string Rule =
        "(\"MemberId\" IS NOT NULL AND \"ExternalParticipantId\" IS NULL) OR " +
        "(\"MemberId\" IS NULL AND \"ExternalParticipantId\" IS NOT NULL)";

    /// <summary>
    /// Enforces <see cref="IHasParticipant"/> in the database: exactly one of the two participant
    /// keys is set, and each participant appears at most once per <paramref name="parentKey"/>.
    /// </summary>
    public static void ConfigureSingleParticipant<T>(this EntityTypeBuilder<T> e, string checkName, string parentKey)
        where T : class, IHasParticipant
    {
        e.ToTable(t => t.HasCheckConstraint(checkName, Rule));

        e.HasIndex(parentKey, nameof(IHasParticipant.MemberId)).IsUnique()
         .HasFilter("\"MemberId\" IS NOT NULL");
        e.HasIndex(parentKey, nameof(IHasParticipant.ExternalParticipantId)).IsUnique()
         .HasFilter("\"ExternalParticipantId\" IS NOT NULL");
    }
}
