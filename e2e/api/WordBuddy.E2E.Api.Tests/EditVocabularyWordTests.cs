using System.Text.Json;
using FluentAssertions;
using Microsoft.Playwright;

namespace WordBuddy.E2E.Api.Tests;

/// <summary>
/// WB-48: a learner edits their own private word (<c>PUT /api/vocabulary/{id}</c>). The sense id stays,
/// so the Progress history stays valid and a review by that id still works, with the new text.
/// Blackbox HTTP only. Word states arrive through RabbitMQ, so the full stack must run.
/// </summary>
[Collection(ApiRequestContextCollection.Name)]
public sealed class EditVocabularyWordTests
{
    private const string ClientDateHeader = "X-Client-CurrentDateTime";
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    private readonly ApiRequestContextFixture _fixture;

    public EditVocabularyWordTests(ApiRequestContextFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task EditWord_ThenReviewBySameSenseId_UsesNewText()
    {
        IAPIRequestContext identity = await _fixture.NewContextAsync(ServiceUrls.Identity);
        IAPIRequestContext content = await _fixture.NewContextAsync(ServiceUrls.Content);
        IAPIRequestContext progress = await _fixture.NewContextAsync(ServiceUrls.Progress);
        try
        {
            string token = await RegisterAsync(identity);
            string typo = $"edt{Guid.NewGuid():N}";
            string fixedWord = $"{typo}x";

            IAPIResponse add = await content.PostAsync("/api/vocabulary", new APIRequestContextOptions
            {
                Headers = Auth(token),
                DataObject = new { word = typo, definition = "an e2e definition", example = (string?)null },
            });
            add.Ok.Should().BeTrue(await add.TextAsync());
            Guid senseId = (await add.JsonAsync())!.Value.GetGuid();
            await WaitForStateAsync(progress, token, senseId);

            IAPIResponse edit = await content.PutAsync($"/api/vocabulary/{senseId}", new APIRequestContextOptions
            {
                Headers = Auth(token),
                DataObject = new { word = fixedWord, definition = "an edited e2e definition", example = (string?)null },
            });
            edit.Status.Should().Be(204, await edit.TextAsync());

            IAPIResponse mine = await content.GetAsync("/api/vocabulary/mine", new APIRequestContextOptions { Headers = Auth(token) });
            JsonElement edited = (await mine.JsonAsync())!.Value.EnumerateArray()
                .Single(w => w.GetProperty("id").GetGuid() == senseId);
            edited.GetProperty("word").GetString().Should().Be(fixedWord);

            IAPIResponse session = await progress.GetAsync("/api/progress/vocabulary/session", new APIRequestContextOptions
            {
                Headers = new Dictionary<string, string>(Auth(token))
                {
                    [ClientDateHeader] = DateTimeOffset.Now.ToString("yyyy-MM-ddTHH:mm:sszzz"),
                },
            });
            session.Status.Should().Be(200, await session.TextAsync());
            Guid sessionId = (await session.JsonAsync())!.Value.GetProperty("sessionId").GetGuid();

            JsonElement result = await VocabularyExercises.AnswerRightAsync(progress, token, sessionId, senseId, fixedWord);
            result.GetProperty("isCorrect").GetBoolean().Should().BeTrue();
            result.GetProperty("correctAnswer").GetString().Should().Be(fixedWord);
        }
        finally
        {
            await identity.DisposeAsync();
            await content.DisposeAsync();
            await progress.DisposeAsync();
        }
    }

    [Fact]
    public async Task EditWord_OtherUsersWord_Returns404()
    {
        IAPIRequestContext identity = await _fixture.NewContextAsync(ServiceUrls.Identity);
        IAPIRequestContext content = await _fixture.NewContextAsync(ServiceUrls.Content);
        try
        {
            string owner = await RegisterAsync(identity);
            string stranger = await RegisterAsync(identity);
            IAPIResponse add = await content.PostAsync("/api/vocabulary", new APIRequestContextOptions
            {
                Headers = Auth(owner),
                DataObject = new { word = $"edt{Guid.NewGuid():N}", definition = "def", example = (string?)null },
            });
            Guid senseId = (await add.JsonAsync())!.Value.GetGuid();

            IAPIResponse edit = await content.PutAsync($"/api/vocabulary/{senseId}", new APIRequestContextOptions
            {
                Headers = Auth(stranger),
                DataObject = new { word = "hijack", definition = "def", example = (string?)null },
            });

            edit.Status.Should().Be(404);
        }
        finally
        {
            await identity.DisposeAsync();
            await content.DisposeAsync();
        }
    }

    private static async Task WaitForStateAsync(IAPIRequestContext progress, string token, Guid senseId)
    {
        DateTime deadline = DateTime.UtcNow + Timeout;
        while (DateTime.UtcNow < deadline)
        {
            IAPIResponse response = await progress.GetAsync("/api/progress/vocabulary/words", new APIRequestContextOptions { Headers = Auth(token) });
            response.Status.Should().Be(200, await response.TextAsync());
            if ((await response.JsonAsync())!.Value.EnumerateArray().Any(s => s.GetProperty("senseId").GetGuid() == senseId))
            {
                return;
            }

            await Task.Delay(500);
        }

        throw new TimeoutException($"No word state reached Progress within {Timeout.TotalSeconds}s.");
    }

    private static async Task<string> RegisterAsync(IAPIRequestContext identity)
    {
        IAPIResponse response = await identity.PostAsync("/api/auth/register", new APIRequestContextOptions
        {
            DataObject = new
            {
                email = $"edit-e2e-{Guid.NewGuid():N}@example.com",
                password = "ChangeMe123!",
                displayName = "E2E Edit Learner",
                ageGroup = "Adult",
            },
        });
        response.Ok.Should().BeTrue($"self-registration should succeed, got {response.Status}: {await response.TextAsync()}");
        return (await response.JsonAsync())!.Value.GetProperty("token").GetString()!;
    }

    private static Dictionary<string, string> Auth(string token) => new() { ["Authorization"] = $"Bearer {token}" };
}
