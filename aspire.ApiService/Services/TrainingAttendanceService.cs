using aspire.ApiService.Data;
using aspire.ApiService.Infrastructure;
using aspire.ApiService.Models;
using Microsoft.EntityFrameworkCore;

namespace aspire.ApiService.Services;

public class TrainingAttendanceService(ArcheryDbContext db) : ITrainingAttendanceService
{
    // Raised when another request saved the same date at the same time (the unique index on
    // TrainingSession.Date or on an attendance row fired). Reloading shows the winning save.
    const string ConcurrentSaveError = "Attendance for this date was changed by someone else. Reload and try again.";


    public async Task<IReadOnlyList<DateOnly>> GetTrainingDatesAsync(CancellationToken ct = default)
        => await db.TrainingSessions
            .AsNoTracking()
            .OrderByDescending(s => s.Date)
            .Select(s => s.Date)
            .ToListAsync(ct);

    public async Task<TrainingSessionDetail?> GetByDateAsync(DateOnly date, CancellationToken ct = default)
    {
        var session = await db.TrainingSessions
            .AsNoTracking()
            .Include(s => s.Attendances)
                .ThenInclude(a => a.Member)
            .Include(s => s.Attendances)
                .ThenInclude(a => a.ExternalParticipant)
            .FirstOrDefaultAsync(s => s.Date == date, ct);

        if (session is null)
            return null;

        var attendees = session.Attendances.Select(a => new AttendeeInfo(
            a.Id,
            a.MemberId,
            a.Member?.FirstName,
            a.Member?.LastName,
            a.Member?.Personnummer,
            a.ExternalParticipantId,
            a.ExternalParticipant?.FirstName,
            a.ExternalParticipant?.LastName
        )).ToList();

        return new TrainingSessionDetail(session.Id, session.Date, session.Notes, attendees);
    }

    public async Task<Result<TrainingSessionDetail>> SaveAttendanceAsync(
        DateOnly date,
        SaveAttendanceRequest request,
        CancellationToken ct = default)
    {
        var session = await db.TrainingSessions
            .Include(s => s.Attendances)
            .FirstOrDefaultAsync(s => s.Date == date, ct);

        if (session is null)
        {
            session = new TrainingSession
            {
                Id = Guid.NewGuid(),
                Date = date
            };
            db.TrainingSessions.Add(session);
        }

        session.Notes = request.Notes;

        // Apply the difference rather than delete-all-then-insert, so the whole save is one
        // SaveChanges (atomic) and an attendee who stays never has their row deleted and re-added.
        var memberIds = request.MemberIds.ToHashSet();
        var externalIds = request.ExternalParticipantIds.ToHashSet();

        foreach (var attendance in session.Attendances.ToList())
        {
            var stays = attendance.MemberId is { } memberId
                ? memberIds.Remove(memberId)
                : externalIds.Remove(attendance.ExternalParticipantId!.Value);

            if (!stays)
                db.TrainingAttendances.Remove(attendance);
        }

        // Whatever is left in the two sets was not there before.
        db.TrainingAttendances.AddRange(memberIds.Select(id => new TrainingAttendance
        {
            Id = Guid.NewGuid(),
            TrainingSessionId = session.Id,
            MemberId = id
        }));
        db.TrainingAttendances.AddRange(externalIds.Select(id => new TrainingAttendance
        {
            Id = Guid.NewGuid(),
            TrainingSessionId = session.Id,
            ExternalParticipantId = id
        }));

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueConstraintViolation())
        {
            return Result<TrainingSessionDetail>.Failure(ConcurrentSaveError);
        }

        var detail = await GetByDateAsync(date, ct)
            ?? new TrainingSessionDetail(session.Id, session.Date, session.Notes, []);
        return Result<TrainingSessionDetail>.Success(detail);
    }

    public async Task<ExportFile?> ExportAsync(DateOnly date, ExportFormat format, CancellationToken ct = default)
    {
        var detail = await GetByDateAsync(date, ct);
        if (detail is null) return null;

        string[] headers = ["First Name", "Last Name", "Personnummer", "Category"];
        var rows = detail.Attendees
            .OrderBy(a => a.MemberLastName ?? a.ExternalParticipantLastName)
            .ThenBy(a => a.MemberFirstName ?? a.ExternalParticipantFirstName)
            .Select(a => a.MemberId.HasValue
                ? (IReadOnlyList<string?>)[a.MemberFirstName, a.MemberLastName, a.MemberPersonnummer, "Member"]
                : [a.ExternalParticipantFirstName, a.ExternalParticipantLastName, null, "Guest"]);

        return SpreadsheetWriter.Write(format, $"training-attendance-{date:yyyy-MM-dd}", "Attendance", headers, rows);
    }
}
