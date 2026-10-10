using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Content.Domain;
using WordBuddy.Content.Infrastructure.Persistence;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Infrastructure.Repositories;

internal sealed class LearnerGroupProjectionRepository : ILearnerGroupProjectionRepository
{
    private readonly ContentDbContext _dbContext;
    private readonly ILogger<LearnerGroupProjectionRepository> _logger;

    public LearnerGroupProjectionRepository(ContentDbContext dbContext, ILogger<LearnerGroupProjectionRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<LearnerGroupMemberProjection>> GetTrackedAsync(Guid groupId, Guid learnerId, CancellationToken ct = default)
    {
        LearnerGroupMemberProjection? projection = await _dbContext.LearnerGroupMemberProjections.AsTracking()
            .FirstOrDefaultAsync(p => p.GroupId == groupId && p.LearnerId == learnerId, ct);
        return projection is null
            ? Result.Failure<LearnerGroupMemberProjection>(Error.NotFound("LearnerGroupMemberProjection.NotFound", "No projection for this member."))
            : Result.Success(projection);
    }

    public async Task<IReadOnlyList<LearnerGroupMemberProjection>> GetGroupTrackedAsync(Guid groupId, CancellationToken ct = default) =>
        await _dbContext.LearnerGroupMemberProjections.AsTracking().Where(p => p.GroupId == groupId).ToListAsync(ct);

    public async Task<IReadOnlyList<LearnerGroupMemberProjection>> GetGroupAsync(Guid groupId, CancellationToken ct = default) =>
        await _dbContext.LearnerGroupMemberProjections.Where(p => p.GroupId == groupId).ToListAsync(ct);

    public async Task AddAsync(LearnerGroupMemberProjection projection, CancellationToken ct = default) =>
        await _dbContext.LearnerGroupMemberProjections.AddAsync(projection, ct);

    public async Task<Result> SaveChangesAsync(CancellationToken ct = default)
    {
        try
        {
            await _dbContext.SaveChangesAsync(ct);
            return Result.Success();
        }
        catch (DbUpdateException ex)
        {
            // Concurrent first insert of the same member: the retry policy re-runs and then updates.
            _logger.LogWarning(ex, "LearnerGroupMemberProjection save failed with a database conflict");
            return Result.Failure(Error.Conflict("LearnerGroupMemberProjection.Conflict", "Concurrent projection update."));
        }
    }
}
