using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Archery.Mobile.Core.Tests;

/// <summary>
/// Hand-rolled test doubles. The repo uses no mocking library, so these keep the dependency
/// surface unchanged.
/// </summary>
public sealed class FakeApiClient : IArcheryApiClient
{
    public List<Member> Members { get; set; } = [];

    public List<MembershipFee> Fees { get; set; } = [];

    public List<ExternalParticipant> Externals { get; set; } = [];

    public List<DateOnly> TrainingDates { get; set; } = [];

    /// <summary>What GET /training-attendance/by-date returns; null means the call returned nothing.</summary>
    public TrainingSessionDetail? Detail { get; set; }

    public SaveTrainingAttendanceRequest? LastSaved { get; private set; }

    public DateOnly? LastSavedDate { get; private set; }

    public List<string> Calls { get; } = [];

    /// <summary>When set, every call throws this instead of returning.</summary>
    public Exception? Throws { get; set; }

    /// <summary>Non-success status for the mutating calls, which return a response rather than throw.</summary>
    public HttpStatusCode MutationStatus { get; set; } = HttpStatusCode.NoContent;

    public string? MutationBody { get; set; }

    public Member? LastCreated { get; private set; }

    public Member? LastUpdated { get; private set; }

    public MembershipFee? LastFeeUpdate { get; private set; }

    T Record<T>(string call, T result)
    {
        Calls.Add(call);
        if (Throws is not null)
            throw Throws;
        return result;
    }

    Task<HttpResponseMessage> Mutation(string call)
    {
        Calls.Add(call);
        if (Throws is not null)
            throw Throws;

        var response = new HttpResponseMessage(MutationStatus);
        if (MutationBody is not null)
            response.Content = new StringContent(MutationBody, System.Text.Encoding.UTF8, "application/json");
        return Task.FromResult(response);
    }

    public Task<Member[]?> GetMembersAsync(CancellationToken ct = default) =>
        Task.FromResult<Member[]?>(Record("GetMembers", Members.ToArray()));

    public Task<Member?> GetMemberAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(Record($"GetMember:{id}", Members.FirstOrDefault(m => m.Id == id)));

    public Task<HttpResponseMessage> CreateMemberAsync(Member member, CancellationToken ct = default)
    {
        LastCreated = member;
        return Mutation("CreateMember");
    }

    public Task<HttpResponseMessage> UpdateMemberAsync(Guid id, Member member, CancellationToken ct = default)
    {
        LastUpdated = member;
        return Mutation($"UpdateMember:{id}");
    }

    public Task<HttpResponseMessage> DeleteMemberAsync(Guid id, CancellationToken ct = default) =>
        Mutation($"DeleteMember:{id}");

    public Task<MembershipFee[]?> GetFeesByMemberAsync(Guid memberId, CancellationToken ct = default) =>
        Task.FromResult<MembershipFee[]?>(Record($"GetFees:{memberId}",
            Fees.Where(f => f.MemberId == memberId).ToArray()));

    public Task<HttpResponseMessage> UpdateFeeAsync(Guid id, MembershipFee fee, CancellationToken ct = default)
    {
        LastFeeUpdate = fee;
        return Mutation($"UpdateFee:{id}");
    }

    // --- not exercised by these tests ---
    public Task<ImportResult?> ImportMembersAsync(byte[] content, string fileName, CancellationToken ct = default) => throw new NotSupportedException();
    public Task<DashboardData?> GetDashboardAsync(CancellationToken ct = default) => throw new NotSupportedException();
    public Task<ExportedFile?> ExportMembersAsync(string format, CancellationToken ct = default) => throw new NotSupportedException();
    public Task<MemberFeeOverview[]?> GetFeeOverviewAsync(int year, CancellationToken ct = default) => throw new NotSupportedException();
    public Task<HttpResponseMessage> BulkCreateFeesAsync(BulkFeeRequest req, CancellationToken ct = default) => throw new NotSupportedException();
    public Task<HttpResponseMessage> CreateFeeAsync(MembershipFee fee, CancellationToken ct = default) => throw new NotSupportedException();
    public Task<HttpResponseMessage> DeleteFeeAsync(Guid id, CancellationToken ct = default) => throw new NotSupportedException();
    public Task<Competition[]?> GetCompetitionsAsync(CancellationToken ct = default) => throw new NotSupportedException();
    public Task<Competition?> GetCompetitionAsync(Guid id, CancellationToken ct = default) => throw new NotSupportedException();
    public Task<HttpResponseMessage> CreateCompetitionAsync(Competition competition, CancellationToken ct = default) => throw new NotSupportedException();
    public Task<HttpResponseMessage> UpdateCompetitionAsync(Guid id, Competition competition, CancellationToken ct = default) => throw new NotSupportedException();
    public Task<HttpResponseMessage> DeleteCompetitionAsync(Guid id, CancellationToken ct = default) => throw new NotSupportedException();
    public Task<ImportResult?> ImportCompetitionsAsync(byte[] content, string fileName, CancellationToken ct = default) => throw new NotSupportedException();
    public Task<ImportResult?> ImportExternalParticipantsAsync(byte[] content, string fileName, CancellationToken ct = default) => throw new NotSupportedException();
    public Task<ExternalParticipant[]?> GetExternalParticipantsAsync(CancellationToken ct = default) =>
        Task.FromResult<ExternalParticipant[]?>(Record("GetExternalParticipants", Externals.ToArray()));
    public Task<HttpResponseMessage> CreateExternalParticipantAsync(ExternalParticipant p, CancellationToken ct = default) => throw new NotSupportedException();
    public Task<HttpResponseMessage> UpdateExternalParticipantAsync(Guid id, ExternalParticipant p, CancellationToken ct = default) => throw new NotSupportedException();
    public Task<HttpResponseMessage> DeleteExternalParticipantAsync(Guid id, CancellationToken ct = default) => throw new NotSupportedException();
    public Task<CompetitionParticipant[]?> GetParticipantsByCompetitionAsync(Guid competitionId, CancellationToken ct = default) => throw new NotSupportedException();
    public Task<HttpResponseMessage> RegisterParticipantAsync(CompetitionParticipant participant, CancellationToken ct = default) => throw new NotSupportedException();
    public Task<HttpResponseMessage> RemoveParticipantAsync(Guid id, CancellationToken ct = default) => throw new NotSupportedException();
    public Task<CompetitionResult[]?> GetResultsByCompetitionAsync(Guid competitionId, CancellationToken ct = default) => throw new NotSupportedException();
    public Task<HttpResponseMessage> CreateResultAsync(CompetitionResult result, CancellationToken ct = default) => throw new NotSupportedException();
    public Task<HttpResponseMessage> UpdateResultAsync(Guid id, CompetitionResult result, CancellationToken ct = default) => throw new NotSupportedException();
    public Task<HttpResponseMessage> DeleteResultAsync(Guid id, CancellationToken ct = default) => throw new NotSupportedException();
    public Task<DateOnly[]?> GetTrainingDatesAsync(CancellationToken ct = default) =>
        Task.FromResult<DateOnly[]?>(Record("GetTrainingDates", TrainingDates.ToArray()));
    public Task<TrainingSessionDetail?> GetTrainingAttendanceByDateAsync(DateOnly date, CancellationToken ct = default) =>
        Task.FromResult(Record($"GetTrainingAttendance:{date:yyyy-MM-dd}", Detail));
    public Task<HttpResponseMessage> SaveTrainingAttendanceAsync(DateOnly date, SaveTrainingAttendanceRequest request, CancellationToken ct = default)
    {
        LastSaved = request;
        LastSavedDate = date;
        return Mutation($"SaveTrainingAttendance:{date:yyyy-MM-dd}");
    }
    public Task<ExportedFile?> ExportTrainingAttendanceAsync(DateOnly date, string format, CancellationToken ct = default) => throw new NotSupportedException();
}

public sealed class FakeNavigationService : INavigationService
{
    public List<string> Routes { get; } = [];

    public IDictionary<string, object>? LastParameters { get; private set; }

    public int BackCount { get; private set; }

    public Task GoToAsync(string route, IDictionary<string, object>? parameters = null)
    {
        Routes.Add(route);
        LastParameters = parameters;
        return Task.CompletedTask;
    }

    public Task GoBackAsync()
    {
        BackCount++;
        return Task.CompletedTask;
    }

    public Task GoToRootAsync(string route)
    {
        Routes.Add(route);
        return Task.CompletedTask;
    }
}

public sealed class FakeDialogService(bool confirmResult = true) : IDialogService
{
    public List<string> Confirmations { get; } = [];

    public bool ConfirmResult { get; set; } = confirmResult;

    public Task AlertAsync(string title, string message, string cancel = "OK") => Task.CompletedTask;

    public Task<bool> ConfirmAsync(string title, string message, string accept = "Yes", string cancel = "No")
    {
        Confirmations.Add(title);
        return Task.FromResult(ConfirmResult);
    }
}

public sealed class FakeAuthService : IAuthService
{
    public bool IsSignedIn { get; set; } = true;

    public string? DisplayName { get; set; } = "Admin User";

    public int SignOutCount { get; private set; }

    public bool RestoreResult { get; set; }

    public bool SignInResult { get; set; } = true;

    public Exception? SignInThrows { get; set; }

    public event EventHandler? SessionEnded;

    public Task<bool> SignInAsync(CancellationToken ct = default) =>
        SignInThrows is not null ? Task.FromException<bool>(SignInThrows) : Task.FromResult(SignInResult);

    public Task SignOutAsync(CancellationToken ct = default)
    {
        SignOutCount++;
        IsSignedIn = false;
        return Task.CompletedTask;
    }

    public Task<bool> TryRestoreSessionAsync(CancellationToken ct = default) => Task.FromResult(RestoreResult);

    public void RaiseSessionEnded() => SessionEnded?.Invoke(this, EventArgs.Empty);
}

public static class TestLogger
{
    public static ILogger<T> For<T>() => NullLogger<T>.Instance;
}
