using Microsoft.EntityFrameworkCore;
using WordBuddy.Progress.Application.Interfaces;
using WordBuddy.Progress.Domain;
using WordBuddy.Progress.Infrastructure.Persistence;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.Infrastructure.Repositories;

internal sealed class LearnerWordStateRepository : ILearnerWordStateRepository
{
    private readonly ProgressDbContext _dbContext;

    public LearnerWordStateRepository(ProgressDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<LearnerWordState>> GetTrackedAsync(Guid userId, Guid senseId, CancellationToken ct = default)
    {
        // Local first: a state staged earlier in this scope is not in the database yet.
        LearnerWordState? state = _dbContext.LearnerWordStates.Local
            .FirstOrDefault(s => s.UserId == userId && s.SenseId == senseId)
            ?? await _dbContext.LearnerWordStates
                .AsTracking()
                .FirstOrDefaultAsync(s => s.UserId == userId && s.SenseId == senseId, ct);

        return state is null
            ? Result.Failure<LearnerWordState>(Error.NotFound("LearnerWordState.NotFound", "The word has no learning state."))
            : Result.Success(state);
    }

    public async Task<Result> AddAsync(LearnerWordState state, CancellationToken ct = default)
    {
        await _dbContext.LearnerWordStates.AddAsync(state, ct);
        return Result.Success();
    }

    public async Task<Result<IReadOnlyList<LearnerWordState>>> GetDueAsync(Guid userId, DateTime nowUtc, int take, CancellationToken ct = default)
    {
        List<LearnerWordState> states = await DueQuery(userId, nowUtc)
            .OrderBy(s => s.DueAtUtc)
            .Take(take)
            .ToListAsync(ct);
        return Result.Success<IReadOnlyList<LearnerWordState>>(states);
    }

    public async Task<Result<int>> CountDueAsync(Guid userId, DateTime nowUtc, CancellationToken ct = default) =>
        Result.Success(await DueQuery(userId, nowUtc).CountAsync(ct));

    public async Task<Result<int>> CountFirstReviewedBetweenAsync(Guid userId, DateTime fromUtc, DateTime toUtc, CancellationToken ct = default)
    {
        int count = await _dbContext.LearnerWordStates
            .CountAsync(s => s.UserId == userId && s.FirstReviewedAtUtc >= fromUtc && s.FirstReviewedAtUtc < toUtc, ct);
        return Result.Success(count);
    }

    public async Task<Result<IReadOnlyList<LearnerWordState>>> GetNewInAddedOrderAsync(Guid userId, int take, CancellationToken ct = default)
    {
        if (take <= 0)
        {
            return Result.Success<IReadOnlyList<LearnerWordState>>([]);
        }

        List<LearnerWordState> states = await _dbContext.LearnerWordStates
            .Where(s => s.UserId == userId && s.IsActive && s.Status == WordStatus.New)
            .Join(
                _dbContext.LearnerWordMemberships.Where(m => m.UserId == userId),
                s => s.SenseId,
                m => m.SenseId,
                (s, m) => new { State = s, m.AddedAtUtc })
            .OrderBy(x => x.AddedAtUtc)
            .ThenBy(x => x.State.SenseId)
            .Select(x => x.State)
            .Take(take)
            .ToListAsync(ct);
        return Result.Success<IReadOnlyList<LearnerWordState>>(states);
    }

    public async Task<Result<IReadOnlyList<LearnerWordState>>> GetActiveAsync(Guid userId, CancellationToken ct = default)
    {
        List<LearnerWordState> states = await _dbContext.LearnerWordStates
            .Where(s => s.UserId == userId && s.IsActive)
            .OrderBy(s => s.DueAtUtc)
            .ToListAsync(ct);
        return Result.Success<IReadOnlyList<LearnerWordState>>(states);
    }

    public async Task<Result> SaveChangesAsync(CancellationToken ct = default)
    {
        await _dbContext.SaveChangesAsync(ct);
        return Result.Success();
    }

    private IQueryable<LearnerWordState> DueQuery(Guid userId, DateTime nowUtc) =>
        _dbContext.LearnerWordStates
            .Where(s => s.UserId == userId && s.IsActive && s.Status != WordStatus.New && s.DueAtUtc <= nowUtc);
}
