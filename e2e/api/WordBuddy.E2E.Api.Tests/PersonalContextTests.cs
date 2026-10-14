using System.Text.Json;
using FluentAssertions;
using Microsoft.Playwright;

namespace WordBuddy.E2E.Api.Tests;

/// <summary>
/// WB-49: the private note on a word. A learner adds a word with a note, then edits and clears it
/// (<c>PUT /api/vocabulary/{id}/personal-context</c>). The Progress review exercise carries the note, and
/// another learner cannot change it. Blackbox HTTP only. Word states arrive through RabbitMQ, so the full stack must run.
/// </summary>
[Collection(ApiRequestContextCollection.Name)]
public sealed class PersonalContextTests
{
    private const string ClientDateHeader = "X-Client-CurrentDateTime";
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    private readonly ApiRequestContextFixture _fixture;

    public PersonalContextTests(ApiRequestContextFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task AddWordWithNote_ReviewExerciseCarriesNote_ThenEditAndClear()
    {
        IAPIRequestContext identity = await _fixture.NewContextAsync(ServiceUrls.Identity);
        IAPIRequestContext content = await _fixture.NewContextAsync(ServiceUrls.Content);
        IAPIRequestContext progress = await _fixture.NewContextAsync(ServiceUrls.Progress);
        try
        {
            string token = await RegisterAsync(identity);
            string word = $"ctx{Guid.NewGuid():N}";

            IAPIResponse add = await content.PostAsync("/api/vocabulary", new APIRequestContextOptions
            {
                Headers = Auth(token),
                DataObject = new { word, definition = "an e2e definition", example = (string?)null, personalContext = "met at the zoo" },
            });
            add.Ok.Should().BeTrue(await add.TextAsync());
            Guid senseId = (await add.JsonAsync())!.Value.GetGuid();
            await WaitForStateAsync(progress, token, senseId);

            (await ExerciseNoteAsync(progress, token, senseId)).Should().Be("met at the zoo");

            IAPIResponse edit = await content.PutAsync($"/api/vocabulary/{senseId}/personal-context", new APIRequestContextOptions
            {
                Headers = Auth(token),
                DataObject = new { personalContext = "met at school" },
            });
            edit.Status.Should().Be(204, await edit.TextAsync());
            (await MineNoteAsync(content, token, senseId)).Should().Be("met at school");

            IAPIResponse clear = await content.PutAsync($"/api/vocabulary/{senseId}/personal-context", new APIRequestContextOptions
            {
                Headers = Auth(token),
                DataObject = new { personalContext = (string?)null },
            });
            clear.Status.Should().Be(204, await clear.TextAsync());
            (await MineNoteAsync(content, token, senseId)).Should().BeNull();
        }
        finally
        {
            await identity.DisposeAsync();
            await content.DisposeAsync();
            await progress.DisposeAsync();
        }
    }

    [Fact]
    public async Task EditNote_OtherUsersWord_Returns404()
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
                DataObject = new { word = $"ctx{Guid.NewGuid():N}", definition = "def", example = (string?)null, personalContext = "private" },
            });
            Guid senseId = (await add.JsonAsync())!.Value.GetGuid();

            IAPIResponse edit = await content.PutAsync($"/api/vocabulary/{senseId}/personal-context", new APIRequestContextOptions
            {
                Headers = Auth(stranger),
                DataObject = new { personalContext = "hijack" },
            });

            edit.Status.Should().Be(404);
        }
        finally
        {
            await identity.DisposeAsync();
            await content.DisposeAsync();
        }
    }

    private static async Task<string?> MineNoteAsync(IAPIRequestContext content, string token, Guid senseId)
    {
        IAPIResponse mine = await content.GetAsync("/api/vocabulary/mine", new APIRequestContextOptions { Headers = Auth(token) });
        JsonElement item = (await mine.JsonAsync())!.Value.EnumerateArray().Single(w => w.GetProperty("id").GetGuid() == senseId);
        return item.TryGetProperty("personalContext", out JsonElement note) && note.ValueKind == JsonValueKind.String ? note.GetString() : null;
    }

    private static async Task<string?> ExerciseNoteAsync(IAPIRequestContext progress, string token, Guid senseId)
    {
        IAPIResponse session = await progress.GetAsync("/api/progress/vocabulary/session", new APIRequestContextOptions
        {
            Headers = new Dictionary<string, string>(Auth(token))
            {
                [ClientDateHeader] = DateTimeOffset.Now.ToString("yyyy-MM-ddTHH:mm:sszzz"),
            },
        });
        session.Status.Should().Be(200, await session.TextAsync());
        Guid sessionId = (await session.JsonAsync())!.Value.GetProperty("sessionId").GetGuid();

        JsonElement exercise = await VocabularyExercises.CreateAsync(progress, token, sessionId, senseId);
        return exercise.GetProperty("prompt").TryGetProperty("personalContext", out JsonElement note) && note.ValueKind == JsonValueKind.String ? note.GetString() : null;
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
                email = $"ctx-e2e-{Guid.NewGuid():N}@example.com",
                password = "ChangeMe123!",
                displayName = "E2E Note Learner",
                ageGroup = "Adult",
            },
        });
        response.Ok.Should().BeTrue($"self-registration should succeed, got {response.Status}: {await response.TextAsync()}");
        return (await response.JsonAsync())!.Value.GetProperty("token").GetString()!;
    }

    private static Dictionary<string, string> Auth(string token) => new() { ["Authorization"] = $"Bearer {token}" };
}
