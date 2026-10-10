using System.Text.Json;
using FluentAssertions;
using Microsoft.Playwright;

namespace WordBuddy.E2E.Api.Tests;

/// <summary>
/// WB-23: a full review session the way the UI runs it.
/// Progress session → Content <c>senses?ids=</c> → one review per item → word states change.
/// Word states arrive through RabbitMQ, so the full stack must run.
/// </summary>
[Collection(ApiRequestContextCollection.Name)]
public sealed class VocabularyReviewSessionTests
{
    private const string ClientDateHeader = "X-Client-CurrentDateTime";
    private const int WordCount = 4;
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    private readonly ApiRequestContextFixture _fixture;

    public VocabularyReviewSessionTests(ApiRequestContextFixture fixture)
    {
        _fixture = fixture;
    }

    [Theory]
    [InlineData("Adult")]
    [InlineData("Child")]
    public async Task ReviewSession_OneReviewPerItem_AllWordStatesLeaveNew(string ageGroup)
    {
        await using Contexts c = await Contexts.CreateAsync(_fixture);
        string token = await RegisterAsync(c.Identity, ageGroup);
        Dictionary<string, string> definitionsByWord = await AddWordsAsync(c.Content, token);
        await WaitForStatesAsync(c.Progress, token, WordCount);

        // 1. Session from Progress.
        IAPIResponse sessionResponse = await c.Progress.GetAsync("/api/progress/vocabulary/session", Options(token,
            new() { [ClientDateHeader] = DateTimeOffset.Now.ToString("yyyy-MM-ddTHH:mm:sszzz") }));
        sessionResponse.Status.Should().Be(200, await sessionResponse.TextAsync());
        JsonElement session = (await sessionResponse.JsonAsync())!.Value;
        Guid sessionId = session.GetProperty("sessionId").GetGuid();
        List<Guid> senseIds = session.GetProperty("dueItems").EnumerateArray()
            .Concat(session.GetProperty("newItems").EnumerateArray())
            .Select(i => i.GetProperty("senseId").GetGuid())
            .ToList();
        senseIds.Should().HaveCount(WordCount);

        // 2. Senses from Content: the learner's own words are visible to both age groups.
        string query = string.Join("&", senseIds.Select(id => $"ids={id}"));
        IAPIResponse sensesResponse = await c.Content.GetAsync($"/api/vocabulary/senses?{query}", Options(token));
        sensesResponse.Status.Should().Be(200, await sensesResponse.TextAsync());
        List<JsonElement> senses = (await sensesResponse.JsonAsync())!.Value.EnumerateArray().Select(e => e.Clone()).ToList();
        senses.Select(s => s.GetProperty("senseId").GetGuid()).Should().BeEquivalentTo(senseIds);
        foreach (JsonElement sense in senses)
        {
            string word = sense.GetProperty("word").GetString()!;
            definitionsByWord.Should().ContainKey(word);
            sense.GetProperty("definition").GetString().Should().Be(definitionsByWord[word]);
        }

        // 3. One exercise and one right answer per item (WB-28: the server builds and checks them).
        foreach (Guid senseId in senseIds)
        {
            string word = senses.Single(s => s.GetProperty("senseId").GetGuid() == senseId).GetProperty("word").GetString()!;

            JsonElement exercise = await VocabularyExercises.CreateAsync(c.Progress, token, sessionId, senseId);
            string raw = exercise.GetRawText();
            raw.Should().NotContain(senseId.ToString(), "options carry opaque keys, not sense ids");
            exercise.TryGetProperty("expectedAnswer", out _).Should().BeFalse("the reply never carries the answer");

            (string? optionKey, string? text) = VocabularyExercises.RightAnswer(exercise, word);
            IAPIResponse review = await VocabularyExercises.AnswerAsync(
                c.Progress, token, exercise.GetProperty("exerciseId").GetGuid(), optionKey, text);
            review.Status.Should().Be(200, await review.TextAsync());
            JsonElement result = (await review.JsonAsync())!.Value;
            result.GetProperty("status").GetString().Should().NotBe("New");
            result.GetProperty("isCorrect").GetBoolean().Should().BeTrue();
            result.GetProperty("correctAnswer").GetString().Should().Be(word);
        }

        // 4. States changed.
        List<JsonElement> states = await GetWordsAsync(c.Progress, token);
        states.Should().HaveCount(WordCount);
        states.Should().OnlyContain(s => s.GetProperty("status").GetString() != "New" && s.GetProperty("reps").GetInt32() == 1);
    }

    private static async Task<Dictionary<string, string>> AddWordsAsync(IAPIRequestContext content, string token)
    {
        Dictionary<string, string> definitionsByWord = new();
        for (int i = 0; i < WordCount; i++)
        {
            string word = $"review-{i}-{Guid.NewGuid():N}";
            string definition = $"e2e review definition {i}";
            IAPIResponse add = await content.PostAsync("/api/vocabulary", new APIRequestContextOptions
            {
                Headers = Auth(token),
                DataObject = new { word, definition, example = (string?)null },
            });
            add.Ok.Should().BeTrue($"adding a word should succeed, got {add.Status}: {await add.TextAsync()}");
            definitionsByWord[word] = definition;
        }

        return definitionsByWord;
    }

    private static async Task WaitForStatesAsync(IAPIRequestContext progress, string token, int expected)
    {
        DateTime deadline = DateTime.UtcNow + Timeout;
        while (DateTime.UtcNow < deadline)
        {
            if ((await GetWordsAsync(progress, token)).Count == expected)
            {
                return;
            }

            await Task.Delay(500);
        }

        throw new TimeoutException($"{expected} word states did not reach Progress within {Timeout.TotalSeconds}s.");
    }

    private static async Task<List<JsonElement>> GetWordsAsync(IAPIRequestContext progress, string token)
    {
        IAPIResponse response = await progress.GetAsync("/api/progress/vocabulary/words", Options(token));
        response.Status.Should().Be(200, await response.TextAsync());
        return (await response.JsonAsync())!.Value.EnumerateArray().Select(e => e.Clone()).ToList();
    }

    private static async Task<string> RegisterAsync(IAPIRequestContext identity, string ageGroup)
    {
        IAPIResponse response = await identity.PostAsync("/api/auth/register", new APIRequestContextOptions
        {
            DataObject = new
            {
                email = $"review-e2e-{Guid.NewGuid():N}@example.com",
                password = "ChangeMe123!",
                displayName = "E2E Review Learner",
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
