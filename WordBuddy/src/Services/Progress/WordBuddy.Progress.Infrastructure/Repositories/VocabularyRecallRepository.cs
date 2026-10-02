using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WordBuddy.Progress.Application.Interfaces;
using WordBuddy.Progress.Domain;
using WordBuddy.Progress.Infrastructure.Persistence;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.Infrastructure.Repositories;

internal sealed class VocabularyRecallRepository : IVocabularyRecallRepository
{
    private readonly ProgressDbContext _dbContext;
    private readonly ILogger<VocabularyRecallRepository> _logger;

    public VocabularyRecallRepository(ProgressDbContext dbContext, ILogger<VocabularyRecallRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result> UpsertStatsAsync(Guid userId, IReadOnlyList<VocabularyRecallResult> results, CancellationToken ct = default)
    {
        _logger.LogDebug("Upserting VocabularyRecallStats: UserId={UserId}, Count={Count}", userId, results.Count);

        foreach (VocabularyRecallResult result in results)
        {
            VocabularyRecallStat? existing = await _dbContext.VocabularyRecallStats
                .AsTracking()
                .FirstOrDefaultAsync(s => s.UserId == userId && s.VocabularyWordId == result.VocabularyWordId, ct);

            if (existing is not null)
            {
                existing.UpdateWordText(result.Word);
                existing.ApplyCheckResult(result.Known);
            }
            else
            {
                VocabularyRecallStat stat = new(Guid.NewGuid(), userId, result.VocabularyWordId, result.Word);
                stat.ApplyCheckResult(result.Known);
                await _dbContext.VocabularyRecallStats.AddAsync(stat, ct);
            }
        }

        await _dbContext.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> AddSessionAsync(VocabularyRecallSession session, CancellationToken ct = default)
    {
        _logger.LogDebug("Adding VocabularyRecallSession with Id={SessionId}", session.Id);

        await _dbContext.VocabularyRecallSessions.AddAsync(session, ct);
        await _dbContext.SaveChangesAsync(ct);

        return Result.Success();
    }

    public async Task<Result<IReadOnlyList<VocabularyRecallStat>>> GetStatsByUserAsync(Guid userId, CancellationToken ct = default)
    {
        _logger.LogDebug("Querying VocabularyRecallStats for UserId={UserId}", userId);

        List<VocabularyRecallStat> stats = await _dbContext.VocabularyRecallStats
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .ToListAsync(ct);

        return Result.Success<IReadOnlyList<VocabularyRecallStat>>(stats);
    }

    public async Task<Result<IReadOnlyList<VocabularyRecallSession>>> GetRecentSessionsByUserAsync(Guid userId, int take, CancellationToken ct = default)
    {
        _logger.LogDebug("Querying recent VocabularyRecallSessions: UserId={UserId}, Take={Take}", userId, take);

        List<VocabularyRecallSession> sessions = await _dbContext.VocabularyRecallSessions
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CheckedAtUtc)
            .Take(take)
            .ToListAsync(ct);

        return Result.Success<IReadOnlyList<VocabularyRecallSession>>(sessions);
    }

    public async Task<Result<IReadOnlyList<VocabularyRecallStat>>> GetTrackedStatsByWordIdsAsync(IReadOnlyCollection<Guid> vocabularyWordIds, CancellationToken ct = default)
    {
        _logger.LogDebug("Querying tracked VocabularyRecallStats by word ids: Count={Count}", vocabularyWordIds.Count);

        List<VocabularyRecallStat> stats = await _dbContext.VocabularyRecallStats
            .AsTracking()
            .Where(s => vocabularyWordIds.Contains(s.VocabularyWordId))
            .ToListAsync(ct);

        return Result.Success<IReadOnlyList<VocabularyRecallStat>>(stats);
    }

    public async Task<Result> SaveRemappedStatsAsync(IReadOnlyList<VocabularyRecallStat> statsToRemove, CancellationToken ct = default)
    {
        _logger.LogDebug("Saving remapped VocabularyRecallStats: Removed={Removed}", statsToRemove.Count);

        _dbContext.VocabularyRecallStats.RemoveRange(statsToRemove);
        await _dbContext.SaveChangesAsync(ct);

        return Result.Success();
    }
}
