using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Content.Domain;
using WordBuddy.Content.Infrastructure.Persistence;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Infrastructure.Repositories;

internal sealed class VocabularyWordRepository : IVocabularyWordRepository
{
    private readonly ContentDbContext _dbContext;
    private readonly ILogger<VocabularyWordRepository> _logger;

    public VocabularyWordRepository(ContentDbContext dbContext, ILogger<VocabularyWordRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<VocabularyWord>>> FindByContentHashAsync(string contentHash, CancellationToken ct = default)
    {
        _logger.LogDebug("Querying VocabularyWords by ContentHash");

        List<VocabularyWord> words = await _dbContext.VocabularyWords
            .AsNoTracking()
            .Where(w => w.ContentHash == contentHash)
            .OrderBy(w => w.Id)
            .ToListAsync(ct);

        return Result.Success<IReadOnlyList<VocabularyWord>>(words);
    }

    public async Task<Result<VocabularyWord>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogDebug("Querying tracked VocabularyWord: WordId={WordId}", id);

        VocabularyWord? word = await _dbContext.VocabularyWords
            .AsTracking()
            .FirstOrDefaultAsync(w => w.Id == id, ct);

        if (word is null)
        {
            return Result.Failure<VocabularyWord>(
                Error.NotFound("PersonalVocabularyWord.NotFound", $"Word {id} was not found."));
        }

        return Result.Success(word);
    }

    public async Task<Result<UserVocabularyWord>> GetLinkAsync(Guid userId, Guid vocabularyWordId, CancellationToken ct = default)
    {
        _logger.LogDebug("Querying tracked UserVocabularyWord: UserId={UserId}, WordId={WordId}", userId, vocabularyWordId);

        UserVocabularyWord? link = await _dbContext.UserVocabularyWords
            .AsTracking()
            .Include(l => l.VocabularyWord)
            .FirstOrDefaultAsync(l => l.UserId == userId && l.VocabularyWordId == vocabularyWordId, ct);

        if (link is null)
        {
            // Deliberately the same error whether the word doesn't exist or just isn't in this
            // user's list — never leaks the existence of another user's word.
            return Result.Failure<UserVocabularyWord>(
                Error.NotFound("PersonalVocabularyWord.NotFound", $"Word {vocabularyWordId} was not found."));
        }

        return Result.Success(link);
    }

    public async Task<Result<IReadOnlyList<UserVocabularyWord>>> GetLinkedToUserAsync(Guid userId, CancellationToken ct = default)
    {
        _logger.LogDebug("Querying UserVocabularyWords for UserId={UserId}", userId);

        List<UserVocabularyWord> links = await _dbContext.UserVocabularyWords
            .AsNoTracking()
            .Include(l => l.VocabularyWord)
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.AddedAtUtc)
            .ToListAsync(ct);

        return Result.Success<IReadOnlyList<UserVocabularyWord>>(links);
    }

    public async Task<Result<IReadOnlyList<UserVocabularyWord>>> GetRandomLinkedToUserAsync(Guid userId, int count, CancellationToken ct = default)
    {
        _logger.LogDebug("Querying random UserVocabularyWords: UserId={UserId}, Count={Count}", userId, count);

        // ORDER BY NEWID() is fine at this scale (one learner's own word list).
        List<UserVocabularyWord> links = await _dbContext.UserVocabularyWords
            .AsNoTracking()
            .Include(l => l.VocabularyWord)
            .Where(l => l.UserId == userId)
            .OrderBy(l => Guid.NewGuid())
            .Take(count)
            .ToListAsync(ct);

        return Result.Success<IReadOnlyList<UserVocabularyWord>>(links);
    }

    public async Task<Result<IReadOnlyList<VocabularyWord>>> GetSharedAsync(bool childSafeOnly, CancellationToken ct = default)
    {
        _logger.LogDebug("Querying shared VocabularyWords: ChildSafeOnly={ChildSafeOnly}", childSafeOnly);

        IQueryable<VocabularyWord> query = _dbContext.VocabularyWords
            .AsNoTracking()
            .Where(w => w.ShareStatus == VocabularyShareStatus.Shared);

        if (childSafeOnly)
        {
            query = query.Where(w => w.VisibleToChildren);
        }

        List<VocabularyWord> words = await query.OrderByDescending(w => w.CreatedAtUtc).ToListAsync(ct);
        return Result.Success<IReadOnlyList<VocabularyWord>>(words);
    }

    public async Task<Result<IReadOnlyList<VocabularyWord>>> GetPendingModerationAsync(CancellationToken ct = default)
    {
        _logger.LogDebug("Querying pending-moderation VocabularyWords");

        List<VocabularyWord> words = await _dbContext.VocabularyWords
            .AsNoTracking()
            .Where(w => w.ShareStatus == VocabularyShareStatus.PendingReview)
            .OrderBy(w => w.CreatedAtUtc)
            .ToListAsync(ct);

        return Result.Success<IReadOnlyList<VocabularyWord>>(words);
    }

    public async Task<Result> AddAsync(VocabularyWord word, UserVocabularyWord authorLink, CancellationToken ct = default)
    {
        _logger.LogDebug("Adding VocabularyWord with Id={WordId} and its author link", word.Id);

        await _dbContext.VocabularyWords.AddAsync(word, ct);
        await _dbContext.UserVocabularyWords.AddAsync(authorLink, ct);
        await _dbContext.SaveChangesAsync(ct);

        return Result.Success();
    }

    public async Task<Result> UpdateAsync(VocabularyWord word, CancellationToken ct = default)
    {
        _logger.LogDebug("Updating VocabularyWord with Id={WordId}", word.Id);

        await _dbContext.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> LinkAsync(UserVocabularyWord link, CancellationToken ct = default)
    {
        _logger.LogDebug("Linking UserId={UserId} to WordId={WordId}", link.UserId, link.VocabularyWordId);

        await _dbContext.UserVocabularyWords.AddAsync(link, ct);
        await _dbContext.SaveChangesAsync(ct);

        return Result.Success();
    }

    public async Task<Result> UnlinkAsync(UserVocabularyWord link, CancellationToken ct = default)
    {
        _logger.LogDebug("Unlinking UserId={UserId} from WordId={WordId}", link.UserId, link.VocabularyWordId);

        _dbContext.UserVocabularyWords.Remove(link);
        await _dbContext.SaveChangesAsync(ct);

        return Result.Success();
    }

    public async Task<Result<bool>> DeleteIfOrphanedAsync(Guid vocabularyWordId, CancellationToken ct = default)
    {
        _logger.LogDebug("Deleting VocabularyWord if orphaned: WordId={WordId}", vocabularyWordId);

        VocabularyWord? word = await _dbContext.VocabularyWords
            .AsTracking()
            .FirstOrDefaultAsync(w => w.Id == vocabularyWordId, ct);

        if (word is null)
        {
            return Result.Success(false);
        }

        bool deletable =
            word.Source == VocabularySource.Learner &&
            word.ShareStatus != VocabularyShareStatus.Shared &&
            !await _dbContext.UserVocabularyWords.AnyAsync(l => l.VocabularyWordId == vocabularyWordId, ct) &&
            !await _dbContext.LessonVocabularyWords.AnyAsync(l => l.VocabularyWordId == vocabularyWordId, ct);

        if (!deletable)
        {
            return Result.Success(false);
        }

        _dbContext.VocabularyWords.Remove(word);
        await _dbContext.SaveChangesAsync(ct);

        return Result.Success(true);
    }
}
