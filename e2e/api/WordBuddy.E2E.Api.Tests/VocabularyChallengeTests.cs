using System.Text.Json;
using FluentAssertions;
using Microsoft.Playwright;

namespace WordBuddy.E2E.Api.Tests;

/// <summary>
/// WB-29: vocabulary challenges across Identity and Progress.
/// An admin makes a supporter a Guardian, the Guardian allows challenges for a linked child, the child joins and sees only
/// their alias, avatar, own row and the top 5. A supporter without the Guardian role cannot give consent.
/// Profile, link and role events travel through RabbitMQ and the Progress scheduler creates the challenges, so the full stack
/// must run (with the WB-29 Identity and Progress builds). Ranks appear after the next score recompute (up to 5 minutes), so
/// the tests do not wait for a rank.
/// </summary>
[Collection(ApiRequestContextCollection.Name)]
public sealed class VocabularyChallengeTests
{
    private const string Password = "ChangeMe123!";
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(90);

    private static readonly string AdminEmail =
        Environment.GetEnvironmentVariable("E2E_ADMIN_EMAIL") ?? "admin@wordbuddy.com";

    private static readonly string AdminPassword =
        Environment.GetEnvironmentVariable("E2E_ADMIN_PASSWORD") ?? "Admin@123";

    private readonly ApiRequestContextFixture _fixture;

    public VocabularyChallengeTests(ApiRequestContextFixture fixture)
    {
        _fixture = fixture;
    }

    private sealed record Account(string Token, Guid Id, string Email);

    [Fact]
    public async Task Challenges_GuardianAllowsChild_ChildBoardShowsOnlyAliasAvatarOwnRowAndTopFive()
    {
        await using Contexts c = await Contexts.CreateAsync(_fixture);
        string alias = $"Kiwi{Guid.NewGuid():N}"[..10];
        Account guardian = await RegisterAsync(c.Identity, "Adult", "E2E Guardian Real");
        Account child = await RegisterAsync(c.Identity, "Child", "E2E Child Real");
        string adminToken = await LoginAsync(c.Identity, AdminEmail, AdminPassword);

        IAPIResponse setAlias = await c.Identity.PutAsync("/api/auth/profile", new APIRequestContextOptions
        {
            Headers = Auth(child.Token),
            DataObject = new { alias, avatarId = "fox" },
        });
        setAlias.Status.Should().Be(200, await setAlias.TextAsync());

        IAPIResponse grant = await c.Identity.PutAsync($"/api/auth/admin/users/{guardian.Id}/guardian", new APIRequestContextOptions
        {
            Headers = Auth(adminToken),
            DataObject = new { isGuardian = true },
        });
        grant.Status.Should().Be(204, await grant.TextAsync());
        string guardianToken = await LoginAsync(c.Identity, guardian.Email, Password); // new token carries the guardian claim
        await LinkAsync(c.Identity, child.Token, guardianToken);

        // Without consent the child sees nothing and cannot join.
        (await ListAsync(c.Progress, child.Token)).GetArrayLength().Should().Be(0);

        // The profile, role and link events reach Progress through RabbitMQ: retry until the consent is accepted.
        await EventuallyAsync(async () =>
        {
            IAPIResponse consent = await c.Progress.PutAsync($"/api/progress/challenges/consent/{child.Id}", new APIRequestContextOptions
            {
                Headers = Auth(guardianToken),
                DataObject = new { enabled = true },
            });
            return consent.Status == 204;
        }, "the guardian can give consent");

        string challengeId = null!;
        await EventuallyAsync(async () =>
        {
            JsonElement list = await ListAsync(c.Progress, child.Token);
            foreach (JsonElement challenge in list.EnumerateArray())
            {
                if (challenge.GetProperty("audience").GetString() == "Child" && challenge.GetProperty("templateKey").GetString() == "weekly-league")
                {
                    challengeId = challenge.GetProperty("id").GetString()!;
                    return true;
                }
            }

            return false;
        }, "the scheduler made a child Weekly League");

        IAPIResponse join = await c.Progress.PostAsync($"/api/progress/challenges/{challengeId}/join", new APIRequestContextOptions { Headers = Auth(child.Token) });
        join.Status.Should().Be(204, await join.TextAsync());

        IAPIResponse boardResponse = await c.Progress.GetAsync($"/api/progress/challenges/{challengeId}/board", new APIRequestContextOptions { Headers = Auth(child.Token) });
        boardResponse.Status.Should().Be(200, await boardResponse.TextAsync());
        string raw = await boardResponse.TextAsync();
        JsonElement board = JsonDocument.Parse(raw).RootElement;

        JsonElement entries = board.GetProperty("entries");
        entries.GetArrayLength().Should().BeInRange(1, 6, "own row plus the top 5");
        JsonElement mine = entries.EnumerateArray().Single(e => e.GetProperty("isMe").GetBoolean());
        mine.GetProperty("name").GetString().Should().Be(alias);
        mine.GetProperty("avatarId").GetString().Should().Be("fox");
        foreach (JsonElement entry in entries.EnumerateArray())
        {
            entry.EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo(["rank", "name", "avatarId", "isMe", "score", "targetReached"]);
            if (!entry.GetProperty("isMe").GetBoolean())
            {
                entry.GetProperty("score").ValueKind.Should().Be(JsonValueKind.Null);
            }
        }

        raw.Should().NotContain("E2E Child Real").And.NotContain(child.Email).And.NotContain(child.Id.ToString());
        raw.Should().NotContainEquivalentOf("userId");
    }

    [Fact]
    public async Task Challenges_ChildWithoutConsent_CannotJoinOrReadABoard()
    {
        await using Contexts c = await Contexts.CreateAsync(_fixture);
        Account child = await RegisterAsync(c.Identity, "Child", "E2E Child Real");
        Account adult = await RegisterAsync(c.Identity, "Adult", "E2E Adult Real");

        // An adult can see the adult challenges; use one of them to try the child endpoints with the wrong audience.
        string adultChallengeId = null!;
        await EventuallyAsync(async () =>
        {
            JsonElement list = await ListAsync(c.Progress, adult.Token);
            foreach (JsonElement challenge in list.EnumerateArray())
            {
                if (challenge.GetProperty("audience").GetString() == "Adult")
                {
                    adultChallengeId = challenge.GetProperty("id").GetString()!;
                    return true;
                }
            }

            return false;
        }, "the scheduler made an adult challenge");

        IAPIResponse join = await c.Progress.PostAsync($"/api/progress/challenges/{adultChallengeId}/join", new APIRequestContextOptions { Headers = Auth(child.Token) });
        IAPIResponse board = await c.Progress.GetAsync($"/api/progress/challenges/{adultChallengeId}/board", new APIRequestContextOptions { Headers = Auth(child.Token) });

        join.Status.Should().Be(403, "a child never joins an adult challenge");
        board.Status.Should().Be(403);
    }

    [Fact]
    public async Task Challenges_SupporterWithoutGuardianRole_CannotGiveConsent()
    {
        await using Contexts c = await Contexts.CreateAsync(_fixture);
        Account supporter = await RegisterAsync(c.Identity, "Adult", "E2E Supporter Real");
        Account child = await RegisterAsync(c.Identity, "Child", "E2E Child Real");
        await LinkAsync(c.Identity, child.Token, supporter.Token);

        IAPIResponse consent = await c.Progress.PutAsync($"/api/progress/challenges/consent/{child.Id}", new APIRequestContextOptions
        {
            Headers = Auth(supporter.Token),
            DataObject = new { enabled = true },
        });

        consent.Status.Should().Be(403);
        (await ListAsync(c.Progress, child.Token)).GetArrayLength().Should().Be(0, "no consent was stored");
    }

    private static async Task<JsonElement> ListAsync(IAPIRequestContext progress, string token)
    {
        IAPIResponse response = await progress.GetAsync("/api/progress/challenges", new APIRequestContextOptions { Headers = Auth(token) });
        response.Status.Should().Be(200, await response.TextAsync());
        return (await response.JsonAsync())!.Value.Clone();
    }

    private static async Task LinkAsync(IAPIRequestContext identity, string childToken, string supporterToken)
    {
        IAPIResponse invitation = await identity.PostAsync("/api/auth/support-links/invitations", new APIRequestContextOptions
        {
            Headers = Auth(childToken),
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

    private static async Task<Account> RegisterAsync(IAPIRequestContext identity, string ageGroup, string displayName)
    {
        string email = $"challenge-e2e-{Guid.NewGuid():N}@example.com";
        IAPIResponse response = await identity.PostAsync("/api/auth/register", new APIRequestContextOptions
        {
            DataObject = new { email, password = Password, displayName, ageGroup },
        });
        response.Ok.Should().BeTrue($"self-registration should succeed, got {response.Status}: {await response.TextAsync()}");
        JsonElement body = (await response.JsonAsync())!.Value;
        return new Account(body.GetProperty("token").GetString()!, body.GetProperty("user").GetProperty("id").GetGuid(), email);
    }

    private static async Task<string> LoginAsync(IAPIRequestContext identity, string email, string password)
    {
        IAPIResponse response = await identity.PostAsync("/api/auth/login", new APIRequestContextOptions
        {
            DataObject = new { email, password },
        });
        response.Ok.Should().BeTrue($"login should succeed, got {response.Status}: {await response.TextAsync()}");
        return (await response.JsonAsync())!.Value.GetProperty("token").GetString()!;
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

            await Task.Delay(1000);
        }

        throw new TimeoutException($"Timed out after {Timeout.TotalSeconds}s: {because}.");
    }

    private static Dictionary<string, string> Auth(string token) => new() { ["Authorization"] = $"Bearer {token}" };

    private sealed class Contexts : IAsyncDisposable
    {
        public required IAPIRequestContext Identity { get; init; }

        public required IAPIRequestContext Progress { get; init; }

        public static async Task<Contexts> CreateAsync(ApiRequestContextFixture fixture) => new()
        {
            Identity = await fixture.NewContextAsync(ServiceUrls.Identity),
            Progress = await fixture.NewContextAsync(ServiceUrls.Progress),
        };

        public async ValueTask DisposeAsync()
        {
            await Identity.DisposeAsync();
            await Progress.DisposeAsync();
        }
    }
}
