using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Content.Domain;
using WordBuddy.Content.Infrastructure.Persistence.Configurations;
using WordBuddy.Content.Infrastructure.Persistence;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.IntegrationTests;

/// <summary>
/// WB-71: behaviour that must be the same on SQL Server and PostgreSQL — NULL semantics of the lexeme
/// unique index, the learner-only sense index, and case-insensitive search. Runs on whichever
/// provider the factory selects.
/// </summary>
[Collection(ContentApiCollection.Name)]
public sealed class ProviderParityTests
{
    private readonly ContentApiFactory _factory;

    public ProviderParityTests(ContentApiFactory factory)
    {
        _factory = factory;
    }

    private static string UniqueWord() => "Pw" + Guid.NewGuid().ToString("N")[..10];

    [Fact]
    public async Task Lexemes_TwoWithSameLemmaAndNullPartOfSpeech_SecondIsRejected()
    {
        string word = UniqueWord();
        using IServiceScope scope = _factory.Services.CreateScope();
        ContentDbContext db = scope.ServiceProvider.GetRequiredService<ContentDbContext>();
        db.Lexemes.Add(Lexeme.Create(Guid.NewGuid(), word).Value);
        await db.SaveChangesAsync();

        db.Lexemes.Add(Lexeme.Create(Guid.NewGuid(), word).Value);
        Func<Task> second = () => db.SaveChangesAsync();

        (await second.Should().ThrowAsync<DbUpdateException>())
            .Which.ToString().Should().Contain(LexemeConfiguration.LemmaPartOfSpeechIndexName);
    }

    [Fact]
    public async Task Senses_LearnerDuplicateRejected_IdenticalSystemSensesAllowed()
    {
        string word = UniqueWord();
        Guid owner = Guid.NewGuid();
        using IServiceScope scope = _factory.Services.CreateScope();
        ContentDbContext db = scope.ServiceProvider.GetRequiredService<ContentDbContext>();
        Lexeme lexeme = Lexeme.Create(Guid.NewGuid(), word).Value;
        db.Lexemes.Add(lexeme);

        // System senses may repeat: their ids name audio blobs.
        db.Senses.Add(Sense.CreateSystem(Guid.NewGuid(), lexeme.Id, word, "A test word.", "Example."));
        db.Senses.Add(Sense.CreateSystem(Guid.NewGuid(), lexeme.Id, word, "A test word.", "Example."));
        db.Senses.Add(Sense.CreateLearner(Guid.NewGuid(), lexeme.Id, owner, AgeGroup.Adult, word, "My note.", null).Value);
        await db.SaveChangesAsync();

        db.Senses.Add(Sense.CreateLearner(Guid.NewGuid(), lexeme.Id, owner, AgeGroup.Adult, word, "My note.", null).Value);
        Func<Task> duplicate = () => db.SaveChangesAsync();

        (await duplicate.Should().ThrowAsync<DbUpdateException>())
            .Which.ToString().Should().Contain(SenseConfiguration.ContentHashOwnerIndexName);
    }

    [Fact]
    public async Task Senses_SearchAttachableWithLowerCaseText_FindsUpperCaseWord()
    {
        string word = UniqueWord().ToUpperInvariant();
        using IServiceScope scope = _factory.Services.CreateScope();
        ContentDbContext db = scope.ServiceProvider.GetRequiredService<ContentDbContext>();
        Lexeme lexeme = Lexeme.Create(Guid.NewGuid(), word).Value;
        db.Lexemes.Add(lexeme);
        db.Senses.Add(Sense.CreateSystem(Guid.NewGuid(), lexeme.Id, word, "Upper case word.", "Example."));
        await db.SaveChangesAsync();

        IVocabularyWordRepository repository = scope.ServiceProvider.GetRequiredService<IVocabularyWordRepository>();
        Result<IReadOnlyList<Sense>> found = await repository.SearchAttachableAsync(word.ToLowerInvariant(), 10);

        found.IsSuccess.Should().BeTrue();
        found.Value.Should().ContainSingle(s => s.Word == word);
    }

    [Fact]
    public async Task Senses_RandomLinkedToUser_ReturnsRequestedCount()
    {
        Guid user = Guid.NewGuid();
        using IServiceScope scope = _factory.Services.CreateScope();
        ContentDbContext db = scope.ServiceProvider.GetRequiredService<ContentDbContext>();
        for (int i = 0; i < 5; i++)
        {
            string word = UniqueWord();
            Lexeme lexeme = Lexeme.Create(Guid.NewGuid(), word).Value;
            Sense sense = Sense.CreateLearner(Guid.NewGuid(), lexeme.Id, user, AgeGroup.Adult, word, "Note " + i, null).Value;
            db.Lexemes.Add(lexeme);
            db.Senses.Add(sense);
            db.LearnerWords.Add(new LearnerWord(Guid.NewGuid(), user, sense.Id, isAuthor: true));
        }

        await db.SaveChangesAsync();

        IVocabularyWordRepository repository = scope.ServiceProvider.GetRequiredService<IVocabularyWordRepository>();
        Result<IReadOnlyList<LearnerWord>> random = await repository.GetRandomLinkedToUserAsync(user, 3);

        random.IsSuccess.Should().BeTrue();
        random.Value.Should().HaveCount(3);
    }
}
