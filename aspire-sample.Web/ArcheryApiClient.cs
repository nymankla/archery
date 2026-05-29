using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace aspire_sample.Web;

public class ArcheryApiClient(HttpClient httpClient, AccessTokenProvider tokenProvider)
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
        AddBearerToken(request);
        var response = await httpClient.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<ImportResult>(ct);
    }

    public Task<Member[]?> GetMembersAsync(CancellationToken ct = default)
        => GetFromJsonAsync<Member[]>("/members", ct);

    public Task<Member?> GetMemberAsync(Guid id, CancellationToken ct = default)
        => GetFromJsonAsync<Member>($"/members/{id}", ct);

    public Task<HttpResponseMessage> CreateMemberAsync(Member member, CancellationToken ct = default)
        => SendAsJsonAsync(HttpMethod.Post, "/members", member, ct);

    public Task<HttpResponseMessage> UpdateMemberAsync(Guid id, Member member, CancellationToken ct = default)
        => SendAsJsonAsync(HttpMethod.Put, $"/members/{id}", member, ct);

    public Task<HttpResponseMessage> DeleteMemberAsync(Guid id, CancellationToken ct = default)
        => SendAsync(HttpMethod.Delete, $"/members/{id}", ct);

    public Task<MembershipFee[]?> GetFeesByMemberAsync(Guid memberId, CancellationToken ct = default)
        => GetFromJsonAsync<MembershipFee[]>($"/membership-fees/member/{memberId}", ct);

    public Task<HttpResponseMessage> CreateFeeAsync(MembershipFee fee, CancellationToken ct = default)
        => SendAsJsonAsync(HttpMethod.Post, "/membership-fees", fee, ct);

    public Task<HttpResponseMessage> UpdateFeeAsync(Guid id, MembershipFee fee, CancellationToken ct = default)
        => SendAsJsonAsync(HttpMethod.Put, $"/membership-fees/{id}", fee, ct);

    public Task<HttpResponseMessage> DeleteFeeAsync(Guid id, CancellationToken ct = default)
        => SendAsync(HttpMethod.Delete, $"/membership-fees/{id}", ct);

    public Task<Competition[]?> GetCompetitionsAsync(CancellationToken ct = default)
        => GetFromJsonAsync<Competition[]>("/competitions", ct);

    public Task<Competition?> GetCompetitionAsync(Guid id, CancellationToken ct = default)
        => GetFromJsonAsync<Competition>($"/competitions/{id}", ct);

    public Task<HttpResponseMessage> CreateCompetitionAsync(Competition competition, CancellationToken ct = default)
        => SendAsJsonAsync(HttpMethod.Post, "/competitions", competition, ct);

    public Task<HttpResponseMessage> UpdateCompetitionAsync(Guid id, Competition competition, CancellationToken ct = default)
        => SendAsJsonAsync(HttpMethod.Put, $"/competitions/{id}", competition, ct);

    public Task<HttpResponseMessage> DeleteCompetitionAsync(Guid id, CancellationToken ct = default)
        => SendAsync(HttpMethod.Delete, $"/competitions/{id}", ct);

    public Task<ImportResult?> ImportExternalParticipantsAsync(byte[] content, string fileName, CancellationToken ct = default)
        => ImportAsync("/external-participants/import", content, fileName, ct);

    public Task<ExternalParticipant[]?> GetExternalParticipantsAsync(CancellationToken ct = default)
        => GetFromJsonAsync<ExternalParticipant[]>("/external-participants", ct);

    public Task<HttpResponseMessage> CreateExternalParticipantAsync(ExternalParticipant p, CancellationToken ct = default)
        => SendAsJsonAsync(HttpMethod.Post, "/external-participants", p, ct);

    public Task<HttpResponseMessage> UpdateExternalParticipantAsync(Guid id, ExternalParticipant p, CancellationToken ct = default)
        => SendAsJsonAsync(HttpMethod.Put, $"/external-participants/{id}", p, ct);

    public Task<HttpResponseMessage> DeleteExternalParticipantAsync(Guid id, CancellationToken ct = default)
        => SendAsync(HttpMethod.Delete, $"/external-participants/{id}", ct);

    public Task<CompetitionParticipant[]?> GetParticipantsByCompetitionAsync(Guid competitionId, CancellationToken ct = default)
        => GetFromJsonAsync<CompetitionParticipant[]>($"/competition-participants/competition/{competitionId}", ct);

    public Task<HttpResponseMessage> RegisterParticipantAsync(CompetitionParticipant participant, CancellationToken ct = default)
        => SendAsJsonAsync(HttpMethod.Post, "/competition-participants", participant, ct);

    public Task<HttpResponseMessage> RemoveParticipantAsync(Guid id, CancellationToken ct = default)
        => SendAsync(HttpMethod.Delete, $"/competition-participants/{id}", ct);

    public Task<CompetitionResult[]?> GetResultsByCompetitionAsync(Guid competitionId, CancellationToken ct = default)
        => GetFromJsonAsync<CompetitionResult[]>($"/competition-results/competition/{competitionId}", ct);

    public Task<HttpResponseMessage> CreateResultAsync(CompetitionResult result, CancellationToken ct = default)
        => SendAsJsonAsync(HttpMethod.Post, "/competition-results", result, ct);

    public Task<HttpResponseMessage> UpdateResultAsync(Guid id, CompetitionResult result, CancellationToken ct = default)
        => SendAsJsonAsync(HttpMethod.Put, $"/competition-results/{id}", result, ct);

    public Task<HttpResponseMessage> DeleteResultAsync(Guid id, CancellationToken ct = default)
        => SendAsync(HttpMethod.Delete, $"/competition-results/{id}", ct);

    void AddBearerToken(HttpRequestMessage request)
    {
        if (!string.IsNullOrWhiteSpace(tokenProvider.AccessToken))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenProvider.AccessToken);
    }

    async Task<T?> GetFromJsonAsync<T>(string url, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        AddBearerToken(request);
        using var response = await httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(ct);
    }

    Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, CancellationToken ct)
    {
        var request = new HttpRequestMessage(method, url);
        AddBearerToken(request);
        return httpClient.SendAsync(request, ct);
    }

    Task<HttpResponseMessage> SendAsJsonAsync<T>(HttpMethod method, string url, T value, CancellationToken ct)
    {
        var request = new HttpRequestMessage(method, url)
        {
            Content = JsonContent.Create(value)
        };
        AddBearerToken(request);
        return httpClient.SendAsync(request, ct);
    }
}
