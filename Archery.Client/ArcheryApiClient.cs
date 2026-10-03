using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization.Metadata;

namespace Archery.Client;

public class ArcheryApiClient(
    HttpClient httpClient,
    IArcheryTokenProvider tokenProvider) : IArcheryApiClient
{
    public Task<ImportResult?> ImportMembersAsync(byte[] content, string fileName, CancellationToken ct = default)
        => ImportAsync("/members/import", content, fileName, ct);

    async Task<ImportResult?> ImportAsync(string url, byte[] content, string fileName, CancellationToken ct)
    {
        using var form = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(content);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
            fileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase)
                ? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                : "text/csv");
        form.Add(fileContent, "file", fileName);
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = form
        };
        await AddBearerTokenAsync(request, ct);
        var response = await httpClient.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync(ArcheryJsonContext.Default.ImportResult, ct);
    }

    public Task<DashboardData?> GetDashboardAsync(CancellationToken ct = default)
        => GetFromJsonAsync("/dashboard", ArcheryJsonContext.Default.DashboardData, ct);

    public Task<Member[]?> GetMembersAsync(CancellationToken ct = default)
        => GetFromJsonAsync("/members", ArcheryJsonContext.Default.MemberArray, ct);

    public Task<Member?> GetMemberAsync(Guid id, CancellationToken ct = default)
        => GetFromJsonAsync($"/members/{id}", ArcheryJsonContext.Default.Member, ct);

    public Task<HttpResponseMessage> CreateMemberAsync(Member member, CancellationToken ct = default)
        => SendAsJsonAsync(HttpMethod.Post, "/members", member, ArcheryJsonContext.Default.Member, ct);

    public Task<HttpResponseMessage> UpdateMemberAsync(Guid id, Member member, CancellationToken ct = default)
        => SendAsJsonAsync(HttpMethod.Put, $"/members/{id}", member, ArcheryJsonContext.Default.Member, ct);

    public Task<HttpResponseMessage> DeleteMemberAsync(Guid id, CancellationToken ct = default)
        => SendAsync(HttpMethod.Delete, $"/members/{id}", ct);

    public Task<ExportedFile?> ExportMembersAsync(string format, CancellationToken ct = default)
        => GetBytesAsync($"/members/export?format={format}", ct);

    public Task<MembershipFee[]?> GetFeesByMemberAsync(Guid memberId, CancellationToken ct = default)
        => GetFromJsonAsync($"/membership-fees/member/{memberId}", ArcheryJsonContext.Default.MembershipFeeArray, ct);

    public Task<MemberFeeOverview[]?> GetFeeOverviewAsync(int year, CancellationToken ct = default)
        => GetFromJsonAsync($"/membership-fees/overview?year={year}", ArcheryJsonContext.Default.MemberFeeOverviewArray, ct);

    public Task<HttpResponseMessage> BulkCreateFeesAsync(BulkFeeRequest req, CancellationToken ct = default)
        => SendAsJsonAsync(HttpMethod.Post, "/membership-fees/bulk", req, ArcheryJsonContext.Default.BulkFeeRequest, ct);

    public Task<HttpResponseMessage> CreateFeeAsync(MembershipFee fee, CancellationToken ct = default)
        => SendAsJsonAsync(HttpMethod.Post, "/membership-fees", fee, ArcheryJsonContext.Default.MembershipFee, ct);

    public Task<HttpResponseMessage> UpdateFeeAsync(Guid id, MembershipFee fee, CancellationToken ct = default)
        => SendAsJsonAsync(HttpMethod.Put, $"/membership-fees/{id}", fee, ArcheryJsonContext.Default.MembershipFee, ct);

    public Task<HttpResponseMessage> DeleteFeeAsync(Guid id, CancellationToken ct = default)
        => SendAsync(HttpMethod.Delete, $"/membership-fees/{id}", ct);

    public Task<Competition[]?> GetCompetitionsAsync(CancellationToken ct = default)
        => GetFromJsonAsync("/competitions", ArcheryJsonContext.Default.CompetitionArray, ct);

    public Task<Competition?> GetCompetitionAsync(Guid id, CancellationToken ct = default)
        => GetFromJsonAsync($"/competitions/{id}", ArcheryJsonContext.Default.Competition, ct);

    public Task<HttpResponseMessage> CreateCompetitionAsync(Competition competition, CancellationToken ct = default)
        => SendAsJsonAsync(HttpMethod.Post, "/competitions", competition, ArcheryJsonContext.Default.Competition, ct);

    public Task<HttpResponseMessage> UpdateCompetitionAsync(Guid id, Competition competition, CancellationToken ct = default)
        => SendAsJsonAsync(HttpMethod.Put, $"/competitions/{id}", competition, ArcheryJsonContext.Default.Competition, ct);

    public Task<HttpResponseMessage> DeleteCompetitionAsync(Guid id, CancellationToken ct = default)
        => SendAsync(HttpMethod.Delete, $"/competitions/{id}", ct);

    public Task<ImportResult?> ImportCompetitionsAsync(byte[] content, string fileName, CancellationToken ct = default)
        => ImportAsync("/competitions/import", content, fileName, ct);

    public Task<ImportResult?> ImportExternalParticipantsAsync(byte[] content, string fileName, CancellationToken ct = default)
        => ImportAsync("/external-participants/import", content, fileName, ct);

    public Task<ExternalParticipant[]?> GetExternalParticipantsAsync(CancellationToken ct = default)
        => GetFromJsonAsync("/external-participants", ArcheryJsonContext.Default.ExternalParticipantArray, ct);

    public Task<HttpResponseMessage> CreateExternalParticipantAsync(ExternalParticipant p, CancellationToken ct = default)
        => SendAsJsonAsync(HttpMethod.Post, "/external-participants", p, ArcheryJsonContext.Default.ExternalParticipant, ct);

    public Task<HttpResponseMessage> UpdateExternalParticipantAsync(Guid id, ExternalParticipant p, CancellationToken ct = default)
        => SendAsJsonAsync(HttpMethod.Put, $"/external-participants/{id}", p, ArcheryJsonContext.Default.ExternalParticipant, ct);

    public Task<HttpResponseMessage> DeleteExternalParticipantAsync(Guid id, CancellationToken ct = default)
        => SendAsync(HttpMethod.Delete, $"/external-participants/{id}", ct);

    public Task<CompetitionParticipant[]?> GetParticipantsByCompetitionAsync(Guid competitionId, CancellationToken ct = default)
        => GetFromJsonAsync($"/competition-participants/competition/{competitionId}", ArcheryJsonContext.Default.CompetitionParticipantArray, ct);

    public Task<HttpResponseMessage> RegisterParticipantAsync(CompetitionParticipant participant, CancellationToken ct = default)
        => SendAsJsonAsync(HttpMethod.Post, "/competition-participants", participant, ArcheryJsonContext.Default.CompetitionParticipant, ct);

    public Task<HttpResponseMessage> RemoveParticipantAsync(Guid id, CancellationToken ct = default)
        => SendAsync(HttpMethod.Delete, $"/competition-participants/{id}", ct);

    public Task<CompetitionResult[]?> GetResultsByCompetitionAsync(Guid competitionId, CancellationToken ct = default)
        => GetFromJsonAsync($"/competition-results/competition/{competitionId}", ArcheryJsonContext.Default.CompetitionResultArray, ct);

    public Task<HttpResponseMessage> CreateResultAsync(CompetitionResult result, CancellationToken ct = default)
        => SendAsJsonAsync(HttpMethod.Post, "/competition-results", result, ArcheryJsonContext.Default.CompetitionResult, ct);

    public Task<HttpResponseMessage> UpdateResultAsync(Guid id, CompetitionResult result, CancellationToken ct = default)
        => SendAsJsonAsync(HttpMethod.Put, $"/competition-results/{id}", result, ArcheryJsonContext.Default.CompetitionResult, ct);

    public Task<HttpResponseMessage> DeleteResultAsync(Guid id, CancellationToken ct = default)
        => SendAsync(HttpMethod.Delete, $"/competition-results/{id}", ct);

    public Task<DateOnly[]?> GetTrainingDatesAsync(CancellationToken ct = default)
        => GetFromJsonAsync("/training-attendance/dates", ArcheryJsonContext.Default.DateOnlyArray, ct);

    public Task<TrainingSessionDetail?> GetTrainingAttendanceByDateAsync(DateOnly date, CancellationToken ct = default)
        => GetFromJsonAsync($"/training-attendance/by-date?date={date:yyyy-MM-dd}", ArcheryJsonContext.Default.TrainingSessionDetail, ct);

    public Task<HttpResponseMessage> SaveTrainingAttendanceAsync(DateOnly date, SaveTrainingAttendanceRequest request, CancellationToken ct = default)
        => SendAsJsonAsync(HttpMethod.Put, $"/training-attendance/by-date?date={date:yyyy-MM-dd}", request, ArcheryJsonContext.Default.SaveTrainingAttendanceRequest, ct);

    public Task<ExportedFile?> ExportTrainingAttendanceAsync(DateOnly date, string format, CancellationToken ct = default)
        => GetBytesAsync($"/training-attendance/by-date/export?date={date:yyyy-MM-dd}&format={format}", ct);

    async ValueTask AddBearerTokenAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var token = await tokenProvider.GetAccessTokenAsync(ct);

        if (!string.IsNullOrWhiteSpace(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    async Task<T?> GetFromJsonAsync<T>(string url, JsonTypeInfo<T> typeInfo, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        await AddBearerTokenAsync(request, ct);
        using var response = await httpClient.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
            throw new ArcheryApiException(await ArcheryApiErrors.ReadAsync(response, ct));
        return await response.Content.ReadFromJsonAsync(typeInfo, ct);
    }

    async Task<ExportedFile?> GetBytesAsync(string url, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        await AddBearerTokenAsync(request, ct);
        using var response = await httpClient.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode) return null;

        var content = await response.Content.ReadAsByteArrayAsync(ct);
        var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
        var fileName = response.Content.Headers.ContentDisposition?.FileNameStar
            ?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"')
            ?? "export";
        return new ExportedFile(fileName, contentType, content);
    }

    async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, CancellationToken ct)
    {
        var request = new HttpRequestMessage(method, url);
        await AddBearerTokenAsync(request, ct);
        return await httpClient.SendAsync(request, ct);
    }

    async Task<HttpResponseMessage> SendAsJsonAsync<T>(
        HttpMethod method, string url, T value, JsonTypeInfo<T> typeInfo, CancellationToken ct)
    {
        var request = new HttpRequestMessage(method, url)
        {
            Content = JsonContent.Create(value, typeInfo)
        };
        await AddBearerTokenAsync(request, ct);
        return await httpClient.SendAsync(request, ct);
    }
}
