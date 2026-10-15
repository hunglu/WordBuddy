using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WordBuddy.Content.Api.Models;
using WordBuddy.Content.Domain;
using WordBuddy.Content.Infrastructure.Persistence;

namespace WordBuddy.Content.IntegrationTests;

/// <summary>WB-55 paging on the Content list endpoints, against real SQL Server: the response keys,
/// page content, validation (400), and the child versus adult total of the shared pool.</summary>
[Collection(ContentApiCollection.Name)]
public sealed class PagingEndpointsTests
{
    private readonly ContentApiFactory _factory;

    public PagingEndpointsTests(ContentApiFactory factory)
    {
        _factory = factory;
    }

    private async Task<HttpClient> ClientAsync(string ageGroup, bool isAdmin = false, Guid? userId = null)
    {
        Guid id = userId ?? Guid.NewGuid();
        if (ageGroup == "Child")
        {
            await SupportLinkTestSeed.SeedActiveSupporterAsync(_factory.Services, id);
        }

        HttpClient client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestJwtTokenFactory.CreateToken(id, ageGroup, isAdmin));
        return client;
    }

    private static async Task<JsonElement> GetJsonAsync(HttpClient client, string url)
    {
        HttpResponseMessage response = await client.GetAsync(url);
        string body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, body);
        using JsonDocument document = JsonDocument.Parse(body);
        return document.RootElement.Clone();
    }

    private static async Task<Guid> AddWordAsync(HttpClient owner, string word)
    {
        HttpResponseMessage added = await owner.PostAsJsonAsync("/api/vocabulary", new AddPersonalVocabularyWordRequest(word, "a definition", null));
        added.StatusCode.Should().Be(HttpStatusCode.Created);
        return await added.Content.ReadFromJsonAsync<Guid>();
    }

    private async Task<Guid> ShareAndApproveAsync(string word, bool visibleToChildren)
    {
        HttpClient owner = await ClientAsync("Adult");
        HttpClient admin = await ClientAsync("Adult", isAdmin: true);
        Guid id = await AddWordAsync(owner, word);
        (await owner.PostAsync($"/api/vocabulary/{id}/share", content: null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await admin.PostAsJsonAsync($"/api/vocabulary/moderation/{id}", new ModerateVocabularyWordRequest(true, visibleToChildren)))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        return id;
    }

    // ---- admin lessons

    [Fact]
    public async Task AdminLessons_PageTwoWithFilter_ReturnsRemainderAndTotal()
    {
        HttpClient admin = await ClientAsync("Adult", isAdmin: true);
        string prefix = $"Paging{Guid.NewGuid():N}"[..14];
        for (int i = 0; i < 3; i++)
        {
            HttpResponseMessage created = await admin.PostAsJsonAsync("/api/lessons/admin", new
            {
                title = $"{prefix}-{i}",
                description = "Paging test.",
                type = "Grammar",
                level = "Advanced",
                targetAgeGroup = "Adult",
            });
            created.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        JsonElement page = await GetJsonAsync(admin, "/api/lessons/admin?type=Grammar&level=Advanced&isPublished=false&page=2&pageSize=2");

        page.GetProperty("page").GetInt32().Should().Be(2);
        page.GetProperty("pageSize").GetInt32().Should().Be(2);
        page.GetProperty("totalCount").GetInt32().Should().BeGreaterThanOrEqualTo(3);
        page.GetProperty("items").GetArrayLength().Should().BeInRange(1, 2);
    }

    [Theory]
    [InlineData("pageSize=101")]
    [InlineData("pageSize=0")]
    [InlineData("page=0")]
    public async Task AdminLessons_InvalidPaging_Returns400(string query)
    {
        HttpClient admin = await ClientAsync("Adult", isAdmin: true);

        HttpResponseMessage response = await admin.GetAsync($"/api/lessons/admin?{query}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ---- moderation

    [Fact]
    public async Task ModerationPending_Admin_ReturnsPagedShape()
    {
        HttpClient owner = await ClientAsync("Adult");
        HttpClient admin = await ClientAsync("Adult", isAdmin: true);
        Guid id = await AddWordAsync(owner, $"pend-{Guid.NewGuid():N}");
        (await owner.PostAsync($"/api/vocabulary/{id}/share", content: null)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        JsonElement page = await GetJsonAsync(admin, "/api/vocabulary/moderation/pending?pageSize=1");

        page.GetProperty("items").GetArrayLength().Should().Be(1);
        page.GetProperty("pageSize").GetInt32().Should().Be(1);
        page.GetProperty("totalCount").GetInt32().Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task ModerationPending_InvalidPaging_Returns400()
    {
        HttpClient admin = await ClientAsync("Adult", isAdmin: true);

        (await admin.GetAsync("/api/vocabulary/moderation/pending?pageSize=101")).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AutofillPending_BothRoutes_ReturnPagedShape()
    {
        HttpClient admin = await ClientAsync("Adult", isAdmin: true);
        Guid supporterId = Guid.NewGuid();
        Guid childId = Guid.NewGuid();
        await SupportLinkTestSeed.SeedActiveSupporterAsync(_factory.Services, childId, supporterId);
        HttpClient supporter = await ClientAsync("Adult", userId: supporterId);

        JsonElement adminPage = await GetJsonAsync(admin, "/api/vocabulary/moderation/autofill-pending?page=1&pageSize=5");
        JsonElement supporterPage = await GetJsonAsync(supporter, $"/api/vocabulary/learners/{childId}/pending-approvals?page=1&pageSize=5");

        foreach (JsonElement page in new[] { adminPage, supporterPage })
        {
            page.GetProperty("items").ValueKind.Should().Be(JsonValueKind.Array);
            page.GetProperty("page").GetInt32().Should().Be(1);
            page.GetProperty("pageSize").GetInt32().Should().Be(5);
            page.GetProperty("totalCount").GetInt32().Should().BeGreaterThanOrEqualTo(0);
        }
    }

    // ---- my words

    [Theory]
    [InlineData("Adult")]
    [InlineData("Child")]
    public async Task MyWords_PagesNewestFirst_ForAdultAndChild(string ageGroup)
    {
        HttpClient client = await ClientAsync(ageGroup);
        List<Guid> ids = [];
        for (int i = 0; i < 3; i++)
        {
            ids.Add(await AddWordAsync(client, $"mine-{Guid.NewGuid():N}"));
        }

        JsonElement first = await GetJsonAsync(client, "/api/vocabulary/mine?page=1&pageSize=2");
        JsonElement second = await GetJsonAsync(client, "/api/vocabulary/mine?page=2&pageSize=2");
        JsonElement beyond = await GetJsonAsync(client, "/api/vocabulary/mine?page=3&pageSize=2");

        first.GetProperty("totalCount").GetInt32().Should().Be(3);
        first.GetProperty("items").GetArrayLength().Should().Be(2);
        second.GetProperty("items").GetArrayLength().Should().Be(1);
        beyond.GetProperty("items").GetArrayLength().Should().Be(0);
        beyond.GetProperty("totalCount").GetInt32().Should().Be(3);
        List<Guid> seen = first.GetProperty("items").EnumerateArray().Concat(second.GetProperty("items").EnumerateArray())
            .Select(i => i.GetProperty("id").GetGuid()).ToList();
        seen.Should().BeEquivalentTo(ids);
    }

    [Theory]
    [InlineData("page=0")]
    [InlineData("pageSize=0")]
    [InlineData("pageSize=101")]
    public async Task MyWords_InvalidPaging_Returns400(string query)
    {
        HttpClient client = await ClientAsync("Adult");

        (await client.GetAsync($"/api/vocabulary/mine?{query}")).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ---- shared pool

    [Fact]
    public async Task SharedPool_Search_FindsWordAndCountsOnlyMatches()
    {
        string token = $"zq{Guid.NewGuid():N}"[..12];
        await ShareAndApproveAsync($"{token}-one", visibleToChildren: true);
        await ShareAndApproveAsync($"{token}-two", visibleToChildren: true);
        HttpClient adult = await ClientAsync("Adult");

        JsonElement page = await GetJsonAsync(adult, $"/api/vocabulary/shared?search={token.ToUpperInvariant()}&page=1&pageSize=1");

        page.GetProperty("totalCount").GetInt32().Should().Be(2);
        page.GetProperty("items").GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task SharedPool_ChildTotalExcludesWordsHiddenFromChildren()
    {
        string token = $"zk{Guid.NewGuid():N}"[..12];
        await ShareAndApproveAsync($"{token}-kids", visibleToChildren: true);
        await ShareAndApproveAsync($"{token}-adults", visibleToChildren: false);
        HttpClient adult = await ClientAsync("Adult");
        HttpClient child = await ClientAsync("Child");

        JsonElement adultPage = await GetJsonAsync(adult, $"/api/vocabulary/shared?search={token}");
        JsonElement childPage = await GetJsonAsync(child, $"/api/vocabulary/shared?search={token}");

        adultPage.GetProperty("totalCount").GetInt32().Should().Be(2);
        childPage.GetProperty("totalCount").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task SharedPool_InvalidPaging_Returns400()
    {
        HttpClient adult = await ClientAsync("Adult");

        (await adult.GetAsync("/api/vocabulary/shared?pageSize=101")).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await adult.GetAsync("/api/vocabulary/shared?page=0")).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ---- group history

    [Fact]
    public async Task GroupWordHistory_Paged_OrderIsStableAcrossPages()
    {
        Guid owner = Guid.NewGuid();
        Guid groupId = Guid.NewGuid();
        Guid member = Guid.NewGuid();
        await SeedMemberAsync(groupId, owner, member);
        List<Guid> senseIds = [];
        for (int i = 0; i < 3; i++)
        {
            senseIds.Add(await SeedSharedSenseAsync());
        }

        HttpClient ownerClient = await ClientAsync("Adult", userId: owner);
        (await ownerClient.PostAsJsonAsync($"/api/vocabulary/groups/{groupId}/words", new { senseIds })).StatusCode.Should().Be(HttpStatusCode.OK);

        JsonElement first = await GetJsonAsync(ownerClient, $"/api/vocabulary/groups/{groupId}/words?page=1&pageSize=2");
        JsonElement second = await GetJsonAsync(ownerClient, $"/api/vocabulary/groups/{groupId}/words?page=2&pageSize=2");

        first.GetProperty("totalCount").GetInt32().Should().Be(3);
        first.GetProperty("items").GetArrayLength().Should().Be(2);
        second.GetProperty("items").GetArrayLength().Should().Be(1);
        List<Guid> ids = first.GetProperty("items").EnumerateArray().Concat(second.GetProperty("items").EnumerateArray())
            .Select(i => i.GetProperty("id").GetGuid()).ToList();
        ids.Should().OnlyHaveUniqueItems().And.HaveCount(3);
    }

    [Fact]
    public async Task GroupWordHistory_InvalidPaging_Returns400()
    {
        Guid owner = Guid.NewGuid();
        Guid groupId = Guid.NewGuid();
        await SeedMemberAsync(groupId, owner, Guid.NewGuid());
        HttpClient ownerClient = await ClientAsync("Adult", userId: owner);

        (await ownerClient.GetAsync($"/api/vocabulary/groups/{groupId}/words?pageSize=101")).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private async Task<Guid> SeedSharedSenseAsync()
    {
        Guid lexemeId = Guid.NewGuid();
        Guid senseId = Guid.NewGuid();
        string word = $"pg{Guid.NewGuid():N}"[..12];
        using IServiceScope scope = _factory.Services.CreateScope();
        ContentDbContext db = scope.ServiceProvider.GetRequiredService<ContentDbContext>();
        db.Lexemes.Add(Lexeme.Create(lexemeId, word).Value);
        db.Senses.Add(Sense.CreateSystem(senseId, lexemeId, word, "A test meaning.", "A test example."));
        await db.SaveChangesAsync();
        return senseId;
    }

    private async Task SeedMemberAsync(Guid groupId, Guid ownerId, Guid learnerId)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ContentDbContext db = scope.ServiceProvider.GetRequiredService<ContentDbContext>();
        db.LearnerGroupMemberProjections.Add(LearnerGroupMemberProjection.Create(groupId, ownerId, learnerId, isActive: true, DateTime.UtcNow));
        await db.SaveChangesAsync();
        await SupportLinkTestSeed.SeedActiveSupporterAsync(_factory.Services, learnerId, ownerId);
    }
}
