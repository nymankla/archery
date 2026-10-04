using System.Globalization;
using System.Net;
using System.Text.Json;

namespace Archery.Client.Tests;

public class ArcheryApiClientTests
{
    static (ArcheryApiClient Client, StubHttpMessageHandler Handler) Build(
        StubHttpMessageHandler handler, string? token = "tok-123")
    {
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://apiservice") };
        return (new ArcheryApiClient(http, new StubTokenProvider(token)), handler);
    }

    // ---------- routes and verbs ----------

    [Fact]
    public async Task GetMembers_CallsExpectedRoute()
    {
        var (client, handler) = Build(StubHttpMessageHandler.Json("[]"));
        await client.GetMembersAsync();

        Assert.Equal(HttpMethod.Get, handler.LastRequest.Method);
        Assert.Equal("/members", handler.LastRequest.RequestUri!.PathAndQuery);
    }

    [Fact]
    public async Task GetMember_PutsIdInRoute()
    {
        var id = Guid.Parse("11111111-2222-3333-4444-555555555555");
        var (client, handler) = Build(StubHttpMessageHandler.Json("{}"));
        await client.GetMemberAsync(id);

        Assert.Equal($"/members/{id}", handler.LastRequest.RequestUri!.PathAndQuery);
    }

    [Fact]
    public async Task DeleteMember_UsesDeleteVerb()
    {
        var id = Guid.NewGuid();
        var (client, handler) = Build(StubHttpMessageHandler.Status(HttpStatusCode.NoContent));
        await client.DeleteMemberAsync(id);

        Assert.Equal(HttpMethod.Delete, handler.LastRequest.Method);
        Assert.Equal($"/members/{id}", handler.LastRequest.RequestUri!.PathAndQuery);
    }

    [Fact]
    public async Task UpdateMember_UsesPutVerb()
    {
        var id = Guid.NewGuid();
        var (client, handler) = Build(StubHttpMessageHandler.Status(HttpStatusCode.NoContent));
        await client.UpdateMemberAsync(id, new Member());

        Assert.Equal(HttpMethod.Put, handler.LastRequest.Method);
        Assert.Equal($"/members/{id}", handler.LastRequest.RequestUri!.PathAndQuery);
    }

    [Fact]
    public async Task GetFeeOverview_PassesYearAsQuery()
    {
        var (client, handler) = Build(StubHttpMessageHandler.Json("[]"));
        await client.GetFeeOverviewAsync(2026);

        Assert.Equal("/membership-fees/overview?year=2026", handler.LastRequest.RequestUri!.PathAndQuery);
    }

    [Fact]
    public async Task GetParticipantsByCompetition_NestsCompetitionId()
    {
        var id = Guid.NewGuid();
        var (client, handler) = Build(StubHttpMessageHandler.Json("[]"));
        await client.GetParticipantsByCompetitionAsync(id);

        Assert.Equal($"/competition-participants/competition/{id}", handler.LastRequest.RequestUri!.PathAndQuery);
    }

    // ---------- DateOnly formatting ----------

    // The app forces sv-SE everywhere. "yyyy-MM-dd" treats the dash as a literal rather than
    // the culture's date separator, so this is already correct - but it is pinned here because
    // a culture-sensitive format would silently produce an unparseable query string.
    [Theory]
    [InlineData("sv-SE")]
    [InlineData("en-US")]
    [InlineData("de-DE")]
    public async Task TrainingAttendanceByDate_FormatsDateIsoRegardlessOfCulture(string cultureName)
    {
        var previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo(cultureName);
        try
        {
            var (client, handler) = Build(StubHttpMessageHandler.Json(
                NoSessionJson));
            await client.GetTrainingAttendanceByDateAsync(new DateOnly(2026, 3, 9));

            Assert.Equal("/training-attendance/by-date?date=2026-03-09",
                handler.LastRequest.RequestUri!.PathAndQuery);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    // ---------- the asymmetric no-session response ----------

    const string NoSessionJson =
        """{"sessionId":null,"date":"2026-03-09","notes":null,"attendees":[]}""";

    // GET /training-attendance/by-date returns a different shape when no session exists for
    // that date: sessionId is null and attendees is empty. The server's own record type
    // declares a non-nullable Guid, which makes this the likeliest source of a client-side
    // NullReferenceException.
    [Fact]
    public async Task TrainingAttendanceByDate_HandlesNoSessionShape()
    {
        var (client, _) = Build(StubHttpMessageHandler.Json(NoSessionJson));

        var detail = await client.GetTrainingAttendanceByDateAsync(new DateOnly(2026, 3, 9));

        Assert.NotNull(detail);
        Assert.Null(detail.SessionId);
        Assert.Equal(new DateOnly(2026, 3, 9), detail.Date);
        Assert.Empty(detail.Attendees);
    }

    [Fact]
    public async Task TrainingDates_DeserializesDateOnlyArray()
    {
        var (client, _) = Build(StubHttpMessageHandler.Json("""["2026-03-09","2026-03-02"]"""));

        var dates = await client.GetTrainingDatesAsync();

        Assert.NotNull(dates);
        Assert.Equal([new DateOnly(2026, 3, 9), new DateOnly(2026, 3, 2)], dates);
    }

    // ---------- bearer token ----------

    [Fact]
    public async Task AttachesBearerToken_WhenProviderReturnsOne()
    {
        var (client, handler) = Build(StubHttpMessageHandler.Json("[]"), token: "abc.def.ghi");
        await client.GetMembersAsync();

        Assert.Equal("Bearer", handler.LastRequest.Headers.Authorization?.Scheme);
        Assert.Equal("abc.def.ghi", handler.LastRequest.Headers.Authorization?.Parameter);
    }

    [Fact]
    public async Task OmitsAuthorizationHeader_WhenProviderReturnsNull()
    {
        var (client, handler) = Build(StubHttpMessageHandler.Json("[]"), token: null);
        await client.GetMembersAsync();

        Assert.Null(handler.LastRequest.Headers.Authorization);
    }

    [Fact]
    public async Task AttachesBearerToken_OnMutatingCallsToo()
    {
        var (client, handler) = Build(StubHttpMessageHandler.Status(HttpStatusCode.Created), token: "t");
        await client.CreateMemberAsync(new Member());

        Assert.Equal("t", handler.LastRequest.Headers.Authorization?.Parameter);
    }

    // ---------- wire format ----------

    // The API registers no JsonStringEnumConverter, so enums cross the wire as their ordinal
    // integers even though they are stored as strings in the database. The shared enum
    // declarations must therefore keep their current member order. If anyone adds a string
    // enum converter on either side, or reorders a member, this test fails.
    [Fact]
    public async Task SerializesMember_WithCamelCaseKeysAndIntegerEnums()
    {
        var (client, handler) = Build(StubHttpMessageHandler.Status(HttpStatusCode.Created));

        await client.CreateMemberAsync(new Member
        {
            Id = Guid.Empty,
            FirstName = "Klas",
            LastName = "Nyman",
            DateOfBirth = new DateOnly(1990, 5, 17),
            JoinDate = new DateOnly(2020, 1, 2),
            IsActive = true,
            PreferredBowClass = BowClass.Compound
        });

        using var doc = JsonDocument.Parse(handler.LastBody);
        var root = doc.RootElement;

        Assert.Equal("Klas", root.GetProperty("firstName").GetString());
        Assert.Equal("1990-05-17", root.GetProperty("dateOfBirth").GetString());
        Assert.Equal("2020-01-02", root.GetProperty("joinDate").GetString());
        Assert.True(root.GetProperty("isActive").GetBoolean());

        // An integer, not the string "Compound".
        Assert.Equal(JsonValueKind.Number, root.GetProperty("preferredBowClass").ValueKind);
        Assert.Equal((int)BowClass.Compound, root.GetProperty("preferredBowClass").GetInt32());
    }

    [Fact]
    public async Task DoesNotSerializeComputedDisplayProperties()
    {
        var (client, handler) = Build(StubHttpMessageHandler.Status(HttpStatusCode.Created));
        await client.CreateMemberAsync(new Member { FirstName = "A", LastName = "B" });

        using var doc = JsonDocument.Parse(handler.LastBody);
        Assert.False(doc.RootElement.TryGetProperty("fullName", out _));
    }

    [Fact]
    public async Task DoesNotSerializeParticipantName()
    {
        var (client, handler) = Build(StubHttpMessageHandler.Status(HttpStatusCode.Created));
        await client.CreateResultAsync(new CompetitionResult
        {
            CompetitionId = Guid.NewGuid(),
            MemberId = Guid.NewGuid(),
            TotalScore = 560,
            XCount = 12
        });

        using var doc = JsonDocument.Parse(handler.LastBody);
        Assert.False(doc.RootElement.TryGetProperty("participantName", out _));
        Assert.Equal(560, doc.RootElement.GetProperty("totalScore").GetInt32());
    }

    [Fact]
    public async Task SaveTrainingAttendance_SendsSelectedIdsInBothCollections()
    {
        var memberId = Guid.NewGuid();
        var externalId = Guid.NewGuid();
        var (client, handler) = Build(StubHttpMessageHandler.Status(HttpStatusCode.OK));

        await client.SaveTrainingAttendanceAsync(new DateOnly(2026, 3, 9),
            new SaveTrainingAttendanceRequest
            {
                Notes = "Indoor",
                MemberIds = [memberId],
                ExternalParticipantIds = [externalId]
            });

        Assert.Equal(HttpMethod.Put, handler.LastRequest.Method);
        using var doc = JsonDocument.Parse(handler.LastBody);
        Assert.Equal("Indoor", doc.RootElement.GetProperty("notes").GetString());
        Assert.Equal(memberId, doc.RootElement.GetProperty("memberIds")[0].GetGuid());
        Assert.Equal(externalId, doc.RootElement.GetProperty("externalParticipantIds")[0].GetGuid());
    }

    [Fact]
    public async Task DeserializesIntegerEnumsBackToTypedValues()
    {
        var (client, _) = Build(StubHttpMessageHandler.Json(
            """
            [{"id":"11111111-1111-1111-1111-111111111111","firstName":"A","lastName":"B",
              "dateOfBirth":"1990-05-17","joinDate":"2020-01-02","isActive":true,
              "preferredBowClass":2}]
            """));

        var members = await client.GetMembersAsync();

        Assert.NotNull(members);
        Assert.Equal(BowClass.Barebow, members[0].PreferredBowClass);
        Assert.Equal("A B", members[0].FullName);
    }

    // ---------- error handling on the GET path ----------

    [Fact]
    public async Task GetThrowsArcheryApiException_CarryingStatusAndMessage()
    {
        var (client, _) = Build(StubHttpMessageHandler.Status(
            HttpStatusCode.BadRequest, """{"errors":["Personnummer is invalid."]}"""));

        var ex = await Assert.ThrowsAsync<ArcheryApiException>(() => client.GetMembersAsync());

        Assert.Equal(400, ex.Error.StatusCode);
        Assert.Equal(["Personnummer is invalid."], ex.Error.Messages);
        Assert.False(ex.IsUnauthorized);
    }

    // aspire.Web's pages relied on EnsureSuccessStatusCode throwing HttpRequestException, so
    // the typed exception must remain catchable as the base type.
    [Fact]
    public async Task ArcheryApiException_IsCatchableAsHttpRequestException()
    {
        var (client, _) = Build(StubHttpMessageHandler.Status(HttpStatusCode.InternalServerError));

        var caughtAsBase = false;
        try
        {
            await client.GetMembersAsync();
        }
        catch (HttpRequestException)
        {
            caughtAsBase = true;
        }

        Assert.True(caughtAsBase);
    }

    [Fact]
    public async Task UnauthorizedIsFlagged()
    {
        var (client, _) = Build(StubHttpMessageHandler.Status(HttpStatusCode.Unauthorized));

        var ex = await Assert.ThrowsAsync<ArcheryApiException>(() => client.GetDashboardAsync());

        Assert.True(ex.IsUnauthorized);
    }

    // ---------- export passthrough (used by the web host, not mobile) ----------

    [Fact]
    public async Task ExportMembers_ReturnsNullOnFailureRatherThanThrowing()
    {
        var (client, _) = Build(StubHttpMessageHandler.Status(HttpStatusCode.BadRequest));

        Assert.Null(await client.ExportMembersAsync("csv"));
    }

    [Fact]
    public async Task ExportMembers_ReadsFileNameFromContentDisposition()
    {
        var handler = new StubHttpMessageHandler(_ =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent([1, 2, 3])
            };
            response.Content.Headers.ContentType = new("text/csv");
            response.Content.Headers.ContentDisposition =
                new("attachment") { FileName = "\"members.csv\"" };
            return response;
        });

        var (client, _) = Build(handler);
        var file = await client.ExportMembersAsync("csv");

        Assert.NotNull(file);
        Assert.Equal("members.csv", file.FileName);
        Assert.Equal("text/csv", file.ContentType);
        Assert.Equal([1, 2, 3], file.Content);
    }
}
