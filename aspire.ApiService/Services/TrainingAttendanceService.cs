using aspire.ApiService.Data;
using aspire.ApiService.Models;
using Microsoft.EntityFrameworkCore;

namespace aspire.ApiService.Services;

public class TrainingAttendanceService(ArcheryDbContext db) : ITrainingAttendanceService
{
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
            a.Member is not null ? $"{a.Member.FirstName} {a.Member.LastName}" : null,
            a.ExternalParticipantId,
            a.ExternalParticipant is not null ? $"{a.ExternalParticipant.FirstName} {a.ExternalParticipant.LastName}" : null
        )).ToList();

        return new TrainingSessionDetail(session.Id, session.Date, session.Notes, attendees);
    }

    public async Task<TrainingSessionDetail> SaveAttendanceAsync(
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

        foreach (var memberId in request.MemberIds)
        {
            newAttendances.Add(new TrainingAttendance
            {
                Id = Guid.NewGuid(),
                TrainingSessionId = session.Id,
                MemberId = memberId
            });
        }

        foreach (var extId in request.ExternalParticipantIds)
        {
            newAttendances.Add(new TrainingAttendance
            {
                Id = Guid.NewGuid(),
                TrainingSessionId = session.Id,
                ExternalParticipantId = extId
            });
        }

        db.TrainingAttendances.AddRange(newAttendances);

        await db.SaveChangesAsync(ct);

        return await GetByDateAsync(date, ct)
            ?? new TrainingSessionDetail(session.Id, session.Date, session.Notes, []);
    }
}
