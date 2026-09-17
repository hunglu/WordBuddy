using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WordBuddy.Progress.Application.Interfaces;
using WordBuddy.Progress.Domain;
using WordBuddy.Progress.Infrastructure.Persistence;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.Infrastructure.Repositories;

internal sealed class LearnerProgressRepository : ILearnerProgressRepository
{
    private readonly ProgressDbContext _dbContext;
    private readonly ILogger<LearnerProgressRepository> _logger;

    public LearnerProgressRepository(ProgressDbContext dbContext, ILogger<LearnerProgressRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<LearnerProgress>>> GetByUserAsync(Guid userId, CancellationToken ct = default)
    {
        _logger.LogDebug("Querying LearnerProgress for UserId={UserId}", userId);

        List<LearnerProgress> entries = await _dbContext.LearnerProgressEntries
            .AsNoTracking()
            .Where(p => p.UserId == userId)
            .ToListAsync(ct);

        return Result.Success<IReadOnlyList<LearnerProgress>>(entries);
    }

    public async Task<Result<LearnerProgress>> GetTrackedByUserAndLessonAsync(Guid userId, Guid lessonId, CancellationToken ct = default)
    {
        _logger.LogDebug("Querying tracked LearnerProgress: UserId={UserId}, LessonId={LessonId}", userId, lessonId);

        LearnerProgress? progress = await _dbContext.LearnerProgressEntries
            .AsTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId && p.LessonId == lessonId, ct);

        if (progress is null)
        {
            return Result.Failure<LearnerProgress>(
                Error.NotFound("LearnerProgress.NotFound", $"No progress entry for user {userId} on lesson {lessonId}."));
        }

        return Result.Success(progress);
    }

    public async Task<Result> AddAsync(LearnerProgress progress, CancellationToken ct = default)
    {
        _logger.LogDebug("Adding LearnerProgress with Id={ProgressId}", progress.Id);

        await _dbContext.LearnerProgressEntries.AddAsync(progress, ct);
        await _dbContext.SaveChangesAsync(ct);

        return Result.Success();
    }

    public async Task<Result> SaveChangesAsync(CancellationToken ct = default)
    {
        await _dbContext.SaveChangesAsync(ct);
        return Result.Success();
    }
}
