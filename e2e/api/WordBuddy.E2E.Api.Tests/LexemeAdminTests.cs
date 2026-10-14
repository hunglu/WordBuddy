using System.Text.Json;
using FluentAssertions;
using Microsoft.Playwright;

namespace WordBuddy.E2E.Api.Tests;

/// <summary>
/// WB-50: an admin searches, edits and fixes the audio of a lexeme through Content's admin
/// endpoints. A lexeme is created the plain way: a learner adds a word (the lexeme has no part of
/// speech yet). Needs the backend up (Identity + Content). The admin login comes from the
/// Development seed (override with <c>E2E_ADMIN_EMAIL</c>/<c>E2E_ADMIN_PASSWORD</c>).
/// </summary>
[Collection(ApiRequestContextCollection.Name)]
public sealed class LexemeAdminTests
{
    private const string Base = "/api/vocabulary/admin/lexemes";
    private const string Password = "ChangeMe123!";

    private static readonly string AdminEmail =
        Environment.GetEnvironmentVariable("E2E_ADMIN_EMAIL") ?? "admin@wordbuddy.com";

    private static readonly string AdminPassword =
        Environment.GetEnvironmentVariable("E2E_ADMIN_PASSWORD") ?? "Admin@123";

    private readonly ApiRequestContextFixture _fixture;

    public LexemeAdminTests(ApiRequestContextFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task AdminSearchesWithContains_EditsFields_AndPartOfSpeechClashReturns409()
    {
        IAPIRequestContext identity = await _fixture.NewContextAsync(ServiceUrls.Identity);
        IAPIRequestContext content = await _fixture.NewContextAsync(ServiceUrls.Content);
        try
        {
            string adminToken = await LoginAsync(identity, AdminEmail, AdminPassword);
            string learnerToken = await RegisterAsync(identity, "Adult");
            string token = Guid.NewGuid().ToString("N")[..10];
            string word = $"pre{token}post";

            await AddWordAsync(content, learnerToken, word);

            // "Contains": a fragment from the middle finds it. The start of the word finds it too.
            Guid first = await FindLexemeIdAsync(content, adminToken, token);
            (await FindLexemeIdAsync(content, adminToken, $"pre{token}")).Should().Be(first);

            IAPIResponse update = await content.PutAsync($"{Base}/{first}", Options(adminToken, new
            {
                partOfSpeech = "Noun",
                ipaUk = "/e2e-uk/",
                ipaUs = "/e2e-us/",
                syllables = "pre·post",
                wordForms = new[] { "forms" },
                cefrLevel = "B1",
                frequencyRank = 321,
            }));
            update.Status.Should().Be(204, await update.TextAsync());

            JsonElement detail = await GetJsonAsync(content, $"{Base}/{first}", adminToken);
            detail.GetProperty("partOfSpeech").GetString().Should().Be("Noun");
            detail.GetProperty("ipaUk").GetString().Should().Be("/e2e-uk/");
            detail.GetProperty("cefrLevel").GetString().Should().Be("B1");
            detail.GetProperty("frequencyRank").GetInt32().Should().Be(321);
            detail.GetProperty("privateLearnerSenseCount").GetInt32().Should().Be(1);
            detail.GetProperty("senses").GetArrayLength().Should().Be(0, "private learner text is never listed");

            // A second learner adds the same word: a new lexeme without part of speech. Making it a Noun clashes.
            await AddWordAsync(content, await RegisterAsync(identity, "Adult"), word);
            JsonElement page = await GetJsonAsync(content, $"{Base}?search={token}", adminToken);
            Guid second = page.GetProperty("items").EnumerateArray()
                .Select(i => i.GetProperty("id").GetGuid())
                .Single(id => id != first);

            IAPIResponse clash = await content.PutAsync($"{Base}/{second}", Options(adminToken, new { partOfSpeech = "Noun" }));
            clash.Status.Should().Be(409, await clash.TextAsync());
        }
        finally
        {
            await identity.DisposeAsync();
            await content.DisposeAsync();
        }
    }

    [Fact]
    public async Task AdminUploadsAndRemovesAudio_FileIsServedThenGone()
    {
        IAPIRequestContext identity = await _fixture.NewContextAsync(ServiceUrls.Identity);
        IAPIRequestContext content = await _fixture.NewContextAsync(ServiceUrls.Content);
        try
        {
            string adminToken = await LoginAsync(identity, AdminEmail, AdminPassword);
            string learnerToken = await RegisterAsync(identity, "Adult");
            string token = Guid.NewGuid().ToString("N")[..10];
            await AddWordAsync(content, learnerToken, $"audio{token}");
            Guid lexemeId = await FindLexemeIdAsync(content, adminToken, token);

            IFormData form = content.CreateFormData();
            form.Set("file", new FilePayload { Name = "audio.mp3", MimeType = "audio/mpeg", Buffer = [1, 2, 3, 4] });
            IAPIResponse upload = await content.PutAsync(
                $"{Base}/{lexemeId}/audio/en-GB",
                new APIRequestContextOptions
                {
                    Headers = new Dictionary<string, string> { ["Authorization"] = $"Bearer {adminToken}" },
                    Multipart = form,
                });
            upload.Status.Should().Be(200, await upload.TextAsync());
            Guid assetId = (await upload.JsonAsync())!.Value.GetProperty("id").GetGuid();

            (await content.GetAsync($"/api/media/{assetId}")).Status.Should().Be(200);
            (await GetJsonAsync(content, $"{Base}/{lexemeId}", adminToken)).GetProperty("hasUkAudio").GetBoolean().Should().BeTrue();

            (await content.DeleteAsync($"{Base}/{lexemeId}/audio/en-GB", Options(adminToken))).Status.Should().Be(204);

            (await GetJsonAsync(content, $"{Base}/{lexemeId}", adminToken)).GetProperty("hasUkAudio").GetBoolean().Should().BeFalse();
            (await content.GetAsync($"/api/media/{assetId}")).Status.Should().Be(404, "the unused asset row is deleted with the file");
            // Removing again is a no-op.
            (await content.DeleteAsync($"{Base}/{lexemeId}/audio/en-GB", Options(adminToken))).Status.Should().Be(204);
        }
        finally
        {
            await identity.DisposeAsync();
            await content.DisposeAsync();
        }
    }

    [Fact]
    public async Task LexemeAdminEndpoints_LearnerCaller_ReturnsForbidden()
    {
        IAPIRequestContext identity = await _fixture.NewContextAsync(ServiceUrls.Identity);
        IAPIRequestContext content = await _fixture.NewContextAsync(ServiceUrls.Content);
        try
        {
            string adultToken = await RegisterAsync(identity, "Adult");

            (await content.GetAsync(Base, Options(adultToken))).Status.Should().Be(403);
            (await content.GetAsync($"{Base}/{Guid.NewGuid()}", Options(adultToken))).Status.Should().Be(403);
            (await content.PutAsync($"{Base}/{Guid.NewGuid()}", Options(adultToken, new { partOfSpeech = "Noun" }))).Status.Should().Be(403);
            (await content.DeleteAsync($"{Base}/{Guid.NewGuid()}/audio/en-GB", Options(adultToken))).Status.Should().Be(403);
        }
        finally
        {
            await identity.DisposeAsync();
            await content.DisposeAsync();
        }
    }

    private static async Task AddWordAsync(IAPIRequestContext content, string token, string word)
    {
        IAPIResponse add = await content.PostAsync("/api/vocabulary", Options(token, new
        {
            word,
            definition = $"A word made up by the lexeme admin E2E test {Guid.NewGuid():N}.",
            example = (string?)null,
        }));
        add.Ok.Should().BeTrue($"adding a word should succeed, got {add.Status}: {await add.TextAsync()}");
    }

    private static async Task<Guid> FindLexemeIdAsync(IAPIRequestContext content, string adminToken, string search)
    {
        JsonElement page = await GetJsonAsync(content, $"{Base}?search={search}", adminToken);
        return page.GetProperty("items").EnumerateArray().First().GetProperty("id").GetGuid();
    }

    private static async Task<JsonElement> GetJsonAsync(IAPIRequestContext content, string url, string token)
    {
        IAPIResponse response = await content.GetAsync(url, Options(token));
        response.Ok.Should().BeTrue($"GET {url} should succeed, got {response.Status}: {await response.TextAsync()}");
        return (await response.JsonAsync())!.Value;
    }

    private static async Task<string> RegisterAsync(IAPIRequestContext identity, string ageGroup)
    {
        IAPIResponse response = await identity.PostAsync("/api/auth/register", new APIRequestContextOptions
        {
            DataObject = new
            {
                email = $"lexeme-e2e-{Guid.NewGuid():N}@example.com",
                password = Password,
                displayName = "E2E Lexeme User",
                ageGroup,
            },
        });
        response.Ok.Should().BeTrue($"self-registration should succeed, got {response.Status}: {await response.TextAsync()}");
        return (await response.JsonAsync())!.Value.GetProperty("token").GetString()!;
    }

    private static async Task<string> LoginAsync(IAPIRequestContext identity, string email, string password)
    {
        IAPIResponse response = await identity.PostAsync("/api/auth/login", new APIRequestContextOptions
        {
            DataObject = new { email, password },
        });
        response.Ok.Should().BeTrue($"admin login should succeed (Development seed), got {response.Status}: {await response.TextAsync()}");
        return (await response.JsonAsync())!.Value.GetProperty("token").GetString()!;
    }

    private static APIRequestContextOptions Options(string token, object? body = null) => new()
    {
        Headers = new Dictionary<string, string> { ["Authorization"] = $"Bearer {token}" },
        DataObject = body,
    };
}
