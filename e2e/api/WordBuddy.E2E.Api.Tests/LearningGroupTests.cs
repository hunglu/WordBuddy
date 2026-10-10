using System.Text.Json;
using FluentAssertions;
using Microsoft.Playwright;

namespace WordBuddy.E2E.Api.Tests;

/// <summary>
/// WB-27: learning groups. A supporter creates a group, adds an adult and a child learner (the child waits
/// for the Primary supporter), assigns a word list once, and a revoked support link removes the learner.
/// Group events travel through RabbitMQ, so the full stack must run with the WB-27 Identity, Content and
/// Progress builds. The admin login comes from the Development seed (same as <c>PersonalVocabularyTests</c>).
/// </summary>
[Collection(ApiRequestContextCollection.Name)]
public sealed class LearningGroupTests
{
    private const string ClientDateHeader = "X-Client-CurrentDateTime";
    private const string Password = "ChangeMe123!";
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);
    private static int _offsetCounter = Random.Shared.Next(0, 600);

    private static readonly string AdminEmail =
        Environment.GetEnvironmentVariable("E2E_ADMIN_EMAIL") ?? "admin@wordbuddy.com";

    private static readonly string AdminPassword =
        Environment.GetEnvironmentVariable("E2E_ADMIN_PASSWORD") ?? "Admin@123";

    private readonly ApiRequestContextFixture _fixture;

    public LearningGroupTests(ApiRequestContextFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task LearningGroup_AssignList_AddsWordsToAdultAndApprovedChildAndSkipsAdultOnlyWord()
    {
        await using Contexts c = await Contexts.CreateAsync(_fixture);
        Scenario s = await ArrangeAsync(c);

        // Create the group and add both learners. The adult is active at once; the child waits for the Primary.
        Guid groupId = await CreateGroupAsync(c.Identity, s.SupporterToken);
        JsonElement added = await AddMembersAsync(c.Identity, s.SupporterToken, groupId, s.AdultId, s.ChildId);
        OutcomeOf(added, s.AdultId).Should().Be("Added");
        OutcomeOf(added, s.ChildId).Should().Be("PendingApproval");

        JsonElement pendingGroup = await GetGroupAsync(c.Identity, s.SupporterToken, groupId);
        StatusOf(pendingGroup, s.AdultId).Should().Be("Active");
        StatusOf(pendingGroup, s.ChildId).Should().Be("PendingPrimaryApproval");

        // Only the child's Primary sees and answers the request.
        (await GetApprovalsAsync(c.Identity, s.SupporterToken)).GetArrayLength().Should().Be(0);
        Guid memberId = await FindApprovalAsync(c.Identity, s.PrimaryToken, groupId);
        IAPIResponse approve = await c.Identity.PostAsync($"/api/auth/groups/approvals/{memberId}/approve", Options(s.PrimaryToken));
        approve.Status.Should().Be(204, await approve.TextAsync());
        StatusOf(await GetGroupAsync(c.Identity, s.SupporterToken, groupId), s.ChildId).Should().Be("Active");

        // Wait until Content has both members. An adult-only word is a side-effect free probe:
        // it is skipped for every member, so the skipped count equals the member count.
        await EventuallyAsync(async () =>
        {
            IAPIResponse probe = await AssignAsync(c.Content, s.SupporterToken, groupId, s.AdultOnlyWordId);
            return probe.Status == 200 && (await probe.JsonAsync())!.Value.GetProperty("skippedForChildren").GetInt32() == 2;
        }, "Content knows both active group members");

        // One call, two words: the child-safe word reaches both learners, the adult-only word reaches nobody.
        IAPIResponse assign = await AssignAsync(c.Content, s.SupporterToken, groupId, s.SafeWordId, s.AdultOnlyWordId);
        assign.Status.Should().Be(200, await assign.TextAsync());
        JsonElement result = (await assign.JsonAsync())!.Value;
        result.GetProperty("added").GetInt32().Should().Be(2);
        result.GetProperty("alreadyHad").GetInt32().Should().Be(0);
        result.GetProperty("skippedForChildren").GetInt32().Should().Be(2);

        (await MyWordIdsAsync(c.Content, s.AdultToken)).Should().Contain(s.SafeWordId).And.NotContain(s.AdultOnlyWordId);
        (await MyWordIdsAsync(c.Content, s.ChildToken)).Should().Contain(s.SafeWordId).And.NotContain(s.AdultOnlyWordId);

        // Assigning the same word again is idempotent.
        IAPIResponse again = await AssignAsync(c.Content, s.SupporterToken, groupId, s.SafeWordId);
        again.Status.Should().Be(200, await again.TextAsync());
        JsonElement againResult = (await again.JsonAsync())!.Value;
        againResult.GetProperty("added").GetInt32().Should().Be(0);
        againResult.GetProperty("alreadyHad").GetInt32().Should().Be(2);

        // The assignment history lists the assigned words, owner only.
        IAPIResponse history = await c.Content.GetAsync($"/api/vocabulary/groups/{groupId}/words", Options(s.SupporterToken));
        history.Status.Should().Be(200, await history.TextAsync());
        (await history.JsonAsync())!.Value.EnumerateArray()
            .Select(a => a.GetProperty("senseId").GetGuid()).Should().Contain(s.SafeWordId);
    }

    [Fact]
    public async Task LearningGroup_Dashboard_ListsActiveMembersByIdOnlyAndRejectsOtherSupporter()
    {
        await using Contexts c = await Contexts.CreateAsync(_fixture);
        Scenario s = await ArrangeAsync(c);
        Guid groupId = await CreateGroupAsync(c.Identity, s.SupporterToken);
        await AddMembersAsync(c.Identity, s.SupporterToken, groupId, s.AdultId, s.ChildId);
        Guid memberId = await FindApprovalAsync(c.Identity, s.PrimaryToken, groupId);
        (await c.Identity.PostAsync($"/api/auth/groups/approvals/{memberId}/approve", Options(s.PrimaryToken)))
            .Status.Should().Be(204);

        // Progress receives the member events through RabbitMQ.
        JsonElement dashboard = default;
        await EventuallyAsync(async () =>
        {
            IAPIResponse response = await GetGroupDashboardAsync(c.Progress, s.SupporterToken, groupId);
            if (response.Status != 200)
            {
                return false;
            }

            dashboard = (await response.JsonAsync())!.Value.Clone();
            return dashboard.GetProperty("members").GetArrayLength() == 2;
        }, "group dashboard lists both active members");

        dashboard.GetProperty("totals").GetProperty("memberCount").GetInt32().Should().Be(2);
        dashboard.GetProperty("members").EnumerateArray().Select(m => m.GetProperty("learnerId").GetGuid())
            .Should().BeEquivalentTo(new[] { s.AdultId, s.ChildId });

        // Names, aliases and emails live in Identity only: the dashboard body has none of them.
        string body = dashboard.GetRawText();
        body.Should().NotContainEquivalentOf("displayName").And.NotContainEquivalentOf("email").And.NotContainEquivalentOf("alias");

        // A supporter who does not own the group gets 403 on the dashboard and on assignment.
        (string strangerToken, _) = await RegisterAsync(c.Identity, "Adult");
        (await GetGroupDashboardAsync(c.Progress, strangerToken, groupId)).Status.Should().Be(403);
        (await AssignAsync(c.Content, strangerToken, groupId, s.SafeWordId)).Status.Should().Be(403);
    }

    [Fact]
    public async Task LearningGroup_RevokedSupportLink_RemovesLearnerFromGroup()
    {
        await using Contexts c = await Contexts.CreateAsync(_fixture);
        Scenario s = await ArrangeAsync(c);
        Guid groupId = await CreateGroupAsync(c.Identity, s.SupporterToken);
        await AddMembersAsync(c.Identity, s.SupporterToken, groupId, s.AdultId, s.ChildId);
        Guid memberId = await FindApprovalAsync(c.Identity, s.PrimaryToken, groupId);
        (await c.Identity.PostAsync($"/api/auth/groups/approvals/{memberId}/approve", Options(s.PrimaryToken)))
            .Status.Should().Be(204);
        await EventuallyAsync(async () =>
        {
            IAPIResponse response = await GetGroupDashboardAsync(c.Progress, s.SupporterToken, groupId);
            return response.Status == 200 && (await response.JsonAsync())!.Value.GetProperty("members").GetArrayLength() == 2;
        }, "group dashboard lists both active members");

        // The supporter asks to unlink from the child; the Primary confirms for the child → link revoked.
        (await c.Identity.PostAsync($"/api/auth/support-links/{s.ChildSupporterLinkId}/unlink-request", Options(s.SupporterToken)))
            .Status.Should().Be(204);
        (await c.Identity.PostAsync($"/api/auth/support-links/{s.ChildSupporterLinkId}/unlink-request/confirm", Options(s.PrimaryToken)))
            .Status.Should().Be(204);

        // Identity removes the member in the same transaction as the revoke.
        JsonElement group = await GetGroupAsync(c.Identity, s.SupporterToken, groupId);
        StatusOf(group, s.ChildId).Should().NotBe("Active").And.NotBe("PendingPrimaryApproval");
        StatusOf(group, s.AdultId).Should().Be("Active");

        // Progress drops the child; the adult stays. The dashboard cache is bypassed by a fresh UTC offset per call.
        await EventuallyAsync(async () =>
        {
            IAPIResponse response = await GetGroupDashboardAsync(c.Progress, s.SupporterToken, groupId);
            if (response.Status != 200)
            {
                return false;
            }

            List<Guid> ids = (await response.JsonAsync())!.Value.GetProperty("members").EnumerateArray()
                .Select(m => m.GetProperty("learnerId").GetGuid()).ToList();
            return ids.SequenceEqual(new[] { s.AdultId });
        }, "revoked child leaves the group dashboard");

        // Content stops assigning to the child; only the adult gets the word.
        await EventuallyAsync(async () =>
        {
            IAPIResponse probe = await AssignAsync(c.Content, s.SupporterToken, groupId, s.AdultOnlyWordId);
            return probe.Status == 200 && (await probe.JsonAsync())!.Value.GetProperty("skippedForChildren").GetInt32() == 1;
        }, "Content drops the removed child");
        IAPIResponse assign = await AssignAsync(c.Content, s.SupporterToken, groupId, s.SafeWordId);
        assign.Status.Should().Be(200, await assign.TextAsync());
        (await assign.JsonAsync())!.Value.GetProperty("added").GetInt32().Should().Be(1);
        (await MyWordIdsAsync(c.Content, s.AdultToken)).Should().Contain(s.SafeWordId);
        (await MyWordIdsAsync(c.Content, s.ChildToken)).Should().NotContain(s.SafeWordId);
    }

    // ---- arrange ----

    /// <summary>
    /// Supporter S owns the group. Adult learner A is linked to S. Child C has Primary P and S as a second
    /// supporter (approved by P). One child-safe and one adult-only shared word exist.
    /// </summary>
    private async Task<Scenario> ArrangeAsync(Contexts c)
    {
        (string supporterToken, _) = await RegisterAsync(c.Identity, "Adult");
        (string primaryToken, _) = await RegisterAsync(c.Identity, "Adult");
        (string adultToken, Guid adultId) = await RegisterAsync(c.Identity, "Adult");
        (string childToken, Guid childId) = await RegisterAsync(c.Identity, "Child");

        await AcceptAsync(c.Identity, supporterToken, await InviteAsync(c.Identity, adultToken));

        JsonElement primaryLink = await AcceptAsync(c.Identity, primaryToken, await InviteAsync(c.Identity, childToken));
        primaryLink.GetProperty("isPrimary").GetBoolean().Should().BeTrue();
        JsonElement secondLink = await AcceptAsync(c.Identity, supporterToken, await InviteAsync(c.Identity, childToken));
        secondLink.GetProperty("status").GetString().Should().Be("PendingPrimaryApproval");
        Guid secondLinkId = secondLink.GetProperty("id").GetGuid();
        (await c.Identity.PostAsync($"/api/auth/support-links/{secondLinkId}/approve", Options(primaryToken)))
            .Status.Should().Be(204);

        string adminToken = await LoginAsync(c.Identity, AdminEmail, AdminPassword);
        Guid safeWordId = await ShareWordAsync(c.Content, supporterToken, adminToken, "group-safe", visibleToChildren: true);
        Guid adultOnlyWordId = await ShareWordAsync(c.Content, supporterToken, adminToken, "group-adult", visibleToChildren: false);

        // The group only works once Content knows the supporter links.
        await EventuallyAsync(async () =>
        {
            IAPIResponse response = await c.Progress.GetAsync($"/api/progress/dashboard/learners/{childId}?days=30",
                Options(supporterToken, ClientDate()));
            return response.Status == 200;
        }, "supporter link to the child reaches Progress");

        return new Scenario(supporterToken, primaryToken, adultToken, adultId, childToken, childId, secondLinkId, safeWordId, adultOnlyWordId);
    }

    private static async Task<Guid> ShareWordAsync(IAPIRequestContext content, string authorToken, string adminToken, string prefix, bool visibleToChildren)
    {
        IAPIResponse add = await content.PostAsync("/api/vocabulary", new APIRequestContextOptions
        {
            Headers = Auth(authorToken),
            DataObject = new { word = $"{prefix}-{Guid.NewGuid():N}", definition = "e2e group word", example = (string?)null },
        });
        add.Ok.Should().BeTrue($"adding a word should succeed, got {add.Status}: {await add.TextAsync()}");
        Guid wordId = (await add.JsonAsync())!.Value.GetGuid();

        (await content.PostAsync($"/api/vocabulary/{wordId}/share", Options(authorToken))).Status.Should().Be(204);
        (await content.PostAsync($"/api/vocabulary/moderation/{wordId}", new APIRequestContextOptions
        {
            Headers = Auth(adminToken),
            DataObject = new { approve = true, visibleToChildren },
        })).Status.Should().Be(204);
        return wordId;
    }

    // ---- group calls ----

    private static async Task<Guid> CreateGroupAsync(IAPIRequestContext identity, string token)
    {
        IAPIResponse response = await identity.PostAsync("/api/auth/groups", new APIRequestContextOptions
        {
            Headers = Auth(token),
            DataObject = new { name = $"E2E group {Guid.NewGuid():N}"[..20] },
        });
        response.Status.Should().Be(201, await response.TextAsync());
        return (await response.JsonAsync())!.Value.GetProperty("id").GetGuid();
    }

    private static async Task<JsonElement> AddMembersAsync(IAPIRequestContext identity, string token, Guid groupId, params Guid[] learnerIds)
    {
        IAPIResponse response = await identity.PostAsync($"/api/auth/groups/{groupId}/members", new APIRequestContextOptions
        {
            Headers = Auth(token),
            DataObject = new { learnerIds },
        });
        response.Status.Should().Be(200, await response.TextAsync());
        return (await response.JsonAsync())!.Value.Clone();
    }

    private static async Task<JsonElement> GetGroupAsync(IAPIRequestContext identity, string token, Guid groupId)
    {
        IAPIResponse response = await identity.GetAsync($"/api/auth/groups/{groupId}", Options(token));
        response.Status.Should().Be(200, await response.TextAsync());
        return (await response.JsonAsync())!.Value.Clone();
    }

    private static async Task<JsonElement> GetApprovalsAsync(IAPIRequestContext identity, string token)
    {
        IAPIResponse response = await identity.GetAsync("/api/auth/groups/approvals", Options(token));
        response.Status.Should().Be(200, await response.TextAsync());
        return (await response.JsonAsync())!.Value.Clone();
    }

    private static async Task<Guid> FindApprovalAsync(IAPIRequestContext identity, string primaryToken, Guid groupId)
    {
        JsonElement approvals = await GetApprovalsAsync(identity, primaryToken);
        JsonElement match = approvals.EnumerateArray().Single(a => a.GetProperty("groupId").GetGuid() == groupId);
        return match.GetProperty("memberId").GetGuid();
    }

    private static Task<IAPIResponse> AssignAsync(IAPIRequestContext content, string token, Guid groupId, params Guid[] senseIds) =>
        content.PostAsync($"/api/vocabulary/groups/{groupId}/words", new APIRequestContextOptions
        {
            Headers = Auth(token),
            DataObject = new { senseIds },
        });

    /// <summary>
    /// The group dashboard is cached for 5 minutes per (group, days, client UTC offset) and not cleared on member
    /// events (plan WB-27 / WB-26 Q5). Every call uses a fresh UTC offset, so a poll never reads an older cached answer.
    /// </summary>
    private static Task<IAPIResponse> GetGroupDashboardAsync(IAPIRequestContext progress, string token, Guid groupId)
    {
        int minutes = Interlocked.Increment(ref _offsetCounter) % 600;
        TimeSpan offset = TimeSpan.FromMinutes(minutes);
        string stamp = DateTimeOffset.UtcNow.ToOffset(offset).ToString("yyyy-MM-ddTHH:mm:sszzz");
        return progress.GetAsync($"/api/progress/dashboard/groups/{groupId}?days=30",
            Options(token, new Dictionary<string, string> { [ClientDateHeader] = stamp }));
    }

    private static async Task<List<Guid>> MyWordIdsAsync(IAPIRequestContext content, string token)
    {
        IAPIResponse response = await content.GetAsync("/api/vocabulary/mine", Options(token));
        response.Status.Should().Be(200, await response.TextAsync());
        return (await response.JsonAsync())!.Value.EnumerateArray().Select(w => w.GetProperty("id").GetGuid()).ToList();
    }

    private static string OutcomeOf(JsonElement results, Guid learnerId) =>
        results.EnumerateArray().Single(r => r.GetProperty("learnerId").GetGuid() == learnerId).GetProperty("outcome").GetString()!;

    /// <summary>Member status in a group detail, or "Absent" when the learner is not listed.</summary>
    private static string StatusOf(JsonElement group, Guid learnerId)
    {
        foreach (JsonElement member in group.GetProperty("members").EnumerateArray())
        {
            if (member.GetProperty("learnerId").GetGuid() == learnerId)
            {
                return member.GetProperty("status").GetString()!;
            }
        }

        return "Absent";
    }

    // ---- shared helpers ----

    private static async Task<string> InviteAsync(IAPIRequestContext identity, string learnerToken)
    {
        IAPIResponse response = await identity.PostAsync("/api/auth/support-links/invitations", new APIRequestContextOptions
        {
            Headers = Auth(learnerToken),
            DataObject = new { inviteAs = "Learner", relationship = (string?)null },
        });
        response.Status.Should().Be(201, await response.TextAsync());
        return (await response.JsonAsync())!.Value.GetProperty("code").GetString()!;
    }

    private static async Task<JsonElement> AcceptAsync(IAPIRequestContext identity, string supporterToken, string code)
    {
        IAPIResponse response = await identity.PostAsync("/api/auth/support-links/accept", new APIRequestContextOptions
        {
            Headers = Auth(supporterToken),
            DataObject = new { code, token = (string?)null },
        });
        response.Status.Should().Be(200, await response.TextAsync());
        return (await response.JsonAsync())!.Value.Clone();
    }

    private static async Task<(string Token, Guid UserId)> RegisterAsync(IAPIRequestContext identity, string ageGroup)
    {
        IAPIResponse response = await identity.PostAsync("/api/auth/register", new APIRequestContextOptions
        {
            DataObject = new
            {
                email = $"group-e2e-{Guid.NewGuid():N}@example.com",
                password = Password,
                displayName = "E2E Group User",
                ageGroup,
            },
        });
        response.Ok.Should().BeTrue($"self-registration should succeed, got {response.Status}: {await response.TextAsync()}");
        JsonElement body = (await response.JsonAsync())!.Value;
        return (body.GetProperty("token").GetString()!, body.GetProperty("user").GetProperty("id").GetGuid());
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

    private static Dictionary<string, string> ClientDate() =>
        new() { [ClientDateHeader] = DateTimeOffset.Now.ToString("yyyy-MM-ddTHH:mm:sszzz") };

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

    private sealed record Scenario(
        string SupporterToken,
        string PrimaryToken,
        string AdultToken,
        Guid AdultId,
        string ChildToken,
        Guid ChildId,
        Guid ChildSupporterLinkId,
        Guid SafeWordId,
        Guid AdultOnlyWordId);

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
