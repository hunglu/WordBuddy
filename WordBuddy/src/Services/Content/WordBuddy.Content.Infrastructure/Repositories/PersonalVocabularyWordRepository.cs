using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Content.Domain;
using WordBuddy.Content.Infrastructure.Persistence;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Infrastructure.Repositories;

internal sealed class PersonalVocabularyWordRepository : IPersonalVocabularyWordRepository
{
    private readonly ContentDbContext _dbContext;
    private readonly ILogger<PersonalVocabularyWordRepository> _logger;

    public PersonalVocabularyWordRepository(ContentDbContext dbContext, ILogger<PersonalVocabularyWordRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result> AddAsync(PersonalVocabularyWord word, CancellationToken ct = default)
    {
        _logger.LogDebug("Adding PersonalVocabularyWord with Id={WordId}", word.Id);

        await _dbContext.PersonalVocabularyWords.AddAsync(word, ct);
        await _dbContext.SaveChangesAsync(ct);

        return Result.Success();
    }

    public async Task<Result<PersonalVocabularyWord>> GetOwnedByIdAsync(Guid id, Guid ownerUserId, CancellationToken ct = default)
    {
        _logger.LogDebug("Querying tracked PersonalVocabularyWord: WordId={WordId}, OwnerUserId={OwnerUserId}", id, ownerUserId);

        PersonalVocabularyWord? word = await _dbContext.PersonalVocabularyWords
            .AsTracking()
            .FirstOrDefaultAsync(w => w.Id == id && w.OwnerUserId == ownerUserId, ct);

        if (word is null)
        {
            // Deliberately the same error whether the word doesn't exist or belongs to someone
            // else — never leaks existence of another user's word.
            return Result.Failure<PersonalVocabularyWord>(
                Error.NotFound("PersonalVocabularyWord.NotFound", $"Word {id} was not found."));
        }

        return Result.Success(word);
    }

    public async Task<Result<PersonalVocabularyWord>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogDebug("Querying tracked PersonalVocabularyWord: WordId={WordId}", id);

        PersonalVocabularyWord? word = await _dbContext.PersonalVocabularyWords
            .AsTracking()
            .FirstOrDefaultAsync(w => w.Id == id, ct);

        if (word is null)
        {
            return Result.Failure<PersonalVocabularyWord>(
                Error.NotFound("PersonalVocabularyWord.NotFound", $"Word {id} was not found."));
        }

        return Result.Success(word);
    }

    public async Task<Result<IReadOnlyList<PersonalVocabularyWord>>> GetByOwnerAsync(Guid ownerUserId, CancellationToken ct = default)
    {
        _logger.LogDebug("Querying PersonalVocabularyWords for OwnerUserId={OwnerUserId}", ownerUserId);

        List<PersonalVocabularyWord> words = await _dbContext.PersonalVocabularyWords
            .AsNoTracking()
            .Where(w => w.OwnerUserId == ownerUserId)
            .OrderByDescending(w => w.CreatedAtUtc)
            .ToListAsync(ct);

        return Result.Success<IReadOnlyList<PersonalVocabularyWord>>(words);
    }

    public async Task<Result<IReadOnlyList<PersonalVocabularyWord>>> GetRandomByOwnerAsync(Guid ownerUserId, int count, CancellationToken ct = default)
    {
        _logger.LogDebug("Querying random PersonalVocabularyWords: OwnerUserId={OwnerUserId}, Count={Count}", ownerUserId, count);

        // ORDER BY NEWID() is fine at this scale (one learner's own word list).
        List<PersonalVocabularyWord> words = await _dbContext.PersonalVocabularyWords
            .AsNoTracking()
            .Where(w => w.OwnerUserId == ownerUserId)
            .OrderBy(w => Guid.NewGuid())
            .Take(count)
            .ToListAsync(ct);

        return Result.Success<IReadOnlyList<PersonalVocabularyWord>>(words);
    }

    public async Task<Result<IReadOnlyList<PersonalVocabularyWord>>> GetSharedAsync(bool childSafeOnly, CancellationToken ct = default)
    {
        _logger.LogDebug("Querying shared PersonalVocabularyWords: ChildSafeOnly={ChildSafeOnly}", childSafeOnly);

        IQueryable<PersonalVocabularyWord> query = _dbContext.PersonalVocabularyWords
            .AsNoTracking()
            .Where(w => w.ShareStatus == VocabularyShareStatus.Shared);

        if (childSafeOnly)
        {
            query = query.Where(w => w.VisibleToChildren);
        }

        List<PersonalVocabularyWord> words = await query.OrderByDescending(w => w.CreatedAtUtc).ToListAsync(ct);
        return Result.Success<IReadOnlyList<PersonalVocabularyWord>>(words);
    }

    public async Task<Result<IReadOnlyList<PersonalVocabularyWord>>> GetPendingModerationAsync(CancellationToken ct = default)
    {
        _logger.LogDebug("Querying pending-moderation PersonalVocabularyWords");

        List<PersonalVocabularyWord> words = await _dbContext.PersonalVocabularyWords
            .AsNoTracking()
            .Where(w => w.ShareStatus == VocabularyShareStatus.PendingReview)
            .OrderBy(w => w.CreatedAtUtc)
            .ToListAsync(ct);

        return Result.Success<IReadOnlyList<PersonalVocabularyWord>>(words);
    }

    public async Task<Result> UpdateAsync(PersonalVocabularyWord word, CancellationToken ct = default)
    {
        _logger.LogDebug("Updating PersonalVocabularyWord with Id={WordId}", word.Id);

        await _dbContext.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(PersonalVocabularyWord word, CancellationToken ct = default)
    {
        _logger.LogDebug("Deleting PersonalVocabularyWord with Id={WordId}", word.Id);

        _dbContext.PersonalVocabularyWords.Remove(word);
        await _dbContext.SaveChangesAsync(ct);

        return Result.Success();
    }
}
