using Microsoft.EntityFrameworkCore;
using WordBuddy.Application.Interfaces;
using WordBuddy.Domain.Common;
using WordBuddy.Domain.Entities;
using WordBuddy.Infrastructure.Persistence;

namespace WordBuddy.Infrastructure.Persistence.Repositories;

internal sealed class LearnerProgressRepository : ILearnerProgressRepository
{
    private readonly WordBuddyDbContext _context;

    public LearnerProgressRepository(WordBuddyDbContext context) => _context = context;

    public async Task<Result<LearnerProgress>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        LearnerProgress? record = await _context.LearnerProgress
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        return record is null
            ? Result<LearnerProgress>.Failure(Error.NotFound("LearnerProgress.NotFound", $"Progress record {id} was not found."))
            : Result<LearnerProgress>.Success(record);
    }

    public async Task<Result<IReadOnlyList<LearnerProgress>>> GetAllAsync(CancellationToken ct = default)
    {
        List<LearnerProgress> records = await _context.LearnerProgress.AsNoTracking().ToListAsync(ct);
        return Result<IReadOnlyList<LearnerProgress>>.Success(records);
    }

    public async Task<Result> AddAsync(LearnerProgress entity, CancellationToken ct = default)
    {
        await _context.LearnerProgress.AddAsync(entity, ct);
        await _context.SaveChangesAsync(ct);
        return Result.Success;
    }

    public async Task<Result> UpdateAsync(LearnerProgress entity, CancellationToken ct = default)
    {
        _context.LearnerProgress.Update(entity);
        await _context.SaveChangesAsync(ct);
        return Result.Success;
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        int affected = await _context.LearnerProgress
            .Where(p => p.Id == id)
            .ExecuteDeleteAsync(ct);

        return affected > 0
            ? Result.Success
            : Result.Failure(Error.NotFound("LearnerProgress.NotFound", $"Progress record {id} was not found."));
    }

    public async Task<Result<IReadOnlyList<LearnerProgress>>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        List<LearnerProgress> records = await _context.LearnerProgress
            .AsNoTracking()
            .Where(p => p.UserId == userId)
            .ToListAsync(ct);
        return Result<IReadOnlyList<LearnerProgress>>.Success(records);
    }

    public async Task<Result<LearnerProgress>> GetByUserAndLessonAsync(Guid userId, Guid lessonId, CancellationToken ct = default)
    {
        LearnerProgress? record = await _context.LearnerProgress
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId && p.LessonId == lessonId, ct);

        return record is null
            ? Result<LearnerProgress>.Failure(
                Error.NotFound("LearnerProgress.NotFound",
                    $"No progress record for user {userId} and lesson {lessonId}."))
            : Result<LearnerProgress>.Success(record);
    }
}
