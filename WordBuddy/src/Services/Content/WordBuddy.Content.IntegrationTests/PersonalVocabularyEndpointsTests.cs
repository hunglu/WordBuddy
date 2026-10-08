using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WordBuddy.Content.Api.Models;
using WordBuddy.Content.Application.DTOs;
using WordBuddy.Content.Domain;
using WordBuddy.Content.Infrastructure.Persistence;

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

    private async Task<HttpClient> CreateClientAsync(Guid userId, string ageGroup, bool isAdmin)
    {
        if (ageGroup == "Child")
        {
            // WB-24: a child needs an active supporter for learning endpoints.
            await SupportLinkTestSeed.SeedActiveSupporterAsync(_factory.Services, userId);
        }

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
        HttpClient owner = await CreateClientAsync(ownerId, "Adult", isAdmin: false);
        HttpClient admin = await CreateClientAsync(adminId, "Adult", isAdmin: true);

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
        HttpClient owner = await CreateClientAsync(ownerId, "Adult", isAdmin: false);
        HttpClient admin = await CreateClientAsync(adminId, "Adult", isAdmin: true);
        HttpClient child = await CreateClientAsync(Guid.NewGuid(), "Child", isAdmin: false);

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
        HttpClient nonAdmin = await CreateClientAsync(Guid.NewGuid(), "Adult", isAdmin: false);

        HttpResponseMessage response = await nonAdmin.GetAsync("/api/vocabulary/moderation/pending");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ShareAndDelete_NonOwnerCaller_ReturnsNotFound()
    {
        HttpClient owner = await CreateClientAsync(Guid.NewGuid(), "Adult", isAdmin: false);
        HttpClient otherUser = await CreateClientAsync(Guid.NewGuid(), "Adult", isAdmin: false);

        HttpResponseMessage addResponse = await owner.PostAsJsonAsync(
            "/api/vocabulary", new AddPersonalVocabularyWordRequest("apple", "a fruit", null));
        Guid wordId = await addResponse.Content.ReadFromJsonAsync<Guid>();

        HttpResponseMessage shareAttempt = await otherUser.PostAsync($"/api/vocabulary/{wordId}/share", content: null);
        shareAttempt.StatusCode.Should().Be(HttpStatusCode.NotFound);

        HttpResponseMessage deleteAttempt = await otherUser.DeleteAsync($"/api/vocabulary/{wordId}");
        deleteAttempt.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static string UniqueWord(string prefix) => $"{prefix}-{Guid.NewGuid():N}";

    private static async Task<Guid> AddAsync(HttpClient client, string word)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/vocabulary", new AddPersonalVocabularyWordRequest(word, "a definition", null));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    private static async Task<List<PersonalVocabularyWordDto>> GetListAsync(HttpClient client, string url)
    {
        HttpResponseMessage response = await client.GetAsync(url);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<List<PersonalVocabularyWordDto>>(JsonOptions))!;
    }

    /// <summary>The owner adds and shares a word; an admin approves it when <paramref name="approve"/>.</summary>
    private async Task<Guid> AddAndShareAsync(HttpClient owner, string word, bool approve, bool visibleToChildren = true)
    {
        Guid id = await AddAsync(owner, word);
        (await owner.PostAsync($"/api/vocabulary/{id}/share", content: null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        if (approve)
        {
            HttpClient admin = await CreateClientAsync(Guid.NewGuid(), "Adult", isAdmin: true);
            (await admin.PostAsJsonAsync($"/api/vocabulary/moderation/{id}", new ModerateVocabularyWordRequest(Approve: true, VisibleToChildren: visibleToChildren)))
                .StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        return id;
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task PersonalVocabularyEndpoints_Delete_Returns409WithoutConfirm(bool approved)
    {
        HttpClient owner = await CreateClientAsync(Guid.NewGuid(), "Adult", isAdmin: false);
        Guid id = await AddAndShareAsync(owner, UniqueWord("confirm"), approve: approved);

        HttpResponseMessage response = await owner.DeleteAsync($"/api/vocabulary/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await response.Content.ReadAsStringAsync()).Should().Contain("PersonalVocabularyWord.DeleteConfirmationRequired");
        (await GetListAsync(owner, "/api/vocabulary/mine")).Should().Contain(w => w.Id == id);
    }

    [Fact]
    public async Task PersonalVocabularyEndpoints_Delete_TransfersSharedWordToSystem()
    {
        Guid ownerId = Guid.NewGuid();
        HttpClient owner = await CreateClientAsync(ownerId, "Adult", isAdmin: false);
        HttpClient adopter = await CreateClientAsync(Guid.NewGuid(), "Adult", isAdmin: false);
        Guid id = await AddAndShareAsync(owner, UniqueWord("transfer"), approve: true);
        (await adopter.PostAsync($"/api/vocabulary/shared/{id}/add-to-mine", content: null)).StatusCode.Should().Be(HttpStatusCode.Created);
        // Warm the pool cache so the test proves the delete invalidates it.
        (await GetListAsync(owner, "/api/vocabulary/shared")).Should().Contain(w => w.Id == id && w.IsMine);

        (await owner.DeleteAsync($"/api/vocabulary/{id}?confirm=true")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await GetListAsync(owner, "/api/vocabulary/mine")).Should().NotContain(w => w.Id == id);
        PersonalVocabularyWordDto pooled = (await GetListAsync(owner, "/api/vocabulary/shared")).Should().ContainSingle(w => w.Id == id).Subject;
        pooled.IsMine.Should().BeFalse();
        pooled.OwnerUserId.Should().Be(SystemOwner.UserId);
        (await GetListAsync(adopter, "/api/vocabulary/mine")).Should().Contain(w => w.Id == id);
    }

    [Fact]
    public async Task PersonalVocabularyEndpoints_AddSharedToMine_FormerOwnerGetsAdopterLink()
    {
        HttpClient owner = await CreateClientAsync(Guid.NewGuid(), "Adult", isAdmin: false);
        Guid id = await AddAndShareAsync(owner, UniqueWord("readd"), approve: true);
        (await owner.DeleteAsync($"/api/vocabulary/{id}?confirm=true")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await owner.PostAsync($"/api/vocabulary/shared/{id}/add-to-mine", content: null)).StatusCode.Should().Be(HttpStatusCode.Created);

        (await GetListAsync(owner, "/api/vocabulary/mine")).Should().ContainSingle(w => w.Id == id).Which.IsAuthor.Should().BeFalse();
    }

    [Fact]
    public async Task PersonalVocabularyEndpoints_Delete_PendingReviewConfirmed_LeavesModerationQueue()
    {
        HttpClient owner = await CreateClientAsync(Guid.NewGuid(), "Adult", isAdmin: false);
        HttpClient admin = await CreateClientAsync(Guid.NewGuid(), "Adult", isAdmin: true);
        string word = UniqueWord("pending");
        Guid id = await AddAndShareAsync(owner, word, approve: false);
        (await GetListAsync(admin, "/api/vocabulary/moderation/pending")).Should().Contain(w => w.Id == id);

        (await owner.DeleteAsync($"/api/vocabulary/{id}?confirm=true")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await GetListAsync(admin, "/api/vocabulary/moderation/pending")).Should().NotContain(w => w.Id == id);
        (await GetListAsync(owner, "/api/vocabulary/mine")).Should().NotContain(w => w.Id == id);
        // Re-adding creates a new row, proving the orphaned word was deleted.
        (await AddAsync(owner, word)).Should().NotBe(id);
    }

    [Fact]
    public async Task PersonalVocabularyEndpoints_SharedPool_HidesTransferredNonChildSafeWordFromChild()
    {
        HttpClient owner = await CreateClientAsync(Guid.NewGuid(), "Adult", isAdmin: false);
        HttpClient child = await CreateClientAsync(Guid.NewGuid(), "Child", isAdmin: false);
        string word = UniqueWord("adultonly");
        Guid id = await AddAndShareAsync(owner, word, approve: true, visibleToChildren: false);
        (await owner.DeleteAsync($"/api/vocabulary/{id}?confirm=true")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await GetListAsync(child, "/api/vocabulary/shared")).Should().NotContain(w => w.Id == id);
        (await child.PostAsync($"/api/vocabulary/shared/{id}/add-to-mine", content: null)).StatusCode.Should().Be(HttpStatusCode.NotFound);

        // Typing the same word must not link the child to the (now system-owned) hidden word.
        (await AddAsync(child, word)).Should().NotBe(id);
    }

    [Fact]
    public async Task PersonalVocabularyEndpoints_SharedPool_IsMineTrueOnlyForOwner()
    {
        Guid ownerId = Guid.NewGuid();
        HttpClient owner = await CreateClientAsync(ownerId, "Adult", isAdmin: false);
        HttpClient other = await CreateClientAsync(Guid.NewGuid(), "Adult", isAdmin: false);
        Guid id = await AddAndShareAsync(owner, UniqueWord("ismine"), approve: true);

        List<PersonalVocabularyWordDto> ownerPool = await GetListAsync(owner, "/api/vocabulary/shared");
        List<PersonalVocabularyWordDto> otherPool = await GetListAsync(other, "/api/vocabulary/shared");

        ownerPool.Should().ContainSingle(w => w.Id == id).Which.IsMine.Should().BeTrue();
        ownerPool.Where(w => w.IsMine).Should().OnlyContain(w => w.OwnerUserId == ownerId);
        otherPool.Should().ContainSingle(w => w.Id == id).Which.IsMine.Should().BeFalse();
    }
    private static readonly string[] WordJsonProperties =
        ["id", "ownerUserId", "word", "definition", "example", "shareStatus", "visibleToChildren", "createdAtUtc", "isAuthor", "isMine"];

    private static async Task<JsonElement[]> GetJsonArrayAsync(HttpClient client, string url)
    {
        HttpResponseMessage response = await client.GetAsync(url);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.EnumerateArray().Select(e => e.Clone()).ToArray();
    }

    private async Task<(int Lexemes, int Senses)> CountLexemesAndSensesAsync(string word)
    {
        string normalized = Sense.NormalizeWord(word);
        using IServiceScope scope = _factory.Services.CreateScope();
        ContentDbContext dbContext = scope.ServiceProvider.GetRequiredService<ContentDbContext>();
        int lexemes = await dbContext.Lexemes.CountAsync(l => l.NormalizedLemma == normalized && l.PartOfSpeech == null);
        int senses = await dbContext.Senses.CountAsync(s => s.Word.Trim() == word.Trim());
        return (lexemes, senses);
    }

    [Fact]
    public async Task PersonalVocabularyEndpoints_GetMine_JsonShapeUnchanged()
    {
        HttpClient owner = await CreateClientAsync(Guid.NewGuid(), "Adult", isAdmin: false);
        Guid id = await AddAsync(owner, UniqueWord("shape-mine"));

        JsonElement[] items = await GetJsonArrayAsync(owner, "/api/vocabulary/mine");

        JsonElement item = items.Should().ContainSingle(i => i.GetProperty("id").GetGuid() == id).Subject;
        item.EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo(WordJsonProperties);
        item.GetProperty("isAuthor").GetBoolean().Should().BeTrue();
        item.GetProperty("isMine").GetBoolean().Should().BeFalse();
    }

    [Theory]
    [InlineData("Adult")]
    [InlineData("Child")]
    public async Task PersonalVocabularyEndpoints_GetShared_JsonShapeUnchangedAndChildSeesOnlyChildVisible(string ageGroup)
    {
        HttpClient owner = await CreateClientAsync(Guid.NewGuid(), "Adult", isAdmin: false);
        HttpClient caller = await CreateClientAsync(Guid.NewGuid(), ageGroup, isAdmin: false);
        Guid childSafe = await AddAndShareAsync(owner, UniqueWord("shape-safe"), approve: true, visibleToChildren: true);
        Guid adultOnly = await AddAndShareAsync(owner, UniqueWord("shape-adult"), approve: true, visibleToChildren: false);

        JsonElement[] items = await GetJsonArrayAsync(caller, "/api/vocabulary/shared");

        items.Should().NotBeEmpty();
        items.Should().OnlyContain(i => i.EnumerateObject().Select(p => p.Name).ToHashSet().SetEquals(WordJsonProperties));
        items.Should().Contain(i => i.GetProperty("id").GetGuid() == childSafe);
        if (ageGroup == "Child")
        {
            items.Should().OnlyContain(i => i.GetProperty("visibleToChildren").GetBoolean());
            items.Should().NotContain(i => i.GetProperty("id").GetGuid() == adultOnly);
        }
        else
        {
            items.Should().Contain(i => i.GetProperty("id").GetGuid() == adultOnly);
        }
    }

    [Fact]
    public async Task AddPersonalVocabularyWord_Post_ReusesNullPosLexemeForSameNormalizedWord()
    {
        string suffix = Guid.NewGuid().ToString("N");
        HttpClient first = await CreateClientAsync(Guid.NewGuid(), "Adult", isAdmin: false);
        HttpClient second = await CreateClientAsync(Guid.NewGuid(), "Adult", isAdmin: false);

        HttpResponseMessage a = await first.PostAsJsonAsync("/api/vocabulary", new AddPersonalVocabularyWordRequest($"Pear{suffix}", "a fruit", null));
        HttpResponseMessage b = await second.PostAsJsonAsync("/api/vocabulary", new AddPersonalVocabularyWordRequest($" pear{suffix}", "a soft fruit", null));

        a.StatusCode.Should().Be(HttpStatusCode.Created);
        b.StatusCode.Should().Be(HttpStatusCode.Created);
        using IServiceScope scope = _factory.Services.CreateScope();
        ContentDbContext dbContext = scope.ServiceProvider.GetRequiredService<ContentDbContext>();
        string normalized = Sense.NormalizeWord($"pear{suffix}");
        List<Guid> lexemeIds = await dbContext.Lexemes
            .Where(l => l.NormalizedLemma == normalized)
            .Select(l => l.Id)
            .ToListAsync();
        lexemeIds.Should().ContainSingle();
        (await dbContext.Lexemes.SingleAsync(l => l.Id == lexemeIds[0])).PartOfSpeech.Should().BeNull();
        (await dbContext.Senses.CountAsync(s => s.LexemeId == lexemeIds[0])).Should().Be(2);
    }

    [Fact]
    public async Task DeletePersonalVocabularyWord_Delete_RemovesOrphanLexemeOnly()
    {
        string word = UniqueWord("orphan");
        HttpClient first = await CreateClientAsync(Guid.NewGuid(), "Adult", isAdmin: false);
        HttpClient second = await CreateClientAsync(Guid.NewGuid(), "Adult", isAdmin: false);
        Guid firstId = await AddAsync(first, word);
        HttpResponseMessage secondAdd = await second.PostAsJsonAsync("/api/vocabulary", new AddPersonalVocabularyWordRequest(word, "another meaning", null));
        Guid secondId = await secondAdd.Content.ReadFromJsonAsync<Guid>();
        (await CountLexemesAndSensesAsync(word)).Should().Be((1, 2));

        (await first.DeleteAsync($"/api/vocabulary/{firstId}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await CountLexemesAndSensesAsync(word)).Should().Be((1, 1), "the lexeme is still used by the other sense");

        (await second.DeleteAsync($"/api/vocabulary/{secondId}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await CountLexemesAndSensesAsync(word)).Should().Be((0, 0), "the last sense's lexeme is removed with it");
    }

    [Fact]
    public async Task GetSensesByIds_ChildAndAdult_HiddenForeignAndUnknownIdsOmittedWithSame200()
    {
        HttpClient author = await CreateClientAsync(Guid.NewGuid(), "Adult", isAdmin: false);
        HttpClient admin = await CreateClientAsync(Guid.NewGuid(), "Adult", isAdmin: true);
        HttpClient stranger = await CreateClientAsync(Guid.NewGuid(), "Adult", isAdmin: false);
        HttpClient adult = await CreateClientAsync(Guid.NewGuid(), "Adult", isAdmin: false);
        HttpClient child = await CreateClientAsync(Guid.NewGuid(), "Child", isAdmin: false);

        Guid hiddenSharedId = await (await author.PostAsJsonAsync(
            "/api/vocabulary", new AddPersonalVocabularyWordRequest($"grownup-{Guid.NewGuid():N}", "adult only", null)))
            .Content.ReadFromJsonAsync<Guid>();
        await author.PostAsync($"/api/vocabulary/{hiddenSharedId}/share", content: null);
        await admin.PostAsJsonAsync($"/api/vocabulary/moderation/{hiddenSharedId}", new ModerateVocabularyWordRequest(Approve: true, VisibleToChildren: false));

        Guid foreignPrivateId = await (await stranger.PostAsJsonAsync(
            "/api/vocabulary", new AddPersonalVocabularyWordRequest($"secret-{Guid.NewGuid():N}", "private", null)))
            .Content.ReadFromJsonAsync<Guid>();

        Guid unknownId = Guid.NewGuid();
        string url = $"/api/vocabulary/senses?ids={hiddenSharedId}&ids={foreignPrivateId}&ids={unknownId}";

        HttpResponseMessage adultResponse = await adult.GetAsync(url);
        HttpResponseMessage childResponse = await child.GetAsync(url);

        adultResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        childResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        List<SenseReviewDto>? adultSenses = await adultResponse.Content.ReadFromJsonAsync<List<SenseReviewDto>>(JsonOptions);
        List<SenseReviewDto>? childSenses = await childResponse.Content.ReadFromJsonAsync<List<SenseReviewDto>>(JsonOptions);

        adultSenses.Should().ContainSingle().Which.SenseId.Should().Be(hiddenSharedId);
        childSenses.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSensesByIds_SenseWithAudioAndImage_ReturnsBothUrls()
    {
        HttpClient owner = await CreateClientAsync(Guid.NewGuid(), "Adult", isAdmin: false);
        Guid senseId = await AddAsync(owner, UniqueWord("media"));
        string audioUrl = $"https://media.test/audio-{Guid.NewGuid():N}.mp3";
        string imageUrl = $"https://media.test/image-{Guid.NewGuid():N}.png";

        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            ContentDbContext dbContext = scope.ServiceProvider.GetRequiredService<ContentDbContext>();
            MediaAsset audio = new(Guid.NewGuid(), MediaAssetType.Audio, audioUrl);
            MediaAsset image = new(Guid.NewGuid(), MediaAssetType.Image, imageUrl);
            dbContext.MediaAssets.AddRange(audio, image);
            await dbContext.SaveChangesAsync();
            await dbContext.Senses.Where(s => s.Id == senseId).ExecuteUpdateAsync(u => u
                .SetProperty(s => s.AudioAssetId, audio.Id)
                .SetProperty(s => s.ImageAssetId, image.Id));
        }

        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            ContentDbContext dbContext = scope.ServiceProvider.GetRequiredService<ContentDbContext>();
            Sense stored = await dbContext.Senses.AsNoTracking().SingleAsync(s => s.Id == senseId);
            stored.AudioAssetId.Should().NotBeNull("the arrange step must persist the audio link");
            stored.ImageAssetId.Should().NotBeNull("the arrange step must persist the image link");
        }

        HttpResponseMessage response = await owner.GetAsync($"/api/vocabulary/senses?ids={senseId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        List<SenseReviewDto>? senses = await response.Content.ReadFromJsonAsync<List<SenseReviewDto>>(JsonOptions);
        SenseReviewDto dto = senses.Should().ContainSingle().Subject;
        new[] { dto.AudioUrl, dto.ImageUrl }.Should().Equal(audioUrl, imageUrl);
    }

    [Fact]
    public async Task GetSensesByIds_MoreThan100Ids_ReturnsBadRequest()
    {
        HttpClient adult = await CreateClientAsync(Guid.NewGuid(), "Adult", isAdmin: false);
        string query = string.Join("&", Enumerable.Range(0, 101).Select(_ => $"ids={Guid.NewGuid()}"));

        HttpResponseMessage response = await adult.GetAsync($"/api/vocabulary/senses?{query}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
