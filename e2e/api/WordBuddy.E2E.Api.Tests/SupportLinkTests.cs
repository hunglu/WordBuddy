using System.Text.Json;
using FluentAssertions;
using Microsoft.Playwright;

namespace WordBuddy.E2E.Api.Tests;

/// <summary>
/// WB-24: support links across Identity, Content and Progress.
/// Child invites → adult accepts (Primary) → child learns → supporter sets cap → second supporter
/// approved by Primary → unlink second → second supporter loses access.
/// Link events travel through RabbitMQ, so the full stack must run.
/// </summary>
[Collection(ApiRequestContextCollection.Name)]
public sealed class SupportLinkTests
{
    private const string ClientDateHeader = "X-Client-CurrentDateTime";
    private const string Password = "ChangeMe123!";
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    private readonly ApiRequestContextFixture _fixture;

    public SupportLinkTests(ApiRequestContextFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task SupportLink_ChildLifecycle_SecondSupporterLosesAccessAfterUnlink()
    {
        await using Contexts c = await Contexts.CreateAsync(_fixture);
        (string childToken, Guid childId) = await RegisterAsync(c.Identity, "Child");
        (string primaryToken, _) = await RegisterAsync(c.Identity, "Adult");
        (string secondToken, _) = await RegisterAsync(c.Identity, "Adult");

        // 0. Child without a supporter is blocked from learning.
        IAPIResponse blocked = await GetSessionAsync(c.Progress, childToken);
        blocked.Status.Should().Be(403, await blocked.TextAsync());
        (await blocked.TextAsync()).Should().Contain("Learner.SupporterRequired");

        // 1. Child invites, first adult accepts → Active + Primary.
        string code1 = await CreateInvitationAsync(c.Identity, childToken);
        JsonElement primaryLink = await AcceptAsync(c.Identity, primaryToken, code1);
        primaryLink.GetProperty("status").GetString().Should().Be("Active");
        primaryLink.GetProperty("isPrimary").GetBoolean().Should().BeTrue();

        // 2. Child can learn once the projection has the link.
        await EventuallyAsync(async () => (await GetSessionAsync(c.Progress, childToken)).Status == 200,
            "child session should open after the Primary link is consumed by Progress");

        // 3. Primary sets the supporter cap; the child sees it.
        IAPIResponse cap = await PutCapAsync(c.Progress, primaryToken, childId, 3);
        cap.Status.Should().Be(200, await cap.TextAsync());
        IAPIResponse childSettings = await c.Progress.GetAsync("/api/progress/vocabulary/settings", Options(childToken));
        childSettings.Status.Should().Be(200, await childSettings.TextAsync());
        (await childSettings.JsonAsync())!.Value.GetProperty("supporterNewWordCap").GetInt32().Should().Be(3);

        // 4. Second adult accepts → PendingPrimaryApproval, no access yet.
        string code2 = await CreateInvitationAsync(c.Identity, childToken);
        JsonElement secondLink = await AcceptAsync(c.Identity, secondToken, code2);
        secondLink.GetProperty("status").GetString().Should().Be("PendingPrimaryApproval");
        secondLink.GetProperty("isPrimary").GetBoolean().Should().BeFalse();
        Guid secondLinkId = secondLink.GetProperty("id").GetGuid();
        (await GetLearnerSettingsAsync(c.Progress, secondToken, childId)).Status.Should().Be(403);

        // 5. Primary approves → second supporter gets access.
        IAPIResponse approve = await c.Identity.PostAsync($"/api/auth/support-links/{secondLinkId}/approve", Options(primaryToken));
        approve.Status.Should().Be(204, await approve.TextAsync());
        await EventuallyAsync(async () => (await GetLearnerSettingsAsync(c.Progress, secondToken, childId)).Status == 200,
            "second supporter should get access after approval is consumed by Progress");

        // 6. Child cannot act on links.
        IAPIResponse childUnlink = await c.Identity.PostAsync($"/api/auth/support-links/{secondLinkId}/unlink-request", Options(childToken));
        childUnlink.Status.Should().Be(403, await childUnlink.TextAsync());

        // 7. Second supporter requests unlink, Primary confirms for the child → revoked.
        IAPIResponse request = await c.Identity.PostAsync($"/api/auth/support-links/{secondLinkId}/unlink-request", Options(secondToken));
        request.Status.Should().Be(204, await request.TextAsync());
        IAPIResponse confirm = await c.Identity.PostAsync($"/api/auth/support-links/{secondLinkId}/unlink-request/confirm", Options(primaryToken));
        confirm.Status.Should().Be(204, await confirm.TextAsync());

        // 8. Access lost for the second supporter; Primary and child keep theirs.
        await EventuallyAsync(async () => (await GetLearnerSettingsAsync(c.Progress, secondToken, childId)).Status == 403,
            "second supporter should lose access after the revoke is consumed by Progress");
        (await GetLearnerSettingsAsync(c.Progress, primaryToken, childId)).Status.Should().Be(200);
        (await GetSessionAsync(c.Progress, childToken)).Status.Should().Be(200);
    }

    [Fact]
    public async Task SupportLink_PrimaryUnlinkRequest_IsRejected()
    {
        await using Contexts c = await Contexts.CreateAsync(_fixture);
        (string childToken, _) = await RegisterAsync(c.Identity, "Child");
        (string primaryToken, _) = await RegisterAsync(c.Identity, "Adult");
        Guid linkId = (await AcceptAsync(c.Identity, primaryToken, await CreateInvitationAsync(c.Identity, childToken)))
            .GetProperty("id").GetGuid();

        IAPIResponse response = await c.Identity.PostAsync($"/api/auth/support-links/{linkId}/unlink-request", Options(primaryToken));

        response.Ok.Should().BeFalse("the Primary link can only change through an admin handover");
    }

    [Fact]
    public async Task SupportLink_AdultLearner_AcceptedLinkIsActiveWithoutPrimary()
    {
        await using Contexts c = await Contexts.CreateAsync(_fixture);
        (string learnerToken, _) = await RegisterAsync(c.Identity, "Adult");
        (string supporterToken, _) = await RegisterAsync(c.Identity, "Adult");

        (await GetSessionAsync(c.Progress, learnerToken)).Status.Should().Be(200, "adults are never gated");
        JsonElement link = await AcceptAsync(c.Identity, supporterToken, await CreateInvitationAsync(c.Identity, learnerToken));

        link.GetProperty("status").GetString().Should().Be("Active");
        link.GetProperty("isPrimary").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task SupportLink_ChildAcceptsInvitation_IsRejected()
    {
        await using Contexts c = await Contexts.CreateAsync(_fixture);
        (string adultToken, _) = await RegisterAsync(c.Identity, "Adult");
        (string childToken, _) = await RegisterAsync(c.Identity, "Child");
        string code = await CreateInvitationAsync(c.Identity, adultToken);

        IAPIResponse response = await c.Identity.PostAsync("/api/auth/support-links/accept", new APIRequestContextOptions
        {
            Headers = Auth(childToken),
            DataObject = new { code, token = (string?)null },
        });

        response.Ok.Should().BeFalse("a supporter must be an Adult account");
    }

    private static Task<IAPIResponse> GetSessionAsync(IAPIRequestContext progress, string token) =>
        progress.GetAsync("/api/progress/vocabulary/session", Options(token,
            new() { [ClientDateHeader] = DateTimeOffset.Now.ToString("yyyy-MM-ddTHH:mm:sszzz") }));

    private static Task<IAPIResponse> GetLearnerSettingsAsync(IAPIRequestContext progress, string token, Guid learnerId) =>
        progress.GetAsync($"/api/progress/vocabulary/learners/{learnerId}/settings", Options(token));

    private static Task<IAPIResponse> PutCapAsync(IAPIRequestContext progress, string token, Guid learnerId, int cap) =>
        progress.PutAsync($"/api/progress/vocabulary/learners/{learnerId}/settings", new APIRequestContextOptions
        {
            Headers = Auth(token),
            DataObject = new { supporterNewWordCap = cap },
        });

    private static async Task<string> CreateInvitationAsync(IAPIRequestContext identity, string token)
    {
        IAPIResponse response = await identity.PostAsync("/api/auth/support-links/invitations", new APIRequestContextOptions
        {
            Headers = Auth(token),
            DataObject = new { inviteAs = "Learner", relationship = (string?)null },
        });
        response.Status.Should().Be(201, await response.TextAsync());
        return (await response.JsonAsync())!.Value.GetProperty("code").GetString()!;
    }

    private static async Task<JsonElement> AcceptAsync(IAPIRequestContext identity, string token, string code)
    {
        IAPIResponse response = await identity.PostAsync("/api/auth/support-links/accept", new APIRequestContextOptions
        {
            Headers = Auth(token),
            DataObject = new { code, token = (string?)null },
        });
        response.Status.Should().Be(200, await response.TextAsync());
        return (await response.JsonAsync())!.Value.Clone();
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
                email = $"support-e2e-{Guid.NewGuid():N}@example.com",
                password = Password,
                displayName = "E2E Support User",
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
