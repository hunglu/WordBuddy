using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using WordBuddy.Content.Application.Interfaces.Autofill;
using WordBuddy.Content.Domain;
using WordBuddy.Content.Infrastructure.Persistence;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Infrastructure.Repositories;

internal sealed class AutofillRepository : IAutofillRepository
{
    private readonly ContentDbContext _dbContext;
    private readonly ILogger<AutofillRepository> _logger;

    public AutofillRepository(ContentDbContext dbContext, ILogger<AutofillRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<Sense>>> GetCatalogAsync(string normalizedLemma, CancellationToken ct = default)
    {
        _logger.LogDebug("Querying auto-fill catalog");

        List<Sense> senses = await WithDetails(_dbContext.Senses.AsNoTracking())
            .Where(s => s.Origin == SenseOrigin.AutoFill && s.Lexeme!.NormalizedLemma == normalizedLemma)
            .OrderBy(s => s.Lexeme!.PartOfSpeech)
            .ThenBy(s => s.CreatedAtUtc)
            .ThenBy(s => s.Id)
            .ToListAsync(ct);

        return Result.Success<IReadOnlyList<Sense>>(senses);
    }

    public async Task<Result<IReadOnlyList<Sense>>> GetMovableCatalogSensesAsync(string normalizedLemma, CancellationToken ct = default)
    {
        List<Sense> senses = await _dbContext.Senses
            .AsTracking()
            .Where(s => s.Lexeme!.NormalizedLemma == normalizedLemma && s.Lexeme.PartOfSpeech == null)
            .Where(s => s.Source == VocabularySource.System || s.ShareStatus == VocabularyShareStatus.Shared)
            .OrderBy(s => s.Id)
            .ToListAsync(ct);

        return Result.Success<IReadOnlyList<Sense>>(senses);
    }

    public async Task<Result> SaveAsync(AutofillSave save, CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Saving auto-fill: Lexemes={LexemeCount}, Senses={SenseCount}, Moved={MovedCount}",
            save.Lexemes.Count, save.Senses.Count, save.MovedSenses.Count);

        IExecutionStrategy strategy = _dbContext.Database.CreateExecutionStrategy();
        try
        {
            return await strategy.ExecuteAsync(async () =>
            {
                await using IDbContextTransaction transaction = await _dbContext.Database.BeginTransactionAsync(ct);

                await _dbContext.MediaAssets.AddRangeAsync(save.AudioAssets, ct);
                await _dbContext.Lexemes.AddRangeAsync(save.Lexemes, ct);
                await _dbContext.Senses.AddRangeAsync(save.Senses, ct);
                await _dbContext.SaveChangesAsync(ct);

                // Guarded orphan delete (D14/D16): the null-POS lexeme goes only when no sense —
                // including any private learner sense — references it any more.
                int deleted = await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                    $"DELETE FROM [Lexemes] WHERE [NormalizedLemma] = {save.NormalizedLemma} AND [PartOfSpeech] IS NULL AND NOT EXISTS (SELECT 1 FROM [Senses] s WHERE s.[LexemeId] = [Lexemes].[Id])",
                    ct);

                await transaction.CommitAsync(ct);
                _logger.LogDebug("Auto-fill saved; OrphanLexemesDeleted={Deleted}", deleted);
                return Result.Success();
            });
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            // A concurrent auto-fill stored the same (NormalizedLemma, PartOfSpeech) first.
            _dbContext.ChangeTracker.Clear();
            _logger.LogInformation("Concurrent auto-fill save detected; caller re-reads the catalog");
            return Result.Failure(Error.Conflict("Autofill.ConcurrentSave", "The word was auto-filled concurrently."));
        }
    }

    public async Task<Result<IReadOnlyList<LearnerWord>>> GetPendingLinksForLearnerAsync(Guid learnerId, CancellationToken ct = default)
    {
        List<LearnerWord> links = await _dbContext.LearnerWords
            .AsNoTracking()
            .Include(l => l.Sense!).ThenInclude(s => s.Lexeme)
            .Include(l => l.Sense!).ThenInclude(s => s.Translations)
            .Where(l => l.UserId == learnerId &&
                        l.RequiresChildApproval &&
                        l.ChildApprovedAtUtc == null &&
                        l.Sense!.Origin == SenseOrigin.AutoFill &&
                        !l.Sense.VisibleToChildren)
            .OrderBy(l => l.AddedAtUtc)
            .AsSplitQuery()
            .ToListAsync(ct);

        return Result.Success<IReadOnlyList<LearnerWord>>(links);
    }

    public async Task<Result<IReadOnlyList<Sense>>> GetPendingSensesForAdminAsync(CancellationToken ct = default)
    {
        List<Sense> senses = await _dbContext.Senses
            .AsNoTracking()
            .Include(s => s.Lexeme)
            .Include(s => s.Translations)
            .Where(s => s.Origin == SenseOrigin.AutoFill && !s.VisibleToChildren)
            .Where(s => _dbContext.LearnerWords.Any(l => l.SenseId == s.Id && l.RequiresChildApproval && l.ChildApprovedAtUtc == null))
            .OrderBy(s => s.CreatedAtUtc)
            .AsSplitQuery()
            .ToListAsync(ct);

        return Result.Success<IReadOnlyList<Sense>>(senses);
    }

    public async Task<Result<LearnerWord>> GetTrackedLinkAsync(Guid learnerId, Guid senseId, CancellationToken ct = default)
    {
        LearnerWord? link = await _dbContext.LearnerWords
            .AsTracking()
            .Include(l => l.Sense!).ThenInclude(s => s.Lexeme)
            .FirstOrDefaultAsync(l => l.UserId == learnerId && l.SenseId == senseId, ct);

        return link is null
            ? Result.Failure<LearnerWord>(Error.NotFound("PersonalVocabularyWord.NotFound", $"Word {senseId} was not found."))
            : Result.Success(link);
    }

    public async Task<Result<Sense>> GetTrackedSenseAsync(Guid senseId, CancellationToken ct = default)
    {
        Sense? sense = await _dbContext.Senses
            .AsTracking()
            .Include(s => s.Lexeme)
            .FirstOrDefaultAsync(s => s.Id == senseId, ct);

        return sense is null
            ? Result.Failure<Sense>(Error.NotFound("PersonalVocabularyWord.NotFound", $"Word {senseId} was not found."))
            : Result.Success(sense);
    }

    public async Task<Result> SaveChangesAsync(CancellationToken ct = default)
    {
        await _dbContext.SaveChangesAsync(ct);
        return Result.Success();
    }

    /// <summary>Lexeme with audio, plus translations — what the enriched word DTOs need.</summary>
    internal static IQueryable<Sense> WithDetails(IQueryable<Sense> query) =>
        query
            .Include(s => s.Lexeme!).ThenInclude(l => l.UkAudio)
            .Include(s => s.Lexeme!).ThenInclude(l => l.UsAudio)
            .Include(s => s.Translations)
            .AsSplitQuery();
}
