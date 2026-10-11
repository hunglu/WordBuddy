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
        (Guid senseId, _) = await AddWordAndWaitForStateAsync(c, token);

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
    [InlineData("Adult")]
    [InlineData("Child")]
    public async Task RecordReview_RightAnswerSentAtOnce_IsCorrectAndRatedEasy(string ageGroup)
    {
        // One word in the session means fewer than 4 senses, so the server picks a Typing exercise.
        // The server clock is the reference: an answer sent at once has a tiny server time, so the client
        // time (0 ms) is plausible and the rating is Easy for both age groups.
        await using Contexts c = await Contexts.CreateAsync(_fixture);
        string token = await RegisterAsync(c.Identity, ageGroup);
        (Guid senseId, string word) = await AddWordAndWaitForStateAsync(c, token);
        (Guid sessionId, _) = await GetSessionAsync(c.Progress, token);

        JsonElement exercise = await VocabularyExercises.CreateAsync(c.Progress, token, sessionId, senseId);
        exercise.GetProperty("exerciseType").GetString().Should().Be("Typing");
        exercise.GetRawText().Should().NotContain(word, "the Typing prompt never contains the word");

        // The word, in other case and with spaces: the server trims and ignores case.
        IAPIResponse response = await VocabularyExercises.AnswerAsync(
            c.Progress, token, exercise.GetProperty("exerciseId").GetGuid(), null, $"  {word.ToUpperInvariant()} ");
        response.Status.Should().Be(200, await response.TextAsync());
        JsonElement result = (await response.JsonAsync())!.Value;

        result.GetProperty("isCorrect").GetBoolean().Should().BeTrue();
        result.GetProperty("correctAnswer").GetString().Should().Be(word);
        result.GetProperty("rating").GetString().Should().Be("Easy");
        result.GetProperty("status").GetString().Should().NotBe("New");

        JsonElement state = (await GetWordsAsync(c.Progress, token)).Single(s => s.GetProperty("senseId").GetGuid() == senseId);
        state.GetProperty("reps").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task RecordReview_WrongThenRightAnswerSameSession_OnlyFirstReschedules()
    {
        await using Contexts c = await Contexts.CreateAsync(_fixture);
        string token = await RegisterAsync(c.Identity, "Adult");
        (Guid senseId, string word) = await AddWordAndWaitForStateAsync(c, token);
        (Guid sessionId, _) = await GetSessionAsync(c.Progress, token);

        JsonElement wrong = await VocabularyExercises.AnswerWrongAsync(c.Progress, token, sessionId, senseId, word);
        wrong.GetProperty("isCorrect").GetBoolean().Should().BeFalse();
        wrong.GetProperty("rating").GetString().Should().Be("Again");
        wrong.GetProperty("correctAnswer").GetString().Should().Be(word);

        JsonElement right = await VocabularyExercises.AnswerRightAsync(c.Progress, token, sessionId, senseId, word);
        right.GetProperty("isCorrect").GetBoolean().Should().BeTrue();

        JsonElement state = (await GetWordsAsync(c.Progress, token)).Single(s => s.GetProperty("senseId").GetGuid() == senseId);
        state.GetProperty("reps").GetInt32().Should().Be(1, "only the first due attempt in a session schedules");
    }

    [Fact]
    public async Task RecordReview_ForgedIsCorrectWithWrongAnswer_IsRatedAgain()
    {
        await using Contexts c = await Contexts.CreateAsync(_fixture);
        string token = await RegisterAsync(c.Identity, "Adult");
        (Guid senseId, _) = await AddWordAndWaitForStateAsync(c, token);
        (Guid sessionId, _) = await GetSessionAsync(c.Progress, token);
        JsonElement exercise = await VocabularyExercises.CreateAsync(c.Progress, token, sessionId, senseId);

        // The old contract carried the verdict. A tampered client still sends it; the server must ignore it.
        IAPIResponse response = await c.Progress.PostAsync("/api/progress/vocabulary/reviews", new APIRequestContextOptions
        {
            Headers = Auth(token),
            DataObject = new
            {
                exerciseId = exercise.GetProperty("exerciseId").GetGuid(),
                answer = new { text = "e2e-definitely-wrong" },
                clientResponseMs = 0,
                hintUsed = false,
                isCorrect = true,
                exerciseType = "PictureChoice",
                skill = "Meaning",
                sessionId = Guid.NewGuid(),
                senseId = Guid.NewGuid(),
            },
        });

        response.Status.Should().Be(200, await response.TextAsync());
        JsonElement result = (await response.JsonAsync())!.Value;
        result.GetProperty("isCorrect").GetBoolean().Should().BeFalse();
        result.GetProperty("rating").GetString().Should().Be("Again");
    }

    [Fact]
    public async Task RecordReview_ReplayOfAnsweredExercise_Returns409()
    {
        await using Contexts c = await Contexts.CreateAsync(_fixture);
        string token = await RegisterAsync(c.Identity, "Adult");
        (Guid senseId, string word) = await AddWordAndWaitForStateAsync(c, token);
        (Guid sessionId, _) = await GetSessionAsync(c.Progress, token);
        JsonElement exercise = await VocabularyExercises.CreateAsync(c.Progress, token, sessionId, senseId);
        Guid exerciseId = exercise.GetProperty("exerciseId").GetGuid();

        (await VocabularyExercises.AnswerAsync(c.Progress, token, exerciseId, null, word)).Status.Should().Be(200);
        IAPIResponse replay = await VocabularyExercises.AnswerAsync(c.Progress, token, exerciseId, null, word);

        replay.Status.Should().Be(409, await replay.TextAsync());
    }

    [Fact]
    public async Task CreateExercise_ForeignSessionOrWithoutToken_IsRejected()
    {
        await using Contexts c = await Contexts.CreateAsync(_fixture);
        string token = await RegisterAsync(c.Identity, "Adult");
        string otherToken = await RegisterAsync(c.Identity, "Adult");
        (Guid senseId, _) = await AddWordAndWaitForStateAsync(c, token);
        (Guid sessionId, _) = await GetSessionAsync(c.Progress, token);

        IAPIResponse foreign = await c.Progress.PostAsync("/api/progress/vocabulary/exercises", new APIRequestContextOptions
        {
            Headers = Auth(otherToken),
            DataObject = new { sessionId, senseId },
        });
        IAPIResponse anonymous = await c.Progress.PostAsync("/api/progress/vocabulary/exercises", new APIRequestContextOptions
        {
            DataObject = new { sessionId, senseId },
        });

        foreign.Status.Should().Be(404, await foreign.TextAsync());
        anonymous.Status.Should().Be(401);
    }

    [Fact]
    public async Task RecordReview_UnknownExercise_Returns404()
    {
        await using Contexts c = await Contexts.CreateAsync(_fixture);
        string token = await RegisterAsync(c.Identity, "Adult");

        IAPIResponse response = await VocabularyExercises.AnswerAsync(c.Progress, token, Guid.NewGuid(), "abc", null);
        response.Status.Should().Be(404, await response.TextAsync());
    }

    [Fact]
    public async Task RecordReview_ClientResponseMsOutOfRange_Returns400()
    {
        await using Contexts c = await Contexts.CreateAsync(_fixture);
        string token = await RegisterAsync(c.Identity, "Adult");

        IAPIResponse response = await VocabularyExercises.AnswerAsync(c.Progress, token, Guid.NewGuid(), "abc", null, clientResponseMs: 600_001);
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

    /// <summary>Opens today's session. Returns its id and the full reply.</summary>
    private static async Task<(Guid SessionId, JsonElement Body)> GetSessionAsync(IAPIRequestContext progress, string token)
    {
        IAPIResponse response = await progress.GetAsync("/api/progress/vocabulary/session", Options(token,
            new() { [ClientDateHeader] = DateTimeOffset.Now.ToString("yyyy-MM-ddTHH:mm:sszzz") }));
        response.Status.Should().Be(200, await response.TextAsync());
        JsonElement body = (await response.JsonAsync())!.Value;
        return (body.GetProperty("sessionId").GetGuid(), body);
    }

    private static async Task<List<JsonElement>> GetWordsAsync(IAPIRequestContext progress, string token)
    {
        IAPIResponse response = await progress.GetAsync("/api/progress/vocabulary/words", Options(token));
        response.Status.Should().Be(200, await response.TextAsync());
        return (await response.JsonAsync())!.Value.EnumerateArray().Select(e => e.Clone()).ToList();
    }

    /// <summary>Adds a word in Content and waits for its state to reach Progress (via RabbitMQ).</summary>
    private static async Task<(Guid SenseId, string Word)> AddWordAndWaitForStateAsync(Contexts c, string token)
    {
        string word = $"srs-{Guid.NewGuid():N}";
        IAPIResponse add = await c.Content.PostAsync("/api/vocabulary", new APIRequestContextOptions
        {
            Headers = Auth(token),
            DataObject = new { word, definition = "an e2e definition", example = (string?)null },
        });
        add.Ok.Should().BeTrue($"adding a word should succeed, got {add.Status}: {await add.TextAsync()}");

        DateTime deadline = DateTime.UtcNow + Timeout;
        while (DateTime.UtcNow < deadline)
        {
            List<JsonElement> words = await GetWordsAsync(c.Progress, token);
            if (words.Count == 1)
            {
                words[0].GetProperty("status").GetString().Should().Be("New");
                return (words[0].GetProperty("senseId").GetGuid(), word);
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
