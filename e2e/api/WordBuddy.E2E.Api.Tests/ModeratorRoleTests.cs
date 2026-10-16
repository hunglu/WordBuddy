using System.Text.Json;
using FluentAssertions;
using Microsoft.Playwright;

namespace WordBuddy.E2E.Api.Tests;

/// <summary>
/// WB-56: an admin grants the Moderator role to a fresh adult account. After the next sign-in the
/// moderator manages content, but not users, and cannot moderate their own word. Every test uses
/// fresh accounts, so the seeded users keep their roles. Needs Identity + Content up. The admin
/// login comes from the Development seed (override with <c>E2E_ADMIN_EMAIL</c>/<c>E2E_ADMIN_PASSWORD</c>).
/// </summary>
[Collection(ApiRequestContextCollection.Name)]
public sealed class ModeratorRoleTests
{
    private const string Password = "ChangeMe123!";

    private static readonly string AdminEmail =
        Environment.GetEnvironmentVariable("E2E_ADMIN_EMAIL") ?? "admin@wordbuddy.com";

    private static readonly string AdminPassword =
        Environment.GetEnvironmentVariable("E2E_ADMIN_PASSWORD") ?? "Admin@123";

    private readonly ApiRequestContextFixture _fixture;

    public ModeratorRoleTests(ApiRequestContextFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GrantedModerator_AfterRelogin_ModeratesWordEditsLexeme_ButCannotManageUsers()
    {
        IAPIRequestContext identity = await _fixture.NewContextAsync(ServiceUrls.Identity);
        IAPIRequestContext content = await _fixture.NewContextAsync(ServiceUrls.Content);
        try
        {
            string adminToken = await LoginAsync(identity, AdminEmail, AdminPassword);
            (string _, string email, Guid moderatorId) = await RegisterAsync(identity, "Adult");
            string learnerToken = (await RegisterAsync(identity, "Adult")).Token;

            // Before the grant the new account has no content rights.
            string beforeToken = await LoginAsync(identity, email, Password);
            (await content.GetAsync("/api/vocabulary/moderation/pending", Options(beforeToken))).Status.Should().Be(403);

            await SetModeratorAsync(identity, adminToken, moderatorId, true, 204);
            try
            {
                // The old token does not change. The next sign-in carries the claim.
                (await content.GetAsync("/api/vocabulary/moderation/pending", Options(beforeToken))).Status.Should().Be(403);
                IAPIResponse login = await identity.PostAsync("/api/auth/login", Options(null, new { email, password = Password }));
                JsonElement body = (await login.JsonAsync())!.Value;
                body.GetProperty("user").GetProperty("isModerator").GetBoolean().Should().BeTrue();
                string moderatorToken = body.GetProperty("token").GetString()!;

                // Moderate another learner's shared word.
                string word = $"modw{Guid.NewGuid():N}"[..14];
                Guid wordId = await AddWordAsync(content, learnerToken, word);
                (await content.PostAsync($"/api/vocabulary/{wordId}/share", Options(learnerToken))).Status.Should().Be(204);
                (await content.GetAsync("/api/vocabulary/moderation/pending", Options(moderatorToken))).Status.Should().Be(200);
                IAPIResponse moderate = await content.PostAsync(
                    $"/api/vocabulary/moderation/{wordId}", Options(moderatorToken, new { approve = true, visibleToChildren = true }));
                moderate.Status.Should().Be(204, await moderate.TextAsync());

                // Edit a lexeme.
                IAPIResponse search = await content.GetAsync($"/api/vocabulary/admin/lexemes?search={word}", Options(moderatorToken));
                search.Status.Should().Be(200, await search.TextAsync());
                Guid lexemeId = (await search.JsonAsync())!.Value.GetProperty("items")[0].GetProperty("id").GetGuid();
                IAPIResponse edit = await content.PutAsync(
                    $"/api/vocabulary/admin/lexemes/{lexemeId}", Options(moderatorToken, new { partOfSpeech = "Noun", cefrLevel = "B2" }));
                edit.Status.Should().Be(204, await edit.TextAsync());

                // Lesson authoring is open to moderators, the backfill is not.
                int lessons = (await content.GetAsync("/api/lessons/admin", Options(moderatorToken))).Status;
                lessons.Should().NotBe(403).And.NotBe(401);
                (await content.PostAsync("/api/vocabulary/admin/learner-words/republish", Options(moderatorToken))).Status.Should().Be(403);

                // User and role endpoints stay admin only.
                (await identity.GetAsync("/api/auth/admin/users", Options(moderatorToken))).Status.Should().Be(403);
                (await identity.PutAsync($"/api/auth/admin/users/{moderatorId}/moderator", Options(moderatorToken, new { isModerator = false })))
                    .Status.Should().Be(403);
            }
            finally
            {
                await SetModeratorAsync(identity, adminToken, moderatorId, false, 204);
            }
        }
        finally
        {
            await identity.DisposeAsync();
            await content.DisposeAsync();
        }
    }

    [Fact]
    public async Task Moderator_ModeratesOwnWord_ReturnsForbidden_AndAdminCanStillModerateIt()
    {
        IAPIRequestContext identity = await _fixture.NewContextAsync(ServiceUrls.Identity);
        IAPIRequestContext content = await _fixture.NewContextAsync(ServiceUrls.Content);
        try
        {
            string adminToken = await LoginAsync(identity, AdminEmail, AdminPassword);
            (string _, string email, Guid moderatorId) = await RegisterAsync(identity, "Adult");
            await SetModeratorAsync(identity, adminToken, moderatorId, true, 204);
            try
            {
                string moderatorToken = await LoginAsync(identity, email, Password);
                Guid wordId = await AddWordAsync(content, moderatorToken, $"own{Guid.NewGuid():N}"[..14]);
                (await content.PostAsync($"/api/vocabulary/{wordId}/share", Options(moderatorToken))).Status.Should().Be(204);

                IAPIResponse own = await content.PostAsync(
                    $"/api/vocabulary/moderation/{wordId}", Options(moderatorToken, new { approve = true, visibleToChildren = false }));
                own.Status.Should().Be(403, await own.TextAsync());

                IAPIResponse byAdmin = await content.PostAsync(
                    $"/api/vocabulary/moderation/{wordId}", Options(adminToken, new { approve = true, visibleToChildren = false }));
                byAdmin.Status.Should().Be(204, await byAdmin.TextAsync());
            }
            finally
            {
                await SetModeratorAsync(identity, adminToken, moderatorId, false, 204);
            }
        }
        finally
        {
            await identity.DisposeAsync();
            await content.DisposeAsync();
        }
    }

    [Fact]
    public async Task SetModerator_ChildTarget_Returns400()
    {
        IAPIRequestContext identity = await _fixture.NewContextAsync(ServiceUrls.Identity);
        try
        {
            string adminToken = await LoginAsync(identity, AdminEmail, AdminPassword);
            (string _, string _, Guid childId) = await RegisterAsync(identity, "Child");

            await SetModeratorAsync(identity, adminToken, childId, true, 400);
        }
        finally
        {
            await identity.DisposeAsync();
        }
    }

    [Fact]
    public async Task AdminUsers_ListsAdultsOnly_AndRejectsLearnerCaller()
    {
        IAPIRequestContext identity = await _fixture.NewContextAsync(ServiceUrls.Identity);
        try
        {
            string adminToken = await LoginAsync(identity, AdminEmail, AdminPassword);
            (string learnerToken, string email, Guid id) = await RegisterAsync(identity, "Adult");
            (string _, string childEmail, Guid _) = await RegisterAsync(identity, "Child");

            IAPIResponse list = await identity.GetAsync($"/api/auth/admin/users?search={email}", Options(adminToken));
            list.Status.Should().Be(200, await list.TextAsync());
            JsonElement items = (await list.JsonAsync())!.Value.GetProperty("items");
            items.GetArrayLength().Should().Be(1);
            items[0].GetProperty("id").GetGuid().Should().Be(id);
            items[0].GetProperty("isModerator").GetBoolean().Should().BeFalse();

            IAPIResponse children = await identity.GetAsync($"/api/auth/admin/users?search={childEmail}", Options(adminToken));
            (await children.JsonAsync())!.Value.GetProperty("items").GetArrayLength().Should().Be(0, "children are never listed");

            (await identity.GetAsync("/api/auth/admin/users", Options(learnerToken))).Status.Should().Be(403);
        }
        finally
        {
            await identity.DisposeAsync();
        }
    }

    private static async Task SetModeratorAsync(IAPIRequestContext identity, string adminToken, Guid userId, bool isModerator, int expected)
    {
        IAPIResponse response = await identity.PutAsync(
            $"/api/auth/admin/users/{userId}/moderator", Options(adminToken, new { isModerator }));
        response.Status.Should().Be(expected, await response.TextAsync());
    }

    private static async Task<Guid> AddWordAsync(IAPIRequestContext content, string token, string word)
    {
        IAPIResponse add = await content.PostAsync("/api/vocabulary", Options(token, new
        {
            word,
            definition = $"A word made up by the moderator E2E test {Guid.NewGuid():N}.",
            example = (string?)null,
        }));
        add.Ok.Should().BeTrue($"adding a word should succeed, got {add.Status}: {await add.TextAsync()}");
        return (await add.JsonAsync())!.Value.GetGuid();
    }

    private static async Task<(string Token, string Email, Guid Id)> RegisterAsync(IAPIRequestContext identity, string ageGroup)
    {
        string email = $"moderator-e2e-{Guid.NewGuid():N}@example.com";
        IAPIResponse response = await identity.PostAsync("/api/auth/register", Options(null, new
        {
            email,
            password = Password,
            displayName = "E2E Moderator User",
            ageGroup,
        }));
        response.Ok.Should().BeTrue($"self-registration should succeed, got {response.Status}: {await response.TextAsync()}");
        JsonElement body = (await response.JsonAsync())!.Value;
        return (body.GetProperty("token").GetString()!, email, body.GetProperty("user").GetProperty("id").GetGuid());
    }

    private static async Task<string> LoginAsync(IAPIRequestContext identity, string email, string password)
    {
        IAPIResponse response = await identity.PostAsync("/api/auth/login", Options(null, new { email, password }));
        response.Ok.Should().BeTrue($"login should succeed, got {response.Status}: {await response.TextAsync()}");
        return (await response.JsonAsync())!.Value.GetProperty("token").GetString()!;
    }

    private static APIRequestContextOptions Options(string? token, object? body = null) => new()
    {
        Headers = token is null ? null : new Dictionary<string, string> { ["Authorization"] = $"Bearer {token}" },
        DataObject = body,
    };
}
