using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WordBuddy.Progress.Application.DTOs;
using ApiProgram = WordBuddy.Progress.Api.Program;
using WordBuddy.Progress.Application.Features.Dashboard.Queries.GetLearnerDashboard;
using WordBuddy.Progress.Domain;
using WordBuddy.Progress.Infrastructure.Persistence;

namespace WordBuddy.Progress.IntegrationTests;

/// <summary>
/// WB-26 dashboard endpoints on real SQL Server: <c>me</c> (<c>ChildHasSupporter</c>) and
/// <c>learners/{id}</c> (<c>CanSupportLearner</c>), the date-only response and the log content.
/// Each test uses fresh user ids.
/// </summary>
[Collection(ProgressApiCollection.Name)]
public sealed class DashboardEndpointsTests
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly ProgressApiFactory _factory;

    public DashboardEndpointsTests(ProgressApiFactory factory)
    {
        _factory = factory;
    }

    private static HttpClient Client(HttpClient client, Guid userId, string ageGroup)
    {
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestJwtTokenFactory.CreateToken(userId, ageGroup));
        return client;
    }

    private HttpClient Client(Guid userId, string ageGroup) => Client(_factory.CreateClient(), userId, ageGroup);

    private async Task SeedLinkAsync(Guid learnerId, Guid supporterId, bool isActive)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ProgressDbContext dbContext = scope.ServiceProvider.GetRequiredService<ProgressDbContext>();
        dbContext.SupportLinkProjections.Add(
            SupportLinkProjection.Create(Guid.NewGuid(), learnerId, supporterId, isActive, DateTime.UtcNow));
        await dbContext.SaveChangesAsync();
    }

    private async Task SeedReviewsAsync(Guid userId, int count, int responseMs)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ProgressDbContext dbContext = scope.ServiceProvider.GetRequiredService<ProgressDbContext>();
        Guid sessionId = Guid.NewGuid();
        for (int i = 0; i < count; i++)
        {
            dbContext.ReviewLogs.Add(ReviewLog.Create(
                Guid.NewGuid(), userId, Guid.NewGuid(), sessionId, DateTime.UtcNow.AddMinutes(-5),
                ExerciseType.PictureChoice, VocabularySkill.Meaning, isCorrect: i % 2 == 0, responseMs, hintUsed: false,
                isDue: true, attemptNo: 1, FsrsRating.Good));
        }

        await dbContext.SaveChangesAsync();
    }

    // GET me

    [Fact]
    public async Task DashboardController_GetMine_AdultReturns200()
    {
        Guid adultId = Guid.NewGuid();

        HttpResponseMessage response = await Client(adultId, "Adult").GetAsync("/api/progress/dashboard/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        LearnerDashboardDto? dashboard = await response.Content.ReadFromJsonAsync<LearnerDashboardDto>(Json);
        dashboard!.LearnerId.Should().Be(adultId);
        dashboard.Days.Should().Be(30);
    }

    [Fact]
    public async Task DashboardController_GetMine_ChildWithSupporterReturns200()
    {
        Guid childId = Guid.NewGuid();
        await SupportLinkTestSeed.SeedActiveSupporterAsync(_factory.Services, childId);

        HttpResponseMessage response = await Client(childId, "Child").GetAsync("/api/progress/dashboard/me?days=7");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<LearnerDashboardDto>(Json))!.Days.Should().Be(7);
    }

    [Fact]
    public async Task DashboardController_GetMine_ChildWithoutSupporterReturns403SupporterRequired()
    {
        HttpResponseMessage response = await Client(Guid.NewGuid(), "Child").GetAsync("/api/progress/dashboard/me");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        JsonElement problem = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        problem.GetProperty("title").GetString().Should().Be("Learner.SupporterRequired");
    }

    [Fact]
    public async Task DashboardController_GetMine_InvalidDaysReturns400()
    {
        HttpResponseMessage response = await Client(Guid.NewGuid(), "Adult").GetAsync("/api/progress/dashboard/me?days=14");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DashboardController_GetMine_WithoutTokenReturns401()
    {
        HttpResponseMessage response = await _factory.CreateClient().GetAsync("/api/progress/dashboard/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // GET learners/{id}

    [Fact]
    public async Task DashboardController_GetLearner_ActiveSupporterOfAdultReturns200()
    {
        Guid learnerId = Guid.NewGuid();
        Guid supporterId = Guid.NewGuid();
        await SeedLinkAsync(learnerId, supporterId, isActive: true);
        await SeedReviewsAsync(learnerId, count: 12, responseMs: 4000);

        HttpResponseMessage response = await Client(supporterId, "Adult").GetAsync($"/api/progress/dashboard/learners/{learnerId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        LearnerDashboardDto? dashboard = await response.Content.ReadFromJsonAsync<LearnerDashboardDto>(Json);
        dashboard!.LearnerId.Should().Be(learnerId);
        dashboard.Retention.Sample.Should().Be(12);
        dashboard.Retention.Overall.Should().Be(50);
    }

    [Fact]
    public async Task DashboardController_GetLearner_ActiveSupporterOfChildReturns200()
    {
        Guid childId = Guid.NewGuid();
        Guid supporterId = Guid.NewGuid();
        await SeedLinkAsync(childId, supporterId, isActive: true);

        HttpResponseMessage response = await Client(supporterId, "Adult").GetAsync($"/api/progress/dashboard/learners/{childId}?days=90");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DashboardController_GetLearner_RevokedLinkReturns403()
    {
        Guid learnerId = Guid.NewGuid();
        Guid supporterId = Guid.NewGuid();
        await SeedLinkAsync(learnerId, supporterId, isActive: false);

        HttpResponseMessage response = await Client(supporterId, "Adult").GetAsync($"/api/progress/dashboard/learners/{learnerId}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DashboardController_GetLearner_NoLinkReturns403()
    {
        HttpResponseMessage response = await Client(Guid.NewGuid(), "Adult").GetAsync($"/api/progress/dashboard/learners/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DashboardController_GetLearner_LearnerCallingOwnIdWithoutLinkReturns403()
    {
        Guid learnerId = Guid.NewGuid();

        HttpResponseMessage response = await Client(learnerId, "Adult").GetAsync($"/api/progress/dashboard/learners/{learnerId}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // Privacy

    [Fact]
    public async Task DashboardController_GetLearner_ResponseBodyHasNoTimeOfDay()
    {
        Guid learnerId = Guid.NewGuid();
        Guid supporterId = Guid.NewGuid();
        await SeedLinkAsync(learnerId, supporterId, isActive: true);
        await SeedReviewsAsync(learnerId, count: 12, responseMs: 4000);

        HttpResponseMessage response = await Client(supporterId, "Adult").GetAsync($"/api/progress/dashboard/learners/{learnerId}");
        string body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.Should().MatchRegex(@"\d{4}-\d{2}-\d{2}");
        Regex.IsMatch(body, @"\d{2}:\d{2}").Should().BeFalse("no time of day may be serialized");
        Regex.IsMatch(body, @"\d{4}-\d{2}-\d{2}T").Should().BeFalse("no date-time may be serialized");
    }

    [Fact]
    public async Task DashboardController_GetMine_ChildCallLogsContainNoMetricValuesOrTimings()
    {
        const int DistinctResponseMs = 7351;
        Guid childId = Guid.NewGuid();
        await SupportLinkTestSeed.SeedActiveSupporterAsync(_factory.Services, childId);
        await SeedReviewsAsync(childId, count: 12, responseMs: DistinctResponseMs);
        CapturingLogger<GetLearnerDashboardQueryHandler> logger = new();

        using WebApplicationFactory<ApiProgram> derived = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services => services.AddSingleton<ILogger<GetLearnerDashboardQueryHandler>>(logger)));
        HttpResponseMessage response = await Client(derived.CreateClient(), childId, "Child").GetAsync("/api/progress/dashboard/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        logger.Entries.Should().NotBeEmpty();
        string[] allowedProperties = ["LearnerId", "Days", "ReviewCount", "ErrorCode", "Errors", "{OriginalFormat}"];
        logger.Entries.SelectMany(e => e.PropertyNames).Should().OnlyContain(name => allowedProperties.Contains(name));
        logger.Entries.Select(e => e.Message).Should().NotContain(m => m.Contains(DistinctResponseMs.ToString()) || m.Contains("Retention") || m.Contains("Median"));
    }

    private sealed record LogEntry(string Message, IReadOnlyList<string> PropertyNames);

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<LogEntry> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            List<string> names = state is IEnumerable<KeyValuePair<string, object?>> pairs ? pairs.Select(p => p.Key).ToList() : [];
            Entries.Add(new LogEntry(formatter(state, exception), names));
        }
    }
}
