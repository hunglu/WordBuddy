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
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Content.Domain;
using WordBuddy.Content.Infrastructure.Persistence;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.IntegrationTests;

/// <summary>WB-48: <c>PUT api/vocabulary/{id}</c> against a real SQL Server database.</summary>
[Collection(ContentApiCollection.Name)]
public sealed class PersonalVocabularyEditEndpointsTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly ContentApiFactory _factory;

    public PersonalVocabularyEditEndpointsTests(ContentApiFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClient(Guid userId, bool isAdmin = false)
    {
        HttpClient client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestJwtTokenFactory.CreateToken(userId, "Adult", isAdmin));
        return client;
    }

    private static async Task<Guid> AddWordAsync(HttpClient client, string word, string definition, string? example = null)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/vocabulary", new AddPersonalVocabularyWordRequest(word, definition, example));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    private static Task<HttpResponseMessage> EditAsync(HttpClient client, Guid id, string word, string definition, string? example = null) =>
        client.PutAsJsonAsync($"/api/vocabulary/{id}", new UpdatePersonalVocabularyWordRequest(word, definition, example));

    private static async Task<PersonalVocabularyWordDto?> GetMineAsync(HttpClient client, Guid id)
    {
        List<PersonalVocabularyWordDto>? mine =
            await client.GetFromJsonAsync<List<PersonalVocabularyWordDto>>("/api/vocabulary/mine", JsonOptions);
        return mine!.SingleOrDefault(w => w.Id == id);
    }

    private async Task<Guid> SeedLinkedSystemSenseAsync(Guid userId)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        IVocabularyWordRepository repository = scope.ServiceProvider.GetRequiredService<IVocabularyWordRepository>();
        ContentDbContext dbContext = scope.ServiceProvider.GetRequiredService<ContentDbContext>();

        Result<Lexeme> lexeme = await repository.GetOrCreateLexemeAsync($"sys{Guid.NewGuid():N}");
        Sense sense = Sense.CreateSystem(Guid.NewGuid(), lexeme.Value.Id, lexeme.Value.Lemma, "a system word", "Example.");
        await dbContext.Senses.AddAsync(sense);
        await dbContext.LearnerWords.AddAsync(new LearnerWord(Guid.NewGuid(), userId, sense.Id, isAuthor: false));
        await dbContext.SaveChangesAsync();
        return sense.Id;
    }

    [Fact]
    public async Task PutVocabulary_OwnPrivate_Returns204AndMineShowsNewTextWithSameId()
    {
        HttpClient client = CreateClient(Guid.NewGuid());
        Guid id = await AddWordAsync(client, $"aple{Guid.NewGuid():N}", "a fruit");
        string fixedWord = $"apple{Guid.NewGuid():N}";

        HttpResponseMessage response = await EditAsync(client, id, fixedWord, "a red fruit", "It is red.");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        PersonalVocabularyWordDto? edited = await GetMineAsync(client, id);
        edited.Should().NotBeNull();
        edited!.Word.Should().Be(fixedWord);
        edited.Definition.Should().Be("a red fruit");
        edited.Example.Should().Be("It is red.");
    }

    [Fact]
    public async Task PutVocabulary_PendingReview_Returns409()
    {
        HttpClient client = CreateClient(Guid.NewGuid());
        Guid id = await AddWordAsync(client, $"w{Guid.NewGuid():N}", "def");
        (await client.PostAsync($"/api/vocabulary/{id}/share", content: null)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        HttpResponseMessage response = await EditAsync(client, id, "changed", "def2");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task PutVocabulary_Shared_Returns409()
    {
        HttpClient client = CreateClient(Guid.NewGuid());
        HttpClient admin = CreateClient(Guid.NewGuid(), isAdmin: true);
        Guid id = await AddWordAsync(client, $"w{Guid.NewGuid():N}", "def");
        await client.PostAsync($"/api/vocabulary/{id}/share", content: null);
        (await admin.PostAsJsonAsync($"/api/vocabulary/moderation/{id}", new ModerateVocabularyWordRequest(true, true)))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        HttpResponseMessage response = await EditAsync(client, id, "changed", "def2");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task PutVocabulary_SystemSenseInMyList_Returns403()
    {
        Guid userId = Guid.NewGuid();
        HttpClient client = CreateClient(userId);
        Guid senseId = await SeedLinkedSystemSenseAsync(userId);

        HttpResponseMessage response = await EditAsync(client, senseId, "changed", "def2");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PutVocabulary_OtherUsersWord_Returns404()
    {
        HttpClient owner = CreateClient(Guid.NewGuid());
        HttpClient stranger = CreateClient(Guid.NewGuid());
        Guid id = await AddWordAsync(owner, $"w{Guid.NewGuid():N}", "def");

        HttpResponseMessage response = await EditAsync(stranger, id, "changed", "def2");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PutVocabulary_ToHashOfAnotherOwnWord_Returns409AndKeepsOriginal()
    {
        HttpClient client = CreateClient(Guid.NewGuid());
        string word = $"w{Guid.NewGuid():N}";
        Guid first = await AddWordAsync(client, word, "first meaning");
        Guid second = await AddWordAsync(client, word, "second meaning");

        HttpResponseMessage response = await EditAsync(client, second, word, "first meaning");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await GetMineAsync(client, second))!.Definition.Should().Be("second meaning");
        (await GetMineAsync(client, first))!.Definition.Should().Be("first meaning");
    }

    [Fact]
    public async Task PutVocabulary_Invalid_Returns400()
    {
        HttpClient client = CreateClient(Guid.NewGuid());
        Guid id = await AddWordAsync(client, $"w{Guid.NewGuid():N}", "def");

        HttpResponseMessage response = await EditAsync(client, id, "", "def");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PutVocabulary_WordChanged_MovesToNewLexemeAndDeletesOrphanedOne()
    {
        HttpClient client = CreateClient(Guid.NewGuid());
        string oldWord = $"old{Guid.NewGuid():N}";
        string newWord = $"new{Guid.NewGuid():N}";
        Guid id = await AddWordAsync(client, oldWord, "def");

        HttpResponseMessage response = await EditAsync(client, id, newWord, "def");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        using IServiceScope scope = _factory.Services.CreateScope();
        ContentDbContext dbContext = scope.ServiceProvider.GetRequiredService<ContentDbContext>();
        Sense sense = await dbContext.Senses.AsNoTracking().SingleAsync(s => s.Id == id);
        Lexeme lexeme = await dbContext.Lexemes.AsNoTracking().SingleAsync(l => l.Id == sense.LexemeId);
        lexeme.NormalizedLemma.Should().Be(Sense.NormalizeWord(newWord));
        (await dbContext.Lexemes.AnyAsync(l => l.NormalizedLemma == Sense.NormalizeWord(oldWord))).Should().BeFalse();
    }
}
