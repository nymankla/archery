namespace aspire_sample.Web;

public class ArcheryApiClient(HttpClient httpClient)
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
        var response = await httpClient.PostAsync(url, form, ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<ImportResult>(ct);
    }

    public Task<Member[]?> GetMembersAsync(CancellationToken ct = default)
        => httpClient.GetFromJsonAsync<Member[]>("/members", ct);

    public Task<Member?> GetMemberAsync(Guid id, CancellationToken ct = default)
        => httpClient.GetFromJsonAsync<Member>($"/members/{id}", ct);

    public Task<HttpResponseMessage> CreateMemberAsync(Member member, CancellationToken ct = default)
        => httpClient.PostAsJsonAsync("/members", member, ct);

    public Task<HttpResponseMessage> UpdateMemberAsync(Guid id, Member member, CancellationToken ct = default)
        => httpClient.PutAsJsonAsync($"/members/{id}", member, ct);

    public Task<HttpResponseMessage> DeleteMemberAsync(Guid id, CancellationToken ct = default)
        => httpClient.DeleteAsync($"/members/{id}", ct);

    public Task<MembershipFee[]?> GetFeesByMemberAsync(Guid memberId, CancellationToken ct = default)
        => httpClient.GetFromJsonAsync<MembershipFee[]>($"/membership-fees/member/{memberId}", ct);

    public Task<HttpResponseMessage> CreateFeeAsync(MembershipFee fee, CancellationToken ct = default)
        => httpClient.PostAsJsonAsync("/membership-fees", fee, ct);

    public Task<HttpResponseMessage> UpdateFeeAsync(Guid id, MembershipFee fee, CancellationToken ct = default)
        => httpClient.PutAsJsonAsync($"/membership-fees/{id}", fee, ct);

    public Task<HttpResponseMessage> DeleteFeeAsync(Guid id, CancellationToken ct = default)
        => httpClient.DeleteAsync($"/membership-fees/{id}", ct);

    public Task<Competition[]?> GetCompetitionsAsync(CancellationToken ct = default)
        => httpClient.GetFromJsonAsync<Competition[]>("/competitions", ct);

    public Task<Competition?> GetCompetitionAsync(Guid id, CancellationToken ct = default)
        => httpClient.GetFromJsonAsync<Competition>($"/competitions/{id}", ct);

    public Task<HttpResponseMessage> CreateCompetitionAsync(Competition competition, CancellationToken ct = default)
        => httpClient.PostAsJsonAsync("/competitions", competition, ct);

    public Task<HttpResponseMessage> UpdateCompetitionAsync(Guid id, Competition competition, CancellationToken ct = default)
        => httpClient.PutAsJsonAsync($"/competitions/{id}", competition, ct);

    public Task<HttpResponseMessage> DeleteCompetitionAsync(Guid id, CancellationToken ct = default)
        => httpClient.DeleteAsync($"/competitions/{id}", ct);

    public Task<ImportResult?> ImportExternalParticipantsAsync(byte[] content, string fileName, CancellationToken ct = default)
        => ImportAsync("/external-participants/import", content, fileName, ct);

    public Task<ExternalParticipant[]?> GetExternalParticipantsAsync(CancellationToken ct = default)
        => httpClient.GetFromJsonAsync<ExternalParticipant[]>("/external-participants", ct);

    public Task<HttpResponseMessage> CreateExternalParticipantAsync(ExternalParticipant p, CancellationToken ct = default)
        => httpClient.PostAsJsonAsync("/external-participants", p, ct);

    public Task<HttpResponseMessage> UpdateExternalParticipantAsync(Guid id, ExternalParticipant p, CancellationToken ct = default)
        => httpClient.PutAsJsonAsync($"/external-participants/{id}", p, ct);

    public Task<HttpResponseMessage> DeleteExternalParticipantAsync(Guid id, CancellationToken ct = default)
        => httpClient.DeleteAsync($"/external-participants/{id}", ct);

    public Task<CompetitionParticipant[]?> GetParticipantsByCompetitionAsync(Guid competitionId, CancellationToken ct = default)
        => httpClient.GetFromJsonAsync<CompetitionParticipant[]>($"/competition-participants/competition/{competitionId}", ct);

    public Task<HttpResponseMessage> RegisterParticipantAsync(CompetitionParticipant participant, CancellationToken ct = default)
        => httpClient.PostAsJsonAsync("/competition-participants", participant, ct);

    public Task<HttpResponseMessage> RemoveParticipantAsync(Guid id, CancellationToken ct = default)
        => httpClient.DeleteAsync($"/competition-participants/{id}", ct);

    public Task<CompetitionResult[]?> GetResultsByCompetitionAsync(Guid competitionId, CancellationToken ct = default)
        => httpClient.GetFromJsonAsync<CompetitionResult[]>($"/competition-results/competition/{competitionId}", ct);

    public Task<HttpResponseMessage> CreateResultAsync(CompetitionResult result, CancellationToken ct = default)
        => httpClient.PostAsJsonAsync("/competition-results", result, ct);

    public Task<HttpResponseMessage> UpdateResultAsync(Guid id, CompetitionResult result, CancellationToken ct = default)
        => httpClient.PutAsJsonAsync($"/competition-results/{id}", result, ct);

    public Task<HttpResponseMessage> DeleteResultAsync(Guid id, CancellationToken ct = default)
        => httpClient.DeleteAsync($"/competition-results/{id}", ct);
}
