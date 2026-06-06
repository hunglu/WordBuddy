using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WordBuddy.Application.Interfaces;
using WordBuddy.Domain.Common;
using WordBuddy.Domain.Entities;
using WordBuddy.Infrastructure.Persistence;

namespace WordBuddy.Infrastructure.Persistence.Repositories;

internal sealed class LearnerProgressRepository : ILearnerProgressRepository
{
    private readonly WordBuddyDbContext _context;
    private readonly ILogger<LearnerProgressRepository> _logger;

    public LearnerProgressRepository(WordBuddyDbContext context, ILogger<LearnerProgressRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<LearnerProgress>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogDebug("Fetching LearnerProgress by Id={ProgressId}", id);

        LearnerProgress? record = await _context.LearnerProgress
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (record is null)
        {
            _logger.LogWarning("LearnerProgress not found: Id={ProgressId}", id);
            return Result<LearnerProgress>.Failure(
                Error.NotFound("LearnerProgress.NotFound", $"Progress record {id} was not found."));
        }

        return Result<LearnerProgress>.Success(record);
    }

    public async Task<Result<IReadOnlyList<LearnerProgress>>> GetAllAsync(CancellationToken ct = default)
    {
        _logger.LogDebug("Fetching all LearnerProgress records");
        List<LearnerProgress> records = await _context.LearnerProgress.AsNoTracking().ToListAsync(ct);
        return Result<IReadOnlyList<LearnerProgress>>.Success(records);
    }

    public async Task<Result> AddAsync(LearnerProgress entity, CancellationToken ct = default)
    {
        _logger.LogDebug("Adding LearnerProgress: Id={ProgressId}", entity.Id);
        await _context.LearnerProgress.AddAsync(entity, ct);
        await _context.SaveChangesAsync(ct);
        return Result.Success;
    }

    public async Task<Result> UpdateAsync(LearnerProgress entity, CancellationToken ct = default)
    {
        _logger.LogDebug("Updating LearnerProgress: Id={ProgressId}", entity.Id);
        _context.LearnerProgress.Update(entity);
        await _context.SaveChangesAsync(ct);
        return Result.Success;
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogDebug("Deleting LearnerProgress: Id={ProgressId}", id);

        int affected = await _context.LearnerProgress
            .Where(p => p.Id == id)
            .ExecuteDeleteAsync(ct);

        if (affected == 0)
        {
            _logger.LogWarning("Delete skipped — LearnerProgress not found: Id={ProgressId}", id);
            return Result.Failure(
                Error.NotFound("LearnerProgress.NotFound", $"Progress record {id} was not found."));
        }

        return Result.Success;
    }

    public async Task<Result<IReadOnlyList<LearnerProgress>>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        _logger.LogDebug("Fetching LearnerProgress by UserId={UserId}", userId);
        List<LearnerProgress> records = await _context.LearnerProgress
            .AsNoTracking()
            .Where(p => p.UserId == userId)
            .ToListAsync(ct);
        return Result<IReadOnlyList<LearnerProgress>>.Success(records);
    }

    public async Task<Result<LearnerProgress>> GetByUserAndLessonAsync(Guid userId, Guid lessonId, CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Fetching LearnerProgress by UserId={UserId}, LessonId={LessonId}", userId, lessonId);

        LearnerProgress? record = await _context.LearnerProgress
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId && p.LessonId == lessonId, ct);

        if (record is null)
        {
            _logger.LogDebug(
                "LearnerProgress not found (first access): UserId={UserId}, LessonId={LessonId}",
                userId, lessonId);
            return Result<LearnerProgress>.Failure(
                Error.NotFound("LearnerProgress.NotFound",
                    $"No progress record for user {userId} and lesson {lessonId}."));
        }

        return Result<LearnerProgress>.Success(record);
    }
}
