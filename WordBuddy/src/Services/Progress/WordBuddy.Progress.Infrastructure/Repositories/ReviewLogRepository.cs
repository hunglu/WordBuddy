using Microsoft.EntityFrameworkCore;
using WordBuddy.Progress.Application.Interfaces;
using WordBuddy.Progress.Domain;
using WordBuddy.Progress.Infrastructure.Persistence;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.Infrastructure.Repositories;

/// <summary>Insert-only: no update or delete methods exist.</summary>
internal sealed class ReviewLogRepository : IReviewLogRepository
{
    private readonly ProgressDbContext _dbContext;

    public ReviewLogRepository(ProgressDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result> AddAsync(ReviewLog log, CancellationToken ct = default)
    {
        await _dbContext.ReviewLogs.AddAsync(log, ct);
        return Result.Success();
    }

    public async Task<Result<int>> CountAttemptsAsync(Guid userId, Guid sessionId, Guid senseId, CancellationToken ct = default)
    {
        int count = await _dbContext.ReviewLogs
            .CountAsync(l => l.UserId == userId && l.SessionId == sessionId && l.SenseId == senseId, ct);
        return Result.Success(count);
    }

    public async Task<Result<int>> CountCorrectAsync(Guid userId, Guid sessionId, Guid senseId, CancellationToken ct = default)
    {
        int count = await _dbContext.ReviewLogs
            .CountAsync(l => l.UserId == userId && l.SessionId == sessionId && l.SenseId == senseId && l.IsCorrect, ct);
        return Result.Success(count);
    }
}
