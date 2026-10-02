using System.Text.Json;
using FluentAssertions;
using Microsoft.Playwright;

namespace WordBuddy.E2E.Api.Tests;

/// <summary>
/// Blackbox coverage of the personal vocabulary builder feature from an authenticated caller's
/// perspective, spanning Identity (to obtain real JWTs) and Content (the vocabulary endpoints
/// themselves): add a word, retrieve it, request it be shared, and — as an admin — moderate it
/// into the shared pool. Requires the backend to be up (e.g. <c>docker compose up</c> from
/// <c>WordBuddy/</c>).
///
/// Tokens are obtained the same way a real client would — Identity's own <c>/api/auth</c>
/// endpoints — rather than a test-only JWT factory, since this project has no project reference
/// to any service and must stay a pure HTTP blackbox:
/// - A fresh learner account is self-registered per test run (unique email, <c>Adult</c>
///   age group) via <c>POST /api/auth/register</c>, avoiding any dependency on pre-seeded
///   non-admin fixtures.
/// - The admin token comes from logging in as the <c>admin@wordbuddy.com</c> account that
///   Identity's <c>DataSeeder</c> creates automatically in Development
///   (<c>WordBuddy.Identity.Infrastructure/Seeding/DataSeeder.cs</c>) — override via
///   <c>E2E_ADMIN_EMAIL</c>/<c>E2E_ADMIN_PASSWORD</c> if a different environment uses different
///   admin credentials.
/// </summary>
[Collection(ApiRequestContextCollection.Name)]
public sealed class PersonalVocabularyTests
{
    private static readonly string AdminEmail =
        Environment.GetEnvironmentVariable("E2E_ADMIN_EMAIL") ?? "admin@wordbuddy.com";

    private static readonly string AdminPassword =
        Environment.GetEnvironmentVariable("E2E_ADMIN_PASSWORD") ?? "Admin@123";

    private readonly ApiRequestContextFixture _fixture;

    public PersonalVocabularyTests(ApiRequestContextFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task AddRetrieveShareAndModerate_WordAppearsInSharedPool()
    {
        IAPIRequestContext identity = await _fixture.NewContextAsync(ServiceUrls.Identity);
        IAPIRequestContext content = await _fixture.NewContextAsync(ServiceUrls.Content);
        try
        {
            string learnerToken = await RegisterLearnerAsync(identity, ageGroup: "Adult");
            string adminToken = await LoginAsync(identity, AdminEmail, AdminPassword);

            // Add a word to the learner's own list. Unique text per run — with dedupe-on-add, a fixed
            // text would link to the word an earlier run already shared, and sharing it again would 409.
            string word = $"ubiquitous-{Guid.NewGuid():N}";
            IAPIResponse addResponse = await content.PostAsync("/api/vocabulary", new APIRequestContextOptions
            {
                Headers = AuthHeader(learnerToken),
                DataObject = new { word, definition = "present everywhere", example = "Smartphones are ubiquitous." },
            });
            addResponse.Ok.Should().BeTrue($"adding a word should succeed, got {addResponse.Status}: {await addResponse.TextAsync()}");
            JsonElement wordIdJson = (await addResponse.JsonAsync())!.Value;
            Guid wordId = wordIdJson.GetGuid();

            // Retrieve it back via the learner's own list.
            IAPIResponse mineResponse = await content.GetAsync("/api/vocabulary/mine", new APIRequestContextOptions
            {
                Headers = AuthHeader(learnerToken),
            });
            mineResponse.Ok.Should().BeTrue();
            JsonElement mine = (await mineResponse.JsonAsync())!.Value;
            mine.EnumerateArray().Should().Contain(w =>
                w.GetProperty("id").GetGuid() == wordId
                && w.GetProperty("shareStatus").GetString() == "Private");

            // Request it be shared into the community pool.
            IAPIResponse shareResponse = await content.PostAsync($"/api/vocabulary/{wordId}/share", new APIRequestContextOptions
            {
                Headers = AuthHeader(learnerToken),
            });
            shareResponse.Status.Should().Be(204, $"sharing should succeed, got {shareResponse.Status}: {await shareResponse.TextAsync()}");

            // Moderate (approve, visible to children) as an admin.
            IAPIResponse moderateResponse = await content.PostAsync($"/api/vocabulary/moderation/{wordId}", new APIRequestContextOptions
            {
                Headers = AuthHeader(adminToken),
                DataObject = new { approve = true, visibleToChildren = true },
            });
            moderateResponse.Status.Should().Be(204, $"moderation should succeed, got {moderateResponse.Status}: {await moderateResponse.TextAsync()}");

            // The word now appears in the shared pool.
            IAPIResponse sharedResponse = await content.GetAsync("/api/vocabulary/shared", new APIRequestContextOptions
            {
                Headers = AuthHeader(learnerToken),
            });
            sharedResponse.Ok.Should().BeTrue();
            JsonElement shared = (await sharedResponse.JsonAsync())!.Value;
            shared.EnumerateArray().Should().Contain(w =>
                w.GetProperty("id").GetGuid() == wordId
                && w.GetProperty("shareStatus").GetString() == "Shared"
                && w.GetProperty("visibleToChildren").GetBoolean());
        }
        finally
        {
            await identity.DisposeAsync();
            await content.DisposeAsync();
        }
    }

    [Fact]
    public async Task ModerationEndpoint_NonAdminCaller_ReturnsForbidden()
    {
        IAPIRequestContext identity = await _fixture.NewContextAsync(ServiceUrls.Identity);
        IAPIRequestContext content = await _fixture.NewContextAsync(ServiceUrls.Content);
        try
        {
            string learnerToken = await RegisterLearnerAsync(identity, ageGroup: "Adult");

            IAPIResponse response = await content.GetAsync("/api/vocabulary/moderation/pending", new APIRequestContextOptions
            {
                Headers = AuthHeader(learnerToken),
            });

            response.Status.Should().Be(403);
        }
        finally
        {
            await identity.DisposeAsync();
            await content.DisposeAsync();
        }
    }

    [Fact]
    public async Task RequestShare_ChildCaller_ReturnsForbidden()
    {
        IAPIRequestContext identity = await _fixture.NewContextAsync(ServiceUrls.Identity);
        IAPIRequestContext content = await _fixture.NewContextAsync(ServiceUrls.Content);
        try
        {
            string childToken = await RegisterLearnerAsync(identity, ageGroup: "Child");

            IAPIResponse addResponse = await content.PostAsync("/api/vocabulary", new APIRequestContextOptions
            {
                Headers = AuthHeader(childToken),
                DataObject = new { word = "curious", definition = "eager to learn", example = (string?)null },
            });
            addResponse.Ok.Should().BeTrue();
            Guid wordId = (await addResponse.JsonAsync())!.Value.GetGuid();

            IAPIResponse shareResponse = await content.PostAsync($"/api/vocabulary/{wordId}/share", new APIRequestContextOptions
            {
                Headers = AuthHeader(childToken),
            });

            shareResponse.Status.Should().Be(403, "child accounts cannot initiate sharing (CanShareVocabulary policy)");
        }
        finally
        {
            await identity.DisposeAsync();
            await content.DisposeAsync();
        }
    }

    [Fact]
    public async Task AdoptSharedWordThenDelete_WordStaysInSharedPool()
    {
        IAPIRequestContext identity = await _fixture.NewContextAsync(ServiceUrls.Identity);
        IAPIRequestContext content = await _fixture.NewContextAsync(ServiceUrls.Content);
        try
        {
            string authorToken = await RegisterLearnerAsync(identity, ageGroup: "Adult");
            string adopterToken = await RegisterLearnerAsync(identity, ageGroup: "Adult");
            string adminToken = await LoginAsync(identity, AdminEmail, AdminPassword);

            // Unique text per run — identical text would dedupe onto an earlier run's word.
            string word = $"serendipity-{Guid.NewGuid():N}";
            IAPIResponse addResponse = await content.PostAsync("/api/vocabulary", new APIRequestContextOptions
            {
                Headers = AuthHeader(authorToken),
                DataObject = new { word, definition = "a happy accident", example = (string?)null },
            });
            addResponse.Ok.Should().BeTrue();
            Guid wordId = (await addResponse.JsonAsync())!.Value.GetGuid();

            (await content.PostAsync($"/api/vocabulary/{wordId}/share", new APIRequestContextOptions { Headers = AuthHeader(authorToken) }))
                .Status.Should().Be(204);
            (await content.PostAsync($"/api/vocabulary/moderation/{wordId}", new APIRequestContextOptions
            {
                Headers = AuthHeader(adminToken),
                DataObject = new { approve = true, visibleToChildren = true },
            })).Status.Should().Be(204);

            // Adopt: the returned id is the shared word's own id (a link, not a copy).
            IAPIResponse adoptResponse = await content.PostAsync($"/api/vocabulary/shared/{wordId}/add-to-mine", new APIRequestContextOptions
            {
                Headers = AuthHeader(adopterToken),
            });
            adoptResponse.Status.Should().Be(201, $"adopting should succeed, got {adoptResponse.Status}: {await adoptResponse.TextAsync()}");
            (await adoptResponse.JsonAsync())!.Value.GetGuid().Should().Be(wordId);

            JsonElement mine = (await (await content.GetAsync("/api/vocabulary/mine", new APIRequestContextOptions
            {
                Headers = AuthHeader(adopterToken),
            })).JsonAsync())!.Value;
            mine.EnumerateArray().Should().Contain(w =>
                w.GetProperty("id").GetGuid() == wordId && !w.GetProperty("isAuthor").GetBoolean());

            // Delete from My words: gone from the adopter's list, still in the shared pool.
            (await content.DeleteAsync($"/api/vocabulary/{wordId}", new APIRequestContextOptions { Headers = AuthHeader(adopterToken) }))
                .Status.Should().Be(204);

            JsonElement mineAfter = (await (await content.GetAsync("/api/vocabulary/mine", new APIRequestContextOptions
            {
                Headers = AuthHeader(adopterToken),
            })).JsonAsync())!.Value;
            mineAfter.EnumerateArray().Should().NotContain(w => w.GetProperty("id").GetGuid() == wordId);

            JsonElement shared = (await (await content.GetAsync("/api/vocabulary/shared", new APIRequestContextOptions
            {
                Headers = AuthHeader(adopterToken),
            })).JsonAsync())!.Value;
            shared.EnumerateArray().Should().Contain(w => w.GetProperty("id").GetGuid() == wordId);
        }
        finally
        {
            await identity.DisposeAsync();
            await content.DisposeAsync();
        }
    }

    [Fact]
    public async Task AuthorDeleteOfSharedWord_NeedsConfirmThenHandsWordOverToWordBuddy()
    {
        IAPIRequestContext identity = await _fixture.NewContextAsync(ServiceUrls.Identity);
        IAPIRequestContext content = await _fixture.NewContextAsync(ServiceUrls.Content);
        try
        {
            string authorToken = await RegisterLearnerAsync(identity, ageGroup: "Adult");
            string adopterToken = await RegisterLearnerAsync(identity, ageGroup: "Adult");
            string adminToken = await LoginAsync(identity, AdminEmail, AdminPassword);

            Guid wordId = await AddAndShareAsync(content, authorToken, $"handover-{Guid.NewGuid():N}");
            (await content.PostAsync($"/api/vocabulary/moderation/{wordId}", new APIRequestContextOptions
            {
                Headers = AuthHeader(adminToken),
                DataObject = new { approve = true, visibleToChildren = true },
            })).Status.Should().Be(204);
            (await content.PostAsync($"/api/vocabulary/shared/{wordId}/add-to-mine", new APIRequestContextOptions { Headers = AuthHeader(adopterToken) }))
                .Status.Should().Be(201);

            JsonElement poolBefore = await GetJsonAsync(content, "/api/vocabulary/shared", authorToken);
            poolBefore.EnumerateArray().Should().Contain(w => w.GetProperty("id").GetGuid() == wordId && w.GetProperty("isMine").GetBoolean());

            // Without confirm: 409, word untouched.
            IAPIResponse unconfirmed = await content.DeleteAsync($"/api/vocabulary/{wordId}", new APIRequestContextOptions { Headers = AuthHeader(authorToken) });
            unconfirmed.Status.Should().Be(409);
            (await unconfirmed.TextAsync()).Should().Contain("PersonalVocabularyWord.DeleteConfirmationRequired");

            // With confirm: 204, gone from the author's list, still in the pool but no longer theirs.
            (await content.DeleteAsync($"/api/vocabulary/{wordId}?confirm=true", new APIRequestContextOptions { Headers = AuthHeader(authorToken) }))
                .Status.Should().Be(204);

            (await GetJsonAsync(content, "/api/vocabulary/mine", authorToken)).EnumerateArray()
                .Should().NotContain(w => w.GetProperty("id").GetGuid() == wordId);
            (await GetJsonAsync(content, "/api/vocabulary/shared", authorToken)).EnumerateArray()
                .Should().Contain(w => w.GetProperty("id").GetGuid() == wordId && !w.GetProperty("isMine").GetBoolean());
            (await GetJsonAsync(content, "/api/vocabulary/mine", adopterToken)).EnumerateArray()
                .Should().Contain(w => w.GetProperty("id").GetGuid() == wordId);

            // The former owner can re-add it, as an adopter.
            (await content.PostAsync($"/api/vocabulary/shared/{wordId}/add-to-mine", new APIRequestContextOptions { Headers = AuthHeader(authorToken) }))
                .Status.Should().Be(201);
            (await GetJsonAsync(content, "/api/vocabulary/mine", authorToken)).EnumerateArray()
                .Should().Contain(w => w.GetProperty("id").GetGuid() == wordId && !w.GetProperty("isAuthor").GetBoolean());
        }
        finally
        {
            await identity.DisposeAsync();
            await content.DisposeAsync();
        }
    }

    [Fact]
    public async Task AuthorDeleteOfPendingWord_NeedsConfirmThenCancelsShareRequest()
    {
        IAPIRequestContext identity = await _fixture.NewContextAsync(ServiceUrls.Identity);
        IAPIRequestContext content = await _fixture.NewContextAsync(ServiceUrls.Content);
        try
        {
            string authorToken = await RegisterLearnerAsync(identity, ageGroup: "Adult");
            string adminToken = await LoginAsync(identity, AdminEmail, AdminPassword);

            Guid wordId = await AddAndShareAsync(content, authorToken, $"pending-{Guid.NewGuid():N}");

            (await content.DeleteAsync($"/api/vocabulary/{wordId}", new APIRequestContextOptions { Headers = AuthHeader(authorToken) }))
                .Status.Should().Be(409);
            (await content.DeleteAsync($"/api/vocabulary/{wordId}?confirm=true", new APIRequestContextOptions { Headers = AuthHeader(authorToken) }))
                .Status.Should().Be(204);

            (await GetJsonAsync(content, "/api/vocabulary/moderation/pending", adminToken)).EnumerateArray()
                .Should().NotContain(w => w.GetProperty("id").GetGuid() == wordId);
            (await GetJsonAsync(content, "/api/vocabulary/mine", authorToken)).EnumerateArray()
                .Should().NotContain(w => w.GetProperty("id").GetGuid() == wordId);
        }
        finally
        {
            await identity.DisposeAsync();
            await content.DisposeAsync();
        }
    }

    private static async Task<Guid> AddAndShareAsync(IAPIRequestContext content, string token, string word)
    {
        IAPIResponse addResponse = await content.PostAsync("/api/vocabulary", new APIRequestContextOptions
        {
            Headers = AuthHeader(token),
            DataObject = new { word, definition = "an e2e definition", example = (string?)null },
        });
        addResponse.Ok.Should().BeTrue($"adding a word should succeed, got {addResponse.Status}: {await addResponse.TextAsync()}");
        Guid wordId = (await addResponse.JsonAsync())!.Value.GetGuid();

        (await content.PostAsync($"/api/vocabulary/{wordId}/share", new APIRequestContextOptions { Headers = AuthHeader(token) }))
            .Status.Should().Be(204);
        return wordId;
    }

    private static async Task<JsonElement> GetJsonAsync(IAPIRequestContext content, string path, string token)
    {
        IAPIResponse response = await content.GetAsync(path, new APIRequestContextOptions { Headers = AuthHeader(token) });
        response.Status.Should().Be(200, $"GET {path} should succeed, got {response.Status}: {await response.TextAsync()}");
        return (await response.JsonAsync())!.Value;
    }

    private static async Task<string> RegisterLearnerAsync(IAPIRequestContext identity, string ageGroup)
    {
        string email = $"vocab-e2e-{Guid.NewGuid():N}@example.com";
        IAPIResponse response = await identity.PostAsync("/api/auth/register", new APIRequestContextOptions
        {
            DataObject = new
            {
                email,
                password = "ChangeMe123!",
                displayName = "E2E Vocabulary Learner",
                ageGroup,
            },
        });
        response.Ok.Should().BeTrue($"self-registration should succeed, got {response.Status}: {await response.TextAsync()}");
        JsonElement body = (await response.JsonAsync())!.Value;
        return body.GetProperty("token").GetString()!;
    }

    private static async Task<string> LoginAsync(IAPIRequestContext identity, string email, string password)
    {
        IAPIResponse response = await identity.PostAsync("/api/auth/login", new APIRequestContextOptions
        {
            DataObject = new { email, password },
        });
        response.Ok.Should().BeTrue(
            $"login for '{email}' should succeed (requires the Development admin seed — see class docs), got {response.Status}: {await response.TextAsync()}");
        JsonElement body = (await response.JsonAsync())!.Value;
        return body.GetProperty("token").GetString()!;
    }

    private static Dictionary<string, string> AuthHeader(string token)
    {
        return new Dictionary<string, string> { ["Authorization"] = $"Bearer {token}" };
    }
}
