using aspire.ApiService.Data;
using aspire.ApiService.Infrastructure;
using aspire.ApiService.Models;
using Microsoft.EntityFrameworkCore;

namespace aspire.ApiService.Services;

public class TrainingAttendanceService(ArcheryDbContext db) : ITrainingAttendanceService
{
    const string DuplicateAttendanceError = "One or more selected participants are already registered for this training session.";


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
            .FirstOrDefaultAsync(s => s.Date == date, ct);

        if (session is null)
        {
            session = new TrainingSession
            {
                Id = Guid.NewGuid(),
                Date = date,
                Notes = request.Notes
            };
            db.TrainingSessions.Add(session);
        }
        else
        {
            session.Notes = request.Notes;

            var existingAttendances = await db.TrainingAttendances
                .Where(a => a.TrainingSessionId == session.Id)
                .ToListAsync(ct);

            if (existingAttendances.Count > 0)
            {
                db.TrainingAttendances.RemoveRange(existingAttendances);
                await db.SaveChangesAsync(ct);
            }
        }

        var newAttendances = new List<TrainingAttendance>();

        foreach (var memberId in request.MemberIds.Distinct())
        {
            newAttendances.Add(new TrainingAttendance
            {
                Id = Guid.NewGuid(),
                TrainingSessionId = session.Id,
                MemberId = memberId
            });
        }

        foreach (var extId in request.ExternalParticipantIds.Distinct())
        {
            newAttendances.Add(new TrainingAttendance
            {
                Id = Guid.NewGuid(),
                TrainingSessionId = session.Id,
                ExternalParticipantId = extId
            });
        }

        db.TrainingAttendances.AddRange(newAttendances);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueConstraintViolation())
        {
            return Result<TrainingSessionDetail>.Failure(DuplicateAttendanceError);
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
