using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WordBuddy.Progress.Application.Interfaces;
using WordBuddy.Progress.Domain.Challenges;
using WordBuddy.Progress.Infrastructure.Persistence;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.Infrastructure.Repositories;

internal sealed class ChallengeProfileRepository : IChallengeProfileRepository
{
    private readonly ProgressDbContext _dbContext;
    private readonly ILogger<ChallengeProfileRepository> _logger;

    public ChallengeProfileRepository(ProgressDbContext dbContext, ILogger<ChallengeProfileRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<ChallengeProfile>> GetAsync(Guid userId, CancellationToken ct = default)
    {
        ChallengeProfile? profile = await _dbContext.ChallengeProfiles.FirstOrDefaultAsync(p => p.UserId == userId, ct);
        return profile is null
            ? Result.Failure<ChallengeProfile>(Error.NotFound("ChallengeProfile.NotFound", "No profile for this learner."))
            : Result.Success(profile);
    }

    public async Task<Result<ChallengeProfile>> GetTrackedAsync(Guid userId, CancellationToken ct = default)
    {
        ChallengeProfile? profile = await _dbContext.ChallengeProfiles.AsTracking().FirstOrDefaultAsync(p => p.UserId == userId, ct);
        return profile is null
            ? Result.Failure<ChallengeProfile>(Error.NotFound("ChallengeProfile.NotFound", "No profile for this learner."))
            : Result.Success(profile);
    }

    public async Task<IReadOnlyList<ChallengeProfile>> GetByIdsAsync(IReadOnlyCollection<Guid> userIds, CancellationToken ct = default) =>
        await _dbContext.ChallengeProfiles.Where(p => userIds.Contains(p.UserId)).ToListAsync(ct);

    public async Task AddAsync(ChallengeProfile profile, CancellationToken ct = default) =>
        await _dbContext.ChallengeProfiles.AddAsync(profile, ct);

    public async Task<Result> SaveChangesAsync(CancellationToken ct = default)
    {
        try
        {
            await _dbContext.SaveChangesAsync(ct);
            return Result.Success();
        }
        catch (DbUpdateException ex)
        {
            // Concurrent first insert of the same UserId: the retry policy re-runs and then updates.
            _logger.LogWarning(ex, "ChallengeProfile save failed with a database conflict");
            return Result.Failure(Error.Conflict("ChallengeProfile.Conflict", "Concurrent profile update."));
        }
    }
}
