using System.Text.Json;
using FluentAssertions;
using Microsoft.Playwright;

namespace WordBuddy.E2E.Api.Tests;

/// <summary>
/// WB-22: vocabulary SRS endpoints in Progress (<c>/api/progress/vocabulary/*</c>) and the Content
/// backfill guard. Blackbox HTTP only. Word states arrive through RabbitMQ, so the full stack must run.
/// </summary>
[Collection(ApiRequestContextCollection.Name)]
public sealed class VocabularySrsTests
{
    private const string ClientDateHeader = "X-Client-CurrentDateTime";
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    private readonly ApiRequestContextFixture _fixture;

    public VocabularySrsTests(ApiRequestContextFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetSession_WithoutToken_Returns401()
    {
        IAPIRequestContext progress = await _fixture.NewContextAsync(ServiceUrls.Progress);
        try
        {
            IAPIResponse response = await progress.GetAsync("/api/progress/vocabulary/session");
            response.Status.Should().Be(401);
        }
        finally
        {
            await progress.DisposeAsync();
        }
    }

    [Theory]
    [InlineData("Adult")]
    [InlineData("Child")]
    public async Task AddWord_Learner_ShowsNewStateAndNewSessionItem(string ageGroup)
    {
        await using Contexts c = await Contexts.CreateAsync(_fixture);
        string token = await RegisterAsync(c.Identity, ageGroup);
        Guid senseId = await AddWordAndWaitForStateAsync(c, token);

        IAPIResponse session = await c.Progress.GetAsync("/api/progress/vocabulary/session", Options(token,
            new() { [ClientDateHeader] = DateTimeOffset.Now.ToString("yyyy-MM-ddTHH:mm:sszzz") }));
        session.Status.Should().Be(200, await session.TextAsync());
        JsonElement body = (await session.JsonAsync())!.Value;
        body.GetProperty("sessionId").GetGuid().Should().NotBeEmpty();
        body.GetProperty("dueItems").GetArrayLength().Should().Be(0);
        body.GetProperty("newItems").EnumerateArray().Select(i => i.GetProperty("senseId").GetGuid())
            .Should().ContainSingle().Which.Should().Be(senseId);
        body.GetProperty("newWordCap").GetInt32().Should().Be(10, "backlog 0 → cap 10 for every age group (D-3)");
    }

    [Theory]
    [InlineData("Adult", "Good")]
    [InlineData("Child", "Easy")]
    public async Task RecordReview_CorrectIn3500Ms_RatingFollowsAgeGroup(string ageGroup, string expectedRating)
    {
        // PictureChoice fast threshold: adult 3000 ms, child 3750 ms (×1.25, D-7).
        await using Contexts c = await Contexts.CreateAsync(_fixture);
        string token = await RegisterAsync(c.Identity, ageGroup);
        Guid senseId = await AddWordAndWaitForStateAsync(c, token);

        JsonElement result = await PostReviewAsync(c.Progress, token, Guid.NewGuid(), senseId, isCorrect: true, responseMs: 3500);

        result.GetProperty("rating").GetString().Should().Be(expectedRating);
        result.GetProperty("status").GetString().Should().NotBe("New");

        JsonElement state = (await GetWordsAsync(c.Progress, token)).Single(s => s.GetProperty("senseId").GetGuid() == senseId);
        state.GetProperty("reps").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task RecordReview_SecondAttemptSameSession_DoesNotReschedule()
    {
        await using Contexts c = await Contexts.CreateAsync(_fixture);
        string token = await RegisterAsync(c.Identity, "Adult");
        Guid senseId = await AddWordAndWaitForStateAsync(c, token);
        Guid sessionId = Guid.NewGuid();

        await PostReviewAsync(c.Progress, token, sessionId, senseId, isCorrect: false, responseMs: 2000);
        await PostReviewAsync(c.Progress, token, sessionId, senseId, isCorrect: true, responseMs: 2000);

        JsonElement state = (await GetWordsAsync(c.Progress, token)).Single(s => s.GetProperty("senseId").GetGuid() == senseId);
        state.GetProperty("reps").GetInt32().Should().Be(1, "only the first due attempt in a session schedules");
    }

    [Fact]
    public async Task RecordReview_WordNotInList_Returns404()
    {
        await using Contexts c = await Contexts.CreateAsync(_fixture);
        string token = await RegisterAsync(c.Identity, "Adult");

        IAPIResponse response = await c.Progress.PostAsync("/api/progress/vocabulary/reviews",
            ReviewOptions(token, Guid.NewGuid(), Guid.NewGuid(), true, 2000));
        response.Status.Should().Be(404, await response.TextAsync());
    }

    [Fact]
    public async Task RecordReview_ResponseMsOutOfRange_Returns400()
    {
        await using Contexts c = await Contexts.CreateAsync(_fixture);
        string token = await RegisterAsync(c.Identity, "Adult");

        IAPIResponse response = await c.Progress.PostAsync("/api/progress/vocabulary/reviews",
            ReviewOptions(token, Guid.NewGuid(), Guid.NewGuid(), true, 600_001));
        response.Status.Should().Be(400, await response.TextAsync());
    }

    [Theory]
    [InlineData("not-a-date")]
    [InlineData("2026-10-07T09:30:00+15:00")]
    [InlineData("2020-01-01T00:00:00+00:00")]
    public async Task GetSession_BadClientDateTime_Returns400(string header)
    {
        await using Contexts c = await Contexts.CreateAsync(_fixture);
        string token = await RegisterAsync(c.Identity, "Adult");

        IAPIResponse response = await c.Progress.GetAsync("/api/progress/vocabulary/session",
            Options(token, new() { [ClientDateHeader] = header }));
        response.Status.Should().Be(400, await response.TextAsync());
    }

    [Theory]
    [InlineData("Adult")]
    [InlineData("Child")]
    public async Task UpdateSettings_ValidCap_IsReturnedByGet(string ageGroup)
    {
        // D-4: child accounts get the same settings as adults in WB-22.
        await using Contexts c = await Contexts.CreateAsync(_fixture);
        string token = await RegisterAsync(c.Identity, ageGroup);

        IAPIResponse put = await c.Progress.PutAsync("/api/progress/vocabulary/settings",
            new APIRequestContextOptions { Headers = Auth(token), DataObject = new { newWordsPerDay = 5 } });
        put.Status.Should().Be(200, await put.TextAsync());

        IAPIResponse get = await c.Progress.GetAsync("/api/progress/vocabulary/settings", Options(token));
        (await get.JsonAsync())!.Value.GetProperty("newWordsPerDay").GetInt32().Should().Be(5);
    }

    [Fact]
    public async Task UpdateSettings_CapAbove50_Returns400()
    {
        await using Contexts c = await Contexts.CreateAsync(_fixture);
        string token = await RegisterAsync(c.Identity, "Adult");

        IAPIResponse put = await c.Progress.PutAsync("/api/progress/vocabulary/settings",
            new APIRequestContextOptions { Headers = Auth(token), DataObject = new { newWordsPerDay = 51 } });
        put.Status.Should().Be(400, await put.TextAsync());
    }

    [Fact]
    public async Task RepublishLearnerWords_NonAdmin_Returns403()
    {
        await using Contexts c = await Contexts.CreateAsync(_fixture);
        string token = await RegisterAsync(c.Identity, "Adult");

        IAPIResponse response = await c.Content.PostAsync("/api/vocabulary/admin/learner-words/republish", Options(token));
        response.Status.Should().Be(403);
    }

    private static async Task<JsonElement> PostReviewAsync(
        IAPIRequestContext progress, string token, Guid sessionId, Guid senseId, bool isCorrect, int responseMs)
    {
        IAPIResponse response = await progress.PostAsync("/api/progress/vocabulary/reviews",
            ReviewOptions(token, sessionId, senseId, isCorrect, responseMs));
        response.Status.Should().Be(200, await response.TextAsync());
        return (await response.JsonAsync())!.Value;
    }

    private static APIRequestContextOptions ReviewOptions(string token, Guid sessionId, Guid senseId, bool isCorrect, int responseMs) => new()
    {
        Headers = Auth(token),
        DataObject = new
        {
            sessionId,
            senseId,
            exerciseType = "PictureChoice",
            skill = "Meaning",
            isCorrect,
            responseMs,
            hintUsed = false,
        },
    };

    private static async Task<List<JsonElement>> GetWordsAsync(IAPIRequestContext progress, string token)
    {
        IAPIResponse response = await progress.GetAsync("/api/progress/vocabulary/words", Options(token));
        response.Status.Should().Be(200, await response.TextAsync());
        return (await response.JsonAsync())!.Value.EnumerateArray().Select(e => e.Clone()).ToList();
    }

    /// <summary>Adds a word in Content and waits for its state to reach Progress (via RabbitMQ).</summary>
    private static async Task<Guid> AddWordAndWaitForStateAsync(Contexts c, string token)
    {
        IAPIResponse add = await c.Content.PostAsync("/api/vocabulary", new APIRequestContextOptions
        {
            Headers = Auth(token),
            DataObject = new { word = $"srs-{Guid.NewGuid():N}", definition = "an e2e definition", example = (string?)null },
        });
        add.Ok.Should().BeTrue($"adding a word should succeed, got {add.Status}: {await add.TextAsync()}");

        DateTime deadline = DateTime.UtcNow + Timeout;
        while (DateTime.UtcNow < deadline)
        {
            List<JsonElement> words = await GetWordsAsync(c.Progress, token);
            if (words.Count == 1)
            {
                words[0].GetProperty("status").GetString().Should().Be("New");
                return words[0].GetProperty("senseId").GetGuid();
            }

            await Task.Delay(500);
        }

        throw new TimeoutException($"No word state reached Progress within {Timeout.TotalSeconds}s.");
    }

    private static async Task<string> RegisterAsync(IAPIRequestContext identity, string ageGroup)
    {
        IAPIResponse response = await identity.PostAsync("/api/auth/register", new APIRequestContextOptions
        {
            DataObject = new
            {
                email = $"srs-e2e-{Guid.NewGuid():N}@example.com",
                password = "ChangeMe123!",
                displayName = "E2E SRS Learner",
                ageGroup,
            },
        });
        response.Ok.Should().BeTrue($"self-registration should succeed, got {response.Status}: {await response.TextAsync()}");
        string token = (await response.JsonAsync())!.Value.GetProperty("token").GetString()!;
        if (ageGroup == "Child")
        {
            // WB-24: a child needs an active supporter before learning.
            await ChildSupport.LinkSupporterAsync(identity, token);
        }

        return token;
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
