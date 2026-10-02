using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WordBuddy.Content.Api.Models;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Content.Domain;
using WordBuddy.Content.Infrastructure.Persistence;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.IntegrationTests;

/// <summary>
/// Concurrent add/adopt of the same word against a real SQL Server database: the unique indexes on
/// <c>VocabularyWords</c> (learner content hash per owner) and <c>UserVocabularyWords</c>
/// (user, word) must resolve a lost race to the existing row instead of surfacing a 500.
/// </summary>
[Collection(ContentApiCollection.Name)]
public sealed class ConcurrentVocabularyAddTests
{
    private const int ParallelRequests = 4;

    private readonly ContentApiFactory _factory;

    public ConcurrentVocabularyAddTests(ContentApiFactory factory)
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

    private async Task<(int Words, int Links)> CountAsync(Guid userId, string contentHash)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ContentDbContext dbContext = scope.ServiceProvider.GetRequiredService<ContentDbContext>();
        int words = await dbContext.VocabularyWords.CountAsync(w => w.OwnerUserId == userId && w.ContentHash == contentHash);
        int links = await dbContext.UserVocabularyWords.CountAsync(l => l.UserId == userId && l.VocabularyWord!.ContentHash == contentHash);
        return (words, links);
    }

    [Fact]
    public async Task AddPersonalVocabularyWord_ParallelIdenticalAdds_AllSucceedWithOneWordAndOneLink()
    {
        Guid ownerId = Guid.NewGuid();
        HttpClient owner = CreateClient(ownerId, "Adult", isAdmin: false);
        string word = $"race-{Guid.NewGuid():N}";
        AddPersonalVocabularyWordRequest request = new(word, "a race", null);

        HttpResponseMessage[] responses = await Task.WhenAll(
            Enumerable.Range(0, ParallelRequests).Select(_ => owner.PostAsJsonAsync("/api/vocabulary", request)));

        responses.Should().OnlyContain(r => r.StatusCode == HttpStatusCode.Created);
        Guid[] ids = await Task.WhenAll(responses.Select(r => r.Content.ReadFromJsonAsync<Guid>()));
        ids.Distinct().Should().ContainSingle();

        (int words, int links) = await CountAsync(ownerId, VocabularyWord.ComputeContentHash(word, "a race", null));
        words.Should().Be(1);
        links.Should().Be(1);
    }

    [Fact]
    public async Task AddSharedVocabularyWordToMyList_ParallelAdopts_AllSucceedWithOneLink()
    {
        Guid authorId = Guid.NewGuid();
        Guid adopterId = Guid.NewGuid();
        HttpClient author = CreateClient(authorId, "Adult", isAdmin: false);
        HttpClient admin = CreateClient(Guid.NewGuid(), "Adult", isAdmin: true);
        HttpClient adopter = CreateClient(adopterId, "Child", isAdmin: false);
        string word = $"shared-race-{Guid.NewGuid():N}";

        HttpResponseMessage addResponse = await author.PostAsJsonAsync(
            "/api/vocabulary", new AddPersonalVocabularyWordRequest(word, "a race", null));
        Guid sharedId = await addResponse.Content.ReadFromJsonAsync<Guid>();
        (await author.PostAsync($"/api/vocabulary/{sharedId}/share", content: null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await admin.PostAsJsonAsync($"/api/vocabulary/moderation/{sharedId}", new ModerateVocabularyWordRequest(Approve: true, VisibleToChildren: true)))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        HttpResponseMessage[] responses = await Task.WhenAll(
            Enumerable.Range(0, ParallelRequests).Select(_ => adopter.PostAsync($"/api/vocabulary/shared/{sharedId}/add-to-mine", content: null)));

        responses.Should().OnlyContain(r => r.IsSuccessStatusCode);
        Guid[] ids = await Task.WhenAll(responses.Select(r => r.Content.ReadFromJsonAsync<Guid>()));
        ids.Should().OnlyContain(id => id == sharedId);

        using IServiceScope scope = _factory.Services.CreateScope();
        ContentDbContext dbContext = scope.ServiceProvider.GetRequiredService<ContentDbContext>();
        (await dbContext.UserVocabularyWords.CountAsync(l => l.UserId == adopterId && l.VocabularyWordId == sharedId)).Should().Be(1);
    }

    /// <summary>Forces the lost-race branch deterministically: the second add's duplicate check
    /// already ran (it never saw the first word), so its insert hits the unique index.</summary>
    [Fact]
    public async Task VocabularyWordRepository_AddAsync_DuplicateKey_ReturnsExistingWordId()
    {
        Guid ownerId = Guid.NewGuid();
        string word = $"forced-{Guid.NewGuid():N}";
        VocabularyWord first = VocabularyWord.CreateLearner(Guid.NewGuid(), ownerId, AgeGroup.Adult, word, "def", null).Value;
        VocabularyWord second = VocabularyWord.CreateLearner(Guid.NewGuid(), ownerId, AgeGroup.Adult, word, "def", null).Value;

        Result<Guid> firstResult;
        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            IVocabularyWordRepository repository = scope.ServiceProvider.GetRequiredService<IVocabularyWordRepository>();
            firstResult = await repository.AddAsync(first, new UserVocabularyWord(Guid.NewGuid(), ownerId, first.Id, isAuthor: true));
        }

        Result<Guid> secondResult;
        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            IVocabularyWordRepository repository = scope.ServiceProvider.GetRequiredService<IVocabularyWordRepository>();
            secondResult = await repository.AddAsync(second, new UserVocabularyWord(Guid.NewGuid(), ownerId, second.Id, isAuthor: true));
        }

        firstResult.Value.Should().Be(first.Id);
        secondResult.IsSuccess.Should().BeTrue();
        secondResult.Value.Should().Be(first.Id);

        (int words, int links) = await CountAsync(ownerId, first.ContentHash);
        words.Should().Be(1);
        links.Should().Be(1);
    }

    /// <summary>Forces the lost-race branch of <c>LinkAsync</c> deterministically.</summary>
    [Fact]
    public async Task VocabularyWordRepository_LinkAsync_DuplicateKey_SucceedsWithOneLink()
    {
        Guid authorId = Guid.NewGuid();
        Guid adopterId = Guid.NewGuid();
        VocabularyWord word = VocabularyWord.CreateLearner(Guid.NewGuid(), authorId, AgeGroup.Adult, $"link-{Guid.NewGuid():N}", "def", null).Value;

        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            IVocabularyWordRepository repository = scope.ServiceProvider.GetRequiredService<IVocabularyWordRepository>();
            (await repository.AddAsync(word, new UserVocabularyWord(Guid.NewGuid(), authorId, word.Id, isAuthor: true))).IsSuccess.Should().BeTrue();
        }

        Result[] results = new Result[2];
        for (int i = 0; i < results.Length; i++)
        {
            using IServiceScope scope = _factory.Services.CreateScope();
            IVocabularyWordRepository repository = scope.ServiceProvider.GetRequiredService<IVocabularyWordRepository>();
            results[i] = await repository.LinkAsync(new UserVocabularyWord(Guid.NewGuid(), adopterId, word.Id, isAuthor: false));
        }

        results.Should().OnlyContain(r => r.IsSuccess);
        using IServiceScope verifyScope = _factory.Services.CreateScope();
        ContentDbContext dbContext = verifyScope.ServiceProvider.GetRequiredService<ContentDbContext>();
        (await dbContext.UserVocabularyWords.CountAsync(l => l.UserId == adopterId && l.VocabularyWordId == word.Id)).Should().Be(1);
    }
}
