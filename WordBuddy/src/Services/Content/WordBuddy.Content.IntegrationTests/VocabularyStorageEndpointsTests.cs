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
/// Behaviour of the unified <c>VocabularyWords</c> storage through the unchanged HTTP API, for
/// Child and Adult callers: dedupe-on-add, adopt-as-link, unlink-on-delete, author-only sharing,
/// and lesson words served through <c>LessonVocabularyWords</c>. Real SQL Server, never mocked EF.
/// Every test uses its own word text — the class fixture shares one database.
/// </summary>
[Collection(ContentApiCollection.Name)]
public sealed class VocabularyStorageEndpointsTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly ContentApiFactory _factory;

    public VocabularyStorageEndpointsTests(ContentApiFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClient(Guid userId, string ageGroup, bool isAdmin = false)
    {
        HttpClient client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestJwtTokenFactory.CreateToken(userId, ageGroup, isAdmin));
        return client;
    }

    private static string UniqueWord(string prefix) => $"{prefix}-{Guid.NewGuid():N}";

    private static async Task<Guid> AddAsync(HttpClient client, string word, string definition, string? example)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/vocabulary", new AddPersonalVocabularyWordRequest(word, definition, example));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    private static async Task<List<PersonalVocabularyWordDto>> GetListAsync(HttpClient client, string url)
    {
        HttpResponseMessage response = await client.GetAsync(url);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<List<PersonalVocabularyWordDto>>(JsonOptions))!;
    }

    /// <summary>Owner adds, shares, and an admin approves — returns the shared word's id.</summary>
    private async Task<Guid> CreateSharedWordAsync(string word, bool visibleToChildren)
    {
        HttpClient owner = CreateClient(Guid.NewGuid(), "Adult");
        HttpClient admin = CreateClient(Guid.NewGuid(), "Adult", isAdmin: true);

        Guid id = await AddAsync(owner, word, "a shared definition", null);
        (await owner.PostAsync($"/api/vocabulary/{id}/share", content: null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await admin.PostAsJsonAsync($"/api/vocabulary/moderation/{id}", new ModerateVocabularyWordRequest(true, visibleToChildren)))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        return id;
    }

    private async Task<JsonElement> GetSeededVocabularyLessonAsync(HttpClient client)
    {
        List<LessonDto>? lessons = await client.GetFromJsonAsync<List<LessonDto>>("/api/lessons?type=Vocabulary", JsonOptions);
        LessonDto animals = lessons!.Single(l => l.Title == "Animals");

        HttpResponseMessage response = await client.GetAsync($"/api/lessons/{animals.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.Clone();
    }

    [Theory]
    [InlineData("Adult")]
    [InlineData("Child")]
    public async Task GetLessonDetail_VocabularyLesson_ReturnsWordsInOrderWithUnchangedShape(string ageGroup)
    {
        HttpClient client = CreateClient(Guid.NewGuid(), ageGroup);

        JsonElement lesson = await GetSeededVocabularyLessonAsync(client);

        JsonElement[] items = lesson.GetProperty("vocabularyItems").EnumerateArray().ToArray();
        items.Select(i => i.GetProperty("word").GetString()).Should().Equal("Dog", "Cat", "Bird", "Fish");
        items.Should().OnlyContain(i =>
            i.EnumerateObject().Select(p => p.Name).SequenceEqual(new[] { "id", "word", "definition", "example", "audio" }) &&
            !string.IsNullOrEmpty(i.GetProperty("example").GetString()));
    }

    [Fact]
    public async Task AddWord_SameAuthorTwice_ReturnsSameIdAndListsOnce()
    {
        Guid userId = Guid.NewGuid();
        HttpClient client = CreateClient(userId, "Adult");
        string word = UniqueWord("twice");

        Guid first = await AddAsync(client, word, "def", "eg");
        Guid second = await AddAsync(client, $"  {word.ToUpperInvariant()} ", "DEF", "eg ");

        second.Should().Be(first);
        List<PersonalVocabularyWordDto> mine = await GetListAsync(client, "/api/vocabulary/mine");
        mine.Should().ContainSingle(w => w.Id == first)
            .Which.Should().Match<PersonalVocabularyWordDto>(w => w.IsAuthor && w.OwnerUserId == userId && w.ShareStatus == VocabularyShareStatus.Private);
    }

    [Fact]
    public async Task AddWord_TwoLearnersSamePrivateText_KeepsSeparateWords()
    {
        string word = UniqueWord("private");

        Guid a = await AddAsync(CreateClient(Guid.NewGuid(), "Adult"), word, "def", null);
        Guid b = await AddAsync(CreateClient(Guid.NewGuid(), "Adult"), word, "def", null);

        b.Should().NotBe(a);
    }

    [Theory]
    [InlineData("Adult")]
    [InlineData("Child")]
    public async Task AddWord_MatchesSystemLessonWord_LinksToLessonWord(string ageGroup)
    {
        HttpClient client = CreateClient(Guid.NewGuid(), ageGroup);
        JsonElement dog = (await GetSeededVocabularyLessonAsync(client)).GetProperty("vocabularyItems").EnumerateArray()
            .Single(i => i.GetProperty("word").GetString() == "Dog");

        Guid id = await AddAsync(client, "dog", "a common domesticated animal.", "the dog barked loudly.");

        id.Should().Be(dog.GetProperty("id").GetGuid());
        List<PersonalVocabularyWordDto> mine = await GetListAsync(client, "/api/vocabulary/mine");
        mine.Should().ContainSingle(w => w.Id == id).Which.IsAuthor.Should().BeFalse();

        (await client.DeleteAsync($"/api/vocabulary/{id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        JsonElement lessonAfter = await GetSeededVocabularyLessonAsync(client);
        lessonAfter.GetProperty("vocabularyItems").EnumerateArray().Should().Contain(i => i.GetProperty("id").GetGuid() == id);
    }

    [Fact]
    public async Task AddWord_ChildAndChildVisibleSharedText_LinksToSharedWord()
    {
        string word = UniqueWord("kidsafe");
        Guid sharedId = await CreateSharedWordAsync(word, visibleToChildren: true);

        Guid id = await AddAsync(CreateClient(Guid.NewGuid(), "Child"), word, "a shared definition", null);

        id.Should().Be(sharedId);
    }

    [Fact]
    public async Task AddWord_ChildAndNonChildVisibleSharedText_CreatesOwnWord()
    {
        string word = UniqueWord("adultonly");
        Guid sharedId = await CreateSharedWordAsync(word, visibleToChildren: false);
        Guid childId = Guid.NewGuid();
        HttpClient child = CreateClient(childId, "Child");

        Guid id = await AddAsync(child, word, "a shared definition", null);

        id.Should().NotBe(sharedId);
        List<PersonalVocabularyWordDto> mine = await GetListAsync(child, "/api/vocabulary/mine");
        mine.Should().ContainSingle(w => w.Id == id).Which.IsAuthor.Should().BeTrue();
    }

    [Theory]
    [InlineData("Adult")]
    [InlineData("Child")]
    public async Task AdoptSharedWord_ThenDelete_LinksIdempotentlyAndWordStaysInPool(string ageGroup)
    {
        Guid sharedId = await CreateSharedWordAsync(UniqueWord("adopt"), visibleToChildren: true);
        Guid adopterId = Guid.NewGuid();
        HttpClient adopter = CreateClient(adopterId, ageGroup);

        HttpResponseMessage first = await adopter.PostAsync($"/api/vocabulary/shared/{sharedId}/add-to-mine", content: null);
        HttpResponseMessage second = await adopter.PostAsync($"/api/vocabulary/shared/{sharedId}/add-to-mine", content: null);

        first.StatusCode.Should().Be(HttpStatusCode.Created);
        second.StatusCode.Should().Be(HttpStatusCode.Created);
        (await first.Content.ReadFromJsonAsync<Guid>()).Should().Be(sharedId);
        (await second.Content.ReadFromJsonAsync<Guid>()).Should().Be(sharedId);

        List<PersonalVocabularyWordDto> mine = await GetListAsync(adopter, "/api/vocabulary/mine");
        mine.Should().ContainSingle(w => w.Id == sharedId).Which.Should().Match<PersonalVocabularyWordDto>(w =>
            !w.IsAuthor && w.OwnerUserId == adopterId && w.ShareStatus == VocabularyShareStatus.Private && !w.VisibleToChildren);

        List<PersonalVocabularyWordDto> check = await GetListAsync(adopter, "/api/vocabulary/check?count=50");
        check.Should().Contain(w => w.Id == sharedId);

        (await adopter.DeleteAsync($"/api/vocabulary/{sharedId}")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await GetListAsync(adopter, "/api/vocabulary/mine")).Should().NotContain(w => w.Id == sharedId);
        (await GetListAsync(adopter, "/api/vocabulary/shared")).Should().Contain(w => w.Id == sharedId);
        (await adopter.DeleteAsync($"/api/vocabulary/{sharedId}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AdoptSharedWord_ChildAndNonChildVisibleWord_ReturnsNotFound()
    {
        Guid sharedId = await CreateSharedWordAsync(UniqueWord("notforkids"), visibleToChildren: false);

        HttpResponseMessage response = await CreateClient(Guid.NewGuid(), "Child")
            .PostAsync($"/api/vocabulary/shared/{sharedId}/add-to-mine", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RequestShare_AdoptedWord_ReturnsConflict()
    {
        Guid sharedId = await CreateSharedWordAsync(UniqueWord("reshare"), visibleToChildren: true);
        HttpClient adopter = CreateClient(Guid.NewGuid(), "Adult");
        await adopter.PostAsync($"/api/vocabulary/shared/{sharedId}/add-to-mine", content: null);

        HttpResponseMessage response = await adopter.PostAsync($"/api/vocabulary/{sharedId}/share", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task DeleteWord_AuthorOfSharedWord_RemovesFromMineButKeepsInPool()
    {
        Guid ownerId = Guid.NewGuid();
        HttpClient owner = CreateClient(ownerId, "Adult");
        HttpClient admin = CreateClient(Guid.NewGuid(), "Adult", isAdmin: true);
        Guid id = await AddAsync(owner, UniqueWord("authorshared"), "def", null);
        await owner.PostAsync($"/api/vocabulary/{id}/share", content: null);
        await admin.PostAsJsonAsync($"/api/vocabulary/moderation/{id}", new ModerateVocabularyWordRequest(true, true));

        (await owner.DeleteAsync($"/api/vocabulary/{id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await GetListAsync(owner, "/api/vocabulary/mine")).Should().NotContain(w => w.Id == id);
        (await GetListAsync(owner, "/api/vocabulary/shared")).Should().Contain(w => w.Id == id);
    }

    [Fact]
    public async Task DeleteWord_AuthorOfPrivateOrphan_DeletesWord()
    {
        HttpClient owner = CreateClient(Guid.NewGuid(), "Adult");
        string word = UniqueWord("orphan");
        Guid id = await AddAsync(owner, word, "def", null);

        (await owner.DeleteAsync($"/api/vocabulary/{id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Re-adding creates a brand-new word, proving the old row was deleted (not just unlinked).
        Guid again = await AddAsync(owner, word, "def", null);
        again.Should().NotBe(id);
    }
}
