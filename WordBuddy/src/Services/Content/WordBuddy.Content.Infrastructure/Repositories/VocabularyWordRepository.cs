using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
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

    public async Task<Result<IReadOnlyList<Sense>>> FindByContentHashAsync(string contentHash, CancellationToken ct = default)
    {
        _logger.LogDebug("Querying Senses by ContentHash");

        List<Sense> words = await _dbContext.Senses
            .AsNoTracking()
            .Where(w => w.ContentHash == contentHash)
            .OrderBy(w => w.Id)
            .ToListAsync(ct);

        return Result.Success<IReadOnlyList<Sense>>(words);
    }

    public async Task<Result<IReadOnlyList<LearnerWordLink>>> GetLearnerLinksPageAsync(int skip, int take, CancellationToken ct = default)
    {
        _logger.LogDebug("Querying LearnerWord links page: Skip={Skip}, Take={Take}", skip, take);

        List<LearnerWordLink> links = await _dbContext.LearnerWords
            .AsNoTracking()
            .Where(l => l.UserId != SystemOwner.UserId)
            .OrderBy(l => l.Id)
            .Skip(skip)
            .Take(take)
            .Select(l => new LearnerWordLink(l.UserId, l.SenseId, l.AddedAtUtc))
            .ToListAsync(ct);

        return Result.Success<IReadOnlyList<LearnerWordLink>>(links);
    }

    public async Task<Result<Sense>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogDebug("Querying tracked Sense: WordId={WordId}", id);

        Sense? word = await _dbContext.Senses
            .AsTracking()
            .FirstOrDefaultAsync(w => w.Id == id, ct);

        if (word is null)
        {
            return Result.Failure<Sense>(
                Error.NotFound("PersonalVocabularyWord.NotFound", $"Word {id} was not found."));
        }

        return Result.Success(word);
    }

    public async Task<Result<LearnerWord>> GetLinkAsync(Guid userId, Guid senseId, CancellationToken ct = default)
    {
        _logger.LogDebug("Querying tracked LearnerWord: UserId={UserId}, WordId={WordId}", userId, senseId);

        LearnerWord? link = await _dbContext.LearnerWords
            .AsTracking()
            .Include(l => l.Sense)
            .FirstOrDefaultAsync(l => l.UserId == userId && l.SenseId == senseId, ct);

        if (link is null)
        {
            // Deliberately the same error whether the word doesn't exist or just isn't in this
            // user's list — never leaks the existence of another user's word.
            return Result.Failure<LearnerWord>(
                Error.NotFound("PersonalVocabularyWord.NotFound", $"Word {senseId} was not found."));
        }

        return Result.Success(link);
    }

    public async Task<Result<IReadOnlyList<LearnerWord>>> GetLinkedToUserAsync(Guid userId, CancellationToken ct = default)
    {
        _logger.LogDebug("Querying LearnerWords for UserId={UserId}", userId);

        List<LearnerWord> links = await _dbContext.LearnerWords
            .AsNoTracking()
            .Include(l => l.Sense)
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.AddedAtUtc)
            .ToListAsync(ct);

        return Result.Success<IReadOnlyList<LearnerWord>>(links);
    }

    public async Task<Result<IReadOnlyList<LearnerWord>>> GetRandomLinkedToUserAsync(Guid userId, int count, CancellationToken ct = default)
    {
        _logger.LogDebug("Querying random LearnerWords: UserId={UserId}, Count={Count}", userId, count);

        // ORDER BY NEWID() is fine at this scale (one learner's own word list).
        List<LearnerWord> links = await _dbContext.LearnerWords
            .AsNoTracking()
            .Include(l => l.Sense)
            .Where(l => l.UserId == userId)
            .OrderBy(l => Guid.NewGuid())
            .Take(count)
            .ToListAsync(ct);

        return Result.Success<IReadOnlyList<LearnerWord>>(links);
    }

    public async Task<Result<IReadOnlyList<Sense>>> GetSharedAsync(bool childSafeOnly, CancellationToken ct = default)
    {
        _logger.LogDebug("Querying shared Senses: ChildSafeOnly={ChildSafeOnly}", childSafeOnly);

        IQueryable<Sense> query = _dbContext.Senses
            .AsNoTracking()
            .Where(w => w.ShareStatus == VocabularyShareStatus.Shared);

        if (childSafeOnly)
        {
            query = query.Where(w => w.VisibleToChildren);
        }

        List<Sense> words = await query.OrderByDescending(w => w.CreatedAtUtc).ToListAsync(ct);
        return Result.Success<IReadOnlyList<Sense>>(words);
    }

    public async Task<Result<IReadOnlyList<Sense>>> GetPendingModerationAsync(CancellationToken ct = default)
    {
        _logger.LogDebug("Querying pending-moderation Senses");

        List<Sense> words = await _dbContext.Senses
            .AsNoTracking()
            .Where(w => w.ShareStatus == VocabularyShareStatus.PendingReview)
            .OrderBy(w => w.CreatedAtUtc)
            .ToListAsync(ct);

        return Result.Success<IReadOnlyList<Sense>>(words);
    }

    public async Task<Result<Lexeme>> GetOrCreateLexemeAsync(string word, CancellationToken ct = default)
    {
        string normalized = Sense.NormalizeWord(word);
        _logger.LogDebug("Getting or creating Lexeme with unknown part of speech");

        // The database collation decides equality, as in the migration's GROUP BY.
        Lexeme? existing = await FindNullPosLexemeAsync(normalized, ct);
        if (existing is not null)
        {
            return Result.Success(existing);
        }

        Result<Lexeme> created = Lexeme.Create(Guid.NewGuid(), word);
        if (created.IsFailure)
        {
            return created;
        }

        await _dbContext.Lexemes.AddAsync(created.Value, ct);

        try
        {
            await _dbContext.SaveChangesAsync(ct);
            return created;
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            // A concurrent request created the same (NormalizedLemma, NULL) lexeme first
            // (UX_Lexemes_NormalizedLemma_PartOfSpeech, unfiltered) — use that one.
            Detach(created.Value);
        }

        Lexeme? winner = await FindNullPosLexemeAsync(normalized, ct);
        if (winner is null)
        {
            _logger.LogWarning("Concurrent lexeme create conflicted but no existing lexeme was found");
            return Result.Failure<Lexeme>(Error.Conflict(
                "PersonalVocabularyWord.ConcurrentAdd", "The word was changed concurrently. Please try again."));
        }

        _logger.LogInformation("Concurrent lexeme create resolved to existing LexemeId={LexemeId}", winner.Id);
        return Result.Success(winner);
    }

    private Task<Lexeme?> FindNullPosLexemeAsync(string normalizedLemma, CancellationToken ct) =>
        _dbContext.Lexemes
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.NormalizedLemma == normalizedLemma && l.PartOfSpeech == null, ct);

    public async Task<Result<Guid>> AddAsync(Sense word, LearnerWord authorLink, CancellationToken ct = default)
    {
        _logger.LogDebug("Adding Sense with Id={WordId} and its author link", word.Id);

        await _dbContext.Senses.AddAsync(word, ct);
        await _dbContext.LearnerWords.AddAsync(authorLink, ct);

        bool lexemeRetried = false;
        while (true)
        {
            try
            {
                await _dbContext.SaveChangesAsync(ct);
                return Result.Success(word.Id);
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                // A concurrent request by the same owner stored the same word first
                // (UX_Senses_ContentHash_OwnerUserId_Learner). Return that word instead.
                Detach(word, authorLink);
                break;
            }
            catch (DbUpdateException ex) when (IsLexemeForeignKeyViolation(ex))
            {
                // Detach first: GetOrCreateLexemeAsync saves, and must not flush this sense again.
                Detach(word, authorLink);

                if (lexemeRetried)
                {
                    _logger.LogWarning("Add of WordId={WordId} failed twice on a missing lexeme", word.Id);
                    return Result.Failure<Guid>(Error.Conflict(
                        "PersonalVocabularyWord.ConcurrentAdd", "The word was changed concurrently. Please try again."));
                }

                // The lexeme was deleted between lookup and save (its last other sense was removed).
                // Get or re-create it once and retry with the new id.
                lexemeRetried = true;
                _logger.LogInformation("Lexeme of WordId={WordId} vanished before save; retrying once", word.Id);

                Result<Lexeme> lexemeResult = await GetOrCreateLexemeAsync(word.Word, ct);
                if (lexemeResult.IsFailure)
                {
                    return Result.Failure<Guid>(lexemeResult.Error);
                }

                await _dbContext.Senses.AddAsync(word, ct);
                _dbContext.Entry(word).Property(w => w.LexemeId).CurrentValue = lexemeResult.Value.Id;
                await _dbContext.LearnerWords.AddAsync(authorLink, ct);
            }
        }

        Sense? existing = await _dbContext.Senses
            .AsNoTracking()
            .FirstOrDefaultAsync(
                w => w.Source == VocabularySource.Learner &&
                     w.OwnerUserId == word.OwnerUserId &&
                     w.ContentHash == word.ContentHash,
                ct);

        if (existing is null)
        {
            // The conflicting row is gone again (unlinked and deleted in between) — not an expected
            // race; surface it as a conflict rather than throwing.
            _logger.LogWarning("Concurrent add of WordId={WordId} conflicted but no existing word was found", word.Id);
            return Result.Failure<Guid>(Error.Conflict(
                "PersonalVocabularyWord.ConcurrentAdd", "The word was changed concurrently. Please try again."));
        }

        _logger.LogInformation(
            "Concurrent add resolved to existing word: WordId={WordId}, ExistingWordId={ExistingWordId}",
            word.Id, existing.Id);

        bool linked = await _dbContext.LearnerWords
            .AnyAsync(l => l.UserId == authorLink.UserId && l.SenseId == existing.Id, ct);
        if (!linked)
        {
            Result linkResult = await LinkAsync(
                new LearnerWord(Guid.NewGuid(), authorLink.UserId, existing.Id, isAuthor: true), ct);
            if (linkResult.IsFailure)
            {
                return Result.Failure<Guid>(linkResult.Error);
            }
        }

        return Result.Success(existing.Id);
    }

    public async Task<Result> UpdateAsync(Sense word, CancellationToken ct = default)
    {
        _logger.LogDebug("Updating Sense with Id={WordId}", word.Id);

        await _dbContext.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> LinkAsync(LearnerWord link, CancellationToken ct = default)
    {
        _logger.LogDebug("Linking UserId={UserId} to WordId={WordId}", link.UserId, link.SenseId);

        await _dbContext.LearnerWords.AddAsync(link, ct);

        try
        {
            await _dbContext.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            // A concurrent request created the same (UserId, SenseId) link first
            // (IX_LearnerWords_UserId_SenseId) — the desired state already holds.
            Detach(link);
            _logger.LogInformation(
                "Concurrent link resolved to existing link: UserId={UserId}, WordId={WordId}",
                link.UserId, link.SenseId);
        }

        return Result.Success();
    }

    /// <summary>SQL Server 2601 (unique index) / 2627 (unique constraint) duplicate-key errors.</summary>
    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is SqlException { Number: 2601 or 2627 };

    /// <summary>Name of the Senses → Lexemes foreign key; SQL Server quotes it in a 547 message.</summary>
    private const string LexemeForeignKeyName = "FK_Senses_Lexemes_LexemeId";

    /// <summary>SQL Server 547 (constraint conflict) on the Senses → Lexemes foreign key.</summary>
    private static bool IsLexemeForeignKeyViolation(DbUpdateException ex) =>
        ex.InnerException is SqlException { Number: 547 } sqlException &&
        sqlException.Message.Contains(LexemeForeignKeyName, StringComparison.Ordinal);

    /// <summary>Stops tracking the entities of a failed save so the context can be reused.</summary>
    private void Detach(params object[] entities)
    {
        foreach (object entity in entities)
        {
            _dbContext.Entry(entity).State = EntityState.Detached;
        }
    }

    public async Task<Result> UnlinkAsync(LearnerWord link, CancellationToken ct = default)
    {
        _logger.LogDebug("Unlinking UserId={UserId} from WordId={WordId}", link.UserId, link.SenseId);

        _dbContext.LearnerWords.Remove(link);
        await _dbContext.SaveChangesAsync(ct);

        return Result.Success();
    }

    public async Task<Result<bool>> DeleteIfOrphanedAsync(Guid senseId, CancellationToken ct = default)
    {
        _logger.LogDebug("Deleting Sense if orphaned: WordId={WordId}", senseId);

        Sense? word = await _dbContext.Senses
            .AsTracking()
            .FirstOrDefaultAsync(w => w.Id == senseId, ct);

        if (word is null)
        {
            return Result.Success(false);
        }

        bool deletable =
            word.Source == VocabularySource.Learner &&
            word.ShareStatus != VocabularyShareStatus.Shared &&
            !await _dbContext.LearnerWords.AnyAsync(l => l.SenseId == senseId, ct) &&
            !await _dbContext.LessonSenses.AnyAsync(l => l.SenseId == senseId, ct);

        if (!deletable)
        {
            return Result.Success(false);
        }

        Guid lexemeId = word.LexemeId;

        // One transaction: the sense delete and the guarded lexeme delete commit together.
        await using IDbContextTransaction transaction = await _dbContext.Database.BeginTransactionAsync(ct);

        _dbContext.Senses.Remove(word);
        await _dbContext.SaveChangesAsync(ct);

        // Guarded: a lexeme still referenced by any other sense is never deleted (D16).
        int lexemesDeleted = await _dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"DELETE FROM [Lexemes] WHERE [Id] = {lexemeId} AND NOT EXISTS (SELECT 1 FROM [Senses] WHERE [LexemeId] = {lexemeId})",
            ct);

        await transaction.CommitAsync(ct);

        _logger.LogDebug(
            "Deleted orphaned Sense WordId={WordId}; LexemesDeleted={LexemesDeleted}", senseId, lexemesDeleted);

        return Result.Success(true);
    }
}
