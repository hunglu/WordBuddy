using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;

namespace WordBuddy.Content.IntegrationTests;

/// <summary>WB-56: the <c>ManageContent</c> policy (admin, or adult moderator) on every content-work endpoint.
/// Each case calls an endpoint with fake ids: an allowed caller gets past authorization (any status but 401/403),
/// a refused caller gets 403.</summary>
[Collection(ContentApiCollection.Name)]
public sealed class ManageContentPolicyTests
{
    private readonly ContentApiFactory _factory;

    public ManageContentPolicyTests(ContentApiFactory factory)
    {
        _factory = factory;
    }

    private async Task<HttpClient> ClientAsync(string ageGroup, bool isAdmin, bool isModerator)
    {
        Guid id = Guid.NewGuid();
        if (ageGroup == "Child")
        {
            await SupportLinkTestSeed.SeedActiveSupporterAsync(_factory.Services, id);
        }

        HttpClient client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestJwtTokenFactory.CreateToken(id, ageGroup, isAdmin, isModerator));
        return client;
    }

    private static Task<HttpResponseMessage> CallAsync(HttpClient client, string method, string url) =>
        method == "GET"
            ? client.GetAsync(url)
            : client.PostAsJsonAsync(url, new { approve = true, visibleToChildren = false });

    public static TheoryData<string, string> ManageContentEndpoints() => new()
    {
        { "GET", "/api/lessons/admin" },
        { "GET", "/api/vocabulary/admin/lexemes" },
        { "GET", "/api/vocabulary/moderation/pending" },
        { "POST", "/api/vocabulary/moderation/00000000-0000-0000-0000-000000000001" },
        { "GET", "/api/vocabulary/moderation/autofill-pending" },
        { "POST", "/api/vocabulary/moderation/autofill/00000000-0000-0000-0000-000000000001/approve" },
    };

    [Theory]
    [MemberData(nameof(ManageContentEndpoints))]
    public async Task ManageContentEndpoint_AdultModeratorAndAdmin_PassAuthorization(string method, string url)
    {
        HttpClient moderator = await ClientAsync("Adult", isAdmin: false, isModerator: true);
        HttpClient admin = await ClientAsync("Adult", isAdmin: true, isModerator: false);

        HttpStatusCode moderatorStatus = (await CallAsync(moderator, method, url)).StatusCode;
        HttpStatusCode adminStatus = (await CallAsync(admin, method, url)).StatusCode;

        moderatorStatus.Should().NotBe(HttpStatusCode.Forbidden).And.NotBe(HttpStatusCode.Unauthorized);
        adminStatus.Should().NotBe(HttpStatusCode.Forbidden).And.NotBe(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [MemberData(nameof(ManageContentEndpoints))]
    public async Task ManageContentEndpoint_ChildWithModeratorClaimAndPlainLearner_Get403(string method, string url)
    {
        HttpClient child = await ClientAsync("Child", isAdmin: false, isModerator: true);
        HttpClient learner = await ClientAsync("Adult", isAdmin: false, isModerator: false);

        (await CallAsync(child, method, url)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await CallAsync(learner, method, url)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task LearnerWordsRepublish_Moderator_Gets403AndAdminPasses()
    {
        HttpClient moderator = await ClientAsync("Adult", isAdmin: false, isModerator: true);
        HttpClient admin = await ClientAsync("Adult", isAdmin: true, isModerator: false);

        (await moderator.PostAsync("/api/vocabulary/admin/learner-words/republish", null)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await admin.PostAsync("/api/vocabulary/admin/learner-words/republish", null)).StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }
}
