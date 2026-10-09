using System.Text.Json;
using FluentAssertions;
using Microsoft.Playwright;

namespace WordBuddy.E2E.Api.Tests;

/// <summary>
/// WB-26: supporter dashboard and resumable sessions.
/// Supporter links the learner → learner reviews words → supporter dashboard shows the reviews and retention.
/// Word states and link events travel through RabbitMQ, so the full stack must run (with the WB-26 Progress build).
/// </summary>
[Collection(ApiRequestContextCollection.Name)]
public sealed class SupporterDashboardTests
{
    private const string ClientDateHeader = "X-Client-CurrentDateTime";
    private const string Password = "ChangeMe123!";
    private const int WordCount = 4;
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    private readonly ApiRequestContextFixture _fixture;

    public SupporterDashboardTests(ApiRequestContextFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task SupporterDashboard_AfterLearnerReviews_ShowsActivityAndRetention()
    {
        await using Contexts c = await Contexts.CreateAsync(_fixture);
        (string learnerToken, Guid learnerId) = await RegisterAsync(c.Identity, "Adult");
        (string supporterToken, _) = await RegisterAsync(c.Identity, "Adult");
        await LinkAsync(c.Identity, learnerToken, supporterToken);
        await AddWordsAsync(c.Content, learnerToken);
        await WaitForStatesAsync(c.Progress, learnerToken);

        JsonElement session = await GetSessionAsync(c.Progress, learnerToken);
        Guid sessionId = session.GetProperty("sessionId").GetGuid();
        List<Guid> senseIds = session.GetProperty("dueItems").EnumerateArray()
            .Concat(session.GetProperty("newItems").EnumerateArray())
            .Select(i => i.GetProperty("senseId").GetGuid())
            .ToList();
        senseIds.Should().HaveCount(WordCount);

        foreach (Guid senseId in senseIds)
        {
            IAPIResponse review = await c.Progress.PostAsync("/api/progress/vocabulary/reviews", new APIRequestContextOptions
            {
                Headers = Auth(learnerToken),
                DataObject = new
                {
                    sessionId,
                    senseId,
                    exerciseType = "PictureChoice",
                    skill = "Meaning",
                    isCorrect = true,
                    responseMs = 2000,
                    hintUsed = false,
                },
            });
            review.Status.Should().Be(200, await review.TextAsync());
        }

        // The supporter link reaches Progress through RabbitMQ.
        await EventuallyAsync(async () => (await GetLearnerDashboardAsync(c.Progress, supporterToken, learnerId)).Status == 200,
            "supporter can read the learner dashboard");

        IAPIResponse response = await GetLearnerDashboardAsync(c.Progress, supporterToken, learnerId);
        string body = await response.TextAsync();
        JsonElement dashboard = JsonDocument.Parse(body).RootElement;

        dashboard.GetProperty("learnerId").GetGuid().Should().Be(learnerId);
        dashboard.GetProperty("activity").GetProperty("currentStreakDays").GetInt32().Should().BeGreaterThanOrEqualTo(1);
        dashboard.GetProperty("activity").GetProperty("heatmap").EnumerateArray()
            .Sum(d => d.GetProperty("reviews").GetInt32()).Should().Be(WordCount);

        // Retention counts first attempts of due words only. Brand-new words are not due, so a fresh
        // learner has no sample and no percentage yet: the field is present and null (below MinSample).
        JsonElement retention = dashboard.GetProperty("retention");
        retention.TryGetProperty("overall", out JsonElement overall).Should().BeTrue();
        retention.GetProperty("sample").GetInt32().Should().BeGreaterThanOrEqualTo(0);
        if (overall.ValueKind != JsonValueKind.Null)
        {
            overall.GetDouble().Should().BeInRange(0, 100);
        }

        // Dates only: no time-of-day field in the body.
        body.Should().NotContainEquivalentOf("occurredAt").And.NotContainEquivalentOf("utc\"");

        // The learner's own view returns the same learner.
        IAPIResponse mine = await c.Progress.GetAsync("/api/progress/dashboard/me?days=30", Options(learnerToken, ClientDate()));
        mine.Status.Should().Be(200, await mine.TextAsync());
        (await mine.JsonAsync())!.Value.GetProperty("learnerId").GetGuid().Should().Be(learnerId);
    }

    [Fact]
    public async Task SupporterDashboard_NoLink_Returns403()
    {
        await using Contexts c = await Contexts.CreateAsync(_fixture);
        (_, Guid learnerId) = await RegisterAsync(c.Identity, "Adult");
        (string strangerToken, _) = await RegisterAsync(c.Identity, "Adult");

        IAPIResponse response = await GetLearnerDashboardAsync(c.Progress, strangerToken, learnerId);

        response.Status.Should().Be(403);
    }

    [Fact]
    public async Task VocabularySession_GetTwice_ReturnsSameSessionId()
    {
        await using Contexts c = await Contexts.CreateAsync(_fixture);
        (string token, _) = await RegisterAsync(c.Identity, "Adult");
        await AddWordsAsync(c.Content, token);
        await WaitForStatesAsync(c.Progress, token);

        JsonElement first = await GetSessionAsync(c.Progress, token);
        JsonElement second = await GetSessionAsync(c.Progress, token);

        second.GetProperty("sessionId").GetGuid().Should().Be(first.GetProperty("sessionId").GetGuid());
        second.GetProperty("newItems").GetArrayLength().Should().Be(first.GetProperty("newItems").GetArrayLength());
    }

    private static async Task<JsonElement> GetSessionAsync(IAPIRequestContext progress, string token)
    {
        IAPIResponse response = await progress.GetAsync("/api/progress/vocabulary/session", Options(token, ClientDate()));
        response.Status.Should().Be(200, await response.TextAsync());
        return (await response.JsonAsync())!.Value.Clone();
    }

    private static Task<IAPIResponse> GetLearnerDashboardAsync(IAPIRequestContext progress, string token, Guid learnerId) =>
        progress.GetAsync($"/api/progress/dashboard/learners/{learnerId}?days=30", Options(token, ClientDate()));

    private static Dictionary<string, string> ClientDate() =>
        new() { [ClientDateHeader] = DateTimeOffset.Now.ToString("yyyy-MM-ddTHH:mm:sszzz") };

    private static async Task LinkAsync(IAPIRequestContext identity, string learnerToken, string supporterToken)
    {
        IAPIResponse invitation = await identity.PostAsync("/api/auth/support-links/invitations", new APIRequestContextOptions
        {
            Headers = Auth(learnerToken),
            DataObject = new { inviteAs = "Learner", relationship = (string?)null },
        });
        invitation.Status.Should().Be(201, await invitation.TextAsync());
        string code = (await invitation.JsonAsync())!.Value.GetProperty("code").GetString()!;

        IAPIResponse accept = await identity.PostAsync("/api/auth/support-links/accept", new APIRequestContextOptions
        {
            Headers = Auth(supporterToken),
            DataObject = new { code, token = (string?)null },
        });
        accept.Status.Should().Be(200, await accept.TextAsync());
    }

    private static async Task AddWordsAsync(IAPIRequestContext content, string token)
    {
        for (int i = 0; i < WordCount; i++)
        {
            IAPIResponse add = await content.PostAsync("/api/vocabulary", new APIRequestContextOptions
            {
                Headers = Auth(token),
                DataObject = new { word = $"dash-{i}-{Guid.NewGuid():N}", definition = $"e2e dashboard definition {i}", example = (string?)null },
            });
            add.Ok.Should().BeTrue($"adding a word should succeed, got {add.Status}: {await add.TextAsync()}");
        }
    }

    private static async Task WaitForStatesAsync(IAPIRequestContext progress, string token)
    {
        await EventuallyAsync(async () =>
        {
            IAPIResponse response = await progress.GetAsync("/api/progress/vocabulary/words", Options(token));
            return response.Status == 200 && (await response.JsonAsync())!.Value.GetArrayLength() == WordCount;
        }, $"{WordCount} word states reach Progress");
    }

    private static async Task EventuallyAsync(Func<Task<bool>> condition, string because)
    {
        DateTime deadline = DateTime.UtcNow + Timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (await condition())
            {
                return;
            }

            await Task.Delay(500);
        }

        throw new TimeoutException($"Timed out after {Timeout.TotalSeconds}s: {because}.");
    }

    private static async Task<(string Token, Guid UserId)> RegisterAsync(IAPIRequestContext identity, string ageGroup)
    {
        IAPIResponse response = await identity.PostAsync("/api/auth/register", new APIRequestContextOptions
        {
            DataObject = new
            {
                email = $"dashboard-e2e-{Guid.NewGuid():N}@example.com",
                password = Password,
                displayName = "E2E Dashboard User",
                ageGroup,
            },
        });
        response.Ok.Should().BeTrue($"self-registration should succeed, got {response.Status}: {await response.TextAsync()}");
        JsonElement body = (await response.JsonAsync())!.Value;
        return (body.GetProperty("token").GetString()!, body.GetProperty("user").GetProperty("id").GetGuid());
    }

    private static APIRequestContextOptions Options(string token, Dictionary<string, string>? extra = null)
    {
        Dictionary<string, string> headers = Auth(token);
        foreach (KeyValuePair<string, string> pair in extra ?? new())
        {
            headers[pair.Key] = pair.Value;
        }

        return new APIRequestContextOptions { Headers = headers };
    }

    private static Dictionary<string, string> Auth(string token) => new() { ["Authorization"] = $"Bearer {token}" };

    private sealed class Contexts : IAsyncDisposable
    {
        public required IAPIRequestContext Identity { get; init; }
        public required IAPIRequestContext Content { get; init; }
        public required IAPIRequestContext Progress { get; init; }

        public static async Task<Contexts> CreateAsync(ApiRequestContextFixture fixture) => new()
        {
            Identity = await fixture.NewContextAsync(ServiceUrls.Identity),
            Content = await fixture.NewContextAsync(ServiceUrls.Content),
            Progress = await fixture.NewContextAsync(ServiceUrls.Progress),
        };

        public async ValueTask DisposeAsync()
        {
            await Identity.DisposeAsync();
            await Content.DisposeAsync();
            await Progress.DisposeAsync();
        }
    }
}
