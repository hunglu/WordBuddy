using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using WordBuddy.Content.Api.Models;
using WordBuddy.Content.Application.DTOs;
using WordBuddy.Content.Domain;

namespace WordBuddy.Content.IntegrationTests;

/// <summary>
/// End-to-end coverage of <c>PersonalVocabularyController</c> against a real SQL Server database
/// (migrated fresh per test-class run) — never mocks EF Core, per repo convention.
///
/// Requires a reachable SQL Server instance (local SQL Server Express/LocalDB, or set
/// <c>CONTENT_TEST_CONNECTION_STRING</c>). Not run as part of a normal `dotnet build`/unit-test
/// pass — see the coder's final report for how to run this against a live database.
/// </summary>
[Collection(ContentApiCollection.Name)]
public sealed class PersonalVocabularyEndpointsTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly ContentApiFactory _factory;

    public PersonalVocabularyEndpointsTests(ContentApiFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClient(Guid userId, string ageGroup, bool isAdmin)
    {
        HttpClient client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestJwtTokenFactory.CreateToken(userId, ageGroup, isAdmin));
        return client;
    }

    [Fact]
    public async Task AddShareModerateApprove_WordAppearsInSharedPool()
    {
        Guid ownerId = Guid.NewGuid();
        Guid adminId = Guid.NewGuid();
        HttpClient owner = CreateClient(ownerId, "Adult", isAdmin: false);
        HttpClient admin = CreateClient(adminId, "Adult", isAdmin: true);

        HttpResponseMessage addResponse = await owner.PostAsJsonAsync(
            "/api/vocabulary", new AddPersonalVocabularyWordRequest("apple", "a fruit", "I ate an apple."));
        addResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        Guid wordId = await addResponse.Content.ReadFromJsonAsync<Guid>();

        HttpResponseMessage mineResponse = await owner.GetAsync("/api/vocabulary/mine");
        mineResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        List<PersonalVocabularyWordDto>? mine = await mineResponse.Content.ReadFromJsonAsync<List<PersonalVocabularyWordDto>>(JsonOptions);
        mine.Should().Contain(w => w.Id == wordId && w.ShareStatus == VocabularyShareStatus.Private);

        HttpResponseMessage shareResponse = await owner.PostAsync($"/api/vocabulary/{wordId}/share", content: null);
        shareResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        HttpResponseMessage moderateResponse = await admin.PostAsJsonAsync(
            $"/api/vocabulary/moderation/{wordId}", new ModerateVocabularyWordRequest(Approve: true, VisibleToChildren: true));
        moderateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        HttpResponseMessage sharedResponse = await owner.GetAsync("/api/vocabulary/shared");
        sharedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        List<PersonalVocabularyWordDto>? shared = await sharedResponse.Content.ReadFromJsonAsync<List<PersonalVocabularyWordDto>>(JsonOptions);
        shared.Should().Contain(w => w.Id == wordId && w.VisibleToChildren);
    }

    [Fact]
    public async Task GetSharedWords_ChildCaller_NeverReceivesNonVisibleToChildrenItem()
    {
        Guid ownerId = Guid.NewGuid();
        Guid adminId = Guid.NewGuid();
        HttpClient owner = CreateClient(ownerId, "Adult", isAdmin: false);
        HttpClient admin = CreateClient(adminId, "Adult", isAdmin: true);
        HttpClient child = CreateClient(Guid.NewGuid(), "Child", isAdmin: false);

        HttpResponseMessage addResponse = await owner.PostAsJsonAsync(
            "/api/vocabulary", new AddPersonalVocabularyWordRequest("mature-topic", "not for kids", null));
        Guid wordId = await addResponse.Content.ReadFromJsonAsync<Guid>();

        await owner.PostAsync($"/api/vocabulary/{wordId}/share", content: null);
        await admin.PostAsJsonAsync($"/api/vocabulary/moderation/{wordId}", new ModerateVocabularyWordRequest(Approve: true, VisibleToChildren: false));

        HttpResponseMessage childSharedResponse = await child.GetAsync("/api/vocabulary/shared");
        childSharedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        List<PersonalVocabularyWordDto>? childShared = await childSharedResponse.Content.ReadFromJsonAsync<List<PersonalVocabularyWordDto>>(JsonOptions);
        childShared.Should().NotContain(w => w.Id == wordId);
    }

    [Fact]
    public async Task ModerationEndpoint_NonAdminCaller_ReturnsForbidden()
    {
        HttpClient nonAdmin = CreateClient(Guid.NewGuid(), "Adult", isAdmin: false);

        HttpResponseMessage response = await nonAdmin.GetAsync("/api/vocabulary/moderation/pending");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ShareAndDelete_NonOwnerCaller_ReturnsNotFound()
    {
        HttpClient owner = CreateClient(Guid.NewGuid(), "Adult", isAdmin: false);
        HttpClient otherUser = CreateClient(Guid.NewGuid(), "Adult", isAdmin: false);

        HttpResponseMessage addResponse = await owner.PostAsJsonAsync(
            "/api/vocabulary", new AddPersonalVocabularyWordRequest("apple", "a fruit", null));
        Guid wordId = await addResponse.Content.ReadFromJsonAsync<Guid>();

        HttpResponseMessage shareAttempt = await otherUser.PostAsync($"/api/vocabulary/{wordId}/share", content: null);
        shareAttempt.StatusCode.Should().Be(HttpStatusCode.NotFound);

        HttpResponseMessage deleteAttempt = await otherUser.DeleteAsync($"/api/vocabulary/{wordId}");
        deleteAttempt.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
