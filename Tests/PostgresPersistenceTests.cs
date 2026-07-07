using aspire.ApiService.Data;
using aspire.ApiService.Infrastructure;
using aspire.ApiService.Models;
using aspire.ApiService.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Tests;

public class PostgresPersistenceTests
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

    [Fact]
    public async Task MembershipFeeService_CreateAsync_ThrowsConflict_ForDuplicateMemberYear()
    {
        var ct = TestContext.Current.CancellationToken;

        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.aspire_AppHost>(ct);
        appHost.Services.AddLogging(logging =>
        {
            logging.SetMinimumLevel(LogLevel.Warning);
            logging.AddFilter(appHost.Environment.ApplicationName, LogLevel.Warning);
            logging.AddFilter("Aspire.", LogLevel.Warning);
        });
        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler();
        });

        await using var app = await appHost.BuildAsync(ct).WaitAsync(DefaultTimeout, ct);
        await app.StartAsync(ct).WaitAsync(DefaultTimeout, ct);
        await app.ResourceNotifications.WaitForResourceHealthyAsync("db", ct).WaitAsync(DefaultTimeout, ct);

        var connectionString = await app.GetConnectionStringAsync("db", ct);
        var memberId = Guid.NewGuid();
        var year = DateTime.Today.Year;

        await using (var db = CreateDb(connectionString))
        {
            db.Members.Add(new Member
            {
                Id = memberId,
                FirstName = "Fee",
                LastName = "Conflict",
                IsActive = true,
                DateOfBirth = new DateOnly(1990, 1, 1),
                JoinDate = new DateOnly(2020, 1, 1)
            });
            db.MembershipFees.Add(new MembershipFee
            {
                Id = Guid.NewGuid(),
                MemberId = memberId,
                Year = year,
                Amount = 500,
                DueDate = new DateOnly(year, 3, 31),
                Status = FeeStatus.Unpaid
            });
            await db.SaveChangesAsync(ct);
        }

        await using (var db = CreateDb(connectionString))
        {
            var svc = new MembershipFeeService(db);

            var ex = await Assert.ThrowsAsync<ConflictException>(() => svc.CreateAsync(new MembershipFee
            {
                MemberId = memberId,
                Year = year,
                Amount = 600,
                DueDate = new DateOnly(year, 4, 30),
                Status = FeeStatus.Unpaid
            }, ct));

            Assert.Equal("A membership fee already exists for this member and year.", ex.Message);
        }
    }

    [Fact]
    public async Task CompetitionParticipantService_RegisterAsync_ThrowsConflict_ForDuplicateRegistration()
    {
        var ct = TestContext.Current.CancellationToken;

        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.aspire_AppHost>(ct);
        appHost.Services.AddLogging(logging =>
        {
            logging.SetMinimumLevel(LogLevel.Warning);
            logging.AddFilter(appHost.Environment.ApplicationName, LogLevel.Warning);
            logging.AddFilter("Aspire.", LogLevel.Warning);
        });
        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler();
        });

        await using var app = await appHost.BuildAsync(ct).WaitAsync(DefaultTimeout, ct);
        await app.StartAsync(ct).WaitAsync(DefaultTimeout, ct);
        await app.ResourceNotifications.WaitForResourceHealthyAsync("db", ct).WaitAsync(DefaultTimeout, ct);

        var connectionString = await app.GetConnectionStringAsync("db", ct);
        var competitionId = Guid.NewGuid();
        var memberId = Guid.NewGuid();

        await using (var db = CreateDb(connectionString))
        {
            db.Members.Add(new Member
            {
                Id = memberId,
                FirstName = "Participant",
                LastName = "Conflict",
                IsActive = true,
                DateOfBirth = new DateOnly(1991, 1, 1),
                JoinDate = new DateOnly(2020, 1, 1)
            });
            db.Competitions.Add(new Competition
            {
                Id = competitionId,
                Name = $"Competition-{Guid.NewGuid():N}",
                Date = DateOnly.FromDateTime(DateTime.Today),
                Location = "Range",
                RoundType = "WA 18m",
                Type = CompetitionType.Indoor
            });
            db.CompetitionParticipants.Add(new CompetitionParticipant
            {
                Id = Guid.NewGuid(),
                CompetitionId = competitionId,
                MemberId = memberId,
                BowClass = BowClass.Recurve,
                AgeClass = AgeClass.Senior,
                Gender = Gender.Male
            });
            await db.SaveChangesAsync(ct);
        }

        await using (var db = CreateDb(connectionString))
        {
            var svc = new CompetitionParticipantService(db);

            var ex = await Assert.ThrowsAsync<ConflictException>(() => svc.RegisterAsync(new CompetitionParticipant
            {
                CompetitionId = competitionId,
                MemberId = memberId,
                BowClass = BowClass.Recurve,
                AgeClass = AgeClass.Senior,
                Gender = Gender.Male
            }, ct));

            Assert.Equal("This participant is already registered for the competition.", ex.Message);
        }
    }

    private static ArcheryDbContext CreateDb(string connectionString)
        => new(new DbContextOptionsBuilder<ArcheryDbContext>()
            .UseNpgsql(connectionString)
            .Options);
}
