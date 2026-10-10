using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WordBuddy.Progress.Application.Interfaces;
using WordBuddy.Progress.Domain;
using WordBuddy.Progress.Infrastructure.Persistence;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.Infrastructure.Repositories;

internal sealed class SupportLinkProjectionRepository : ISupportLinkProjectionRepository
{
    private readonly ProgressDbContext _dbContext;
    private readonly ILogger<SupportLinkProjectionRepository> _logger;

    public SupportLinkProjectionRepository(ProgressDbContext dbContext, ILogger<SupportLinkProjectionRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<SupportLinkProjection>> GetTrackedAsync(Guid linkId, CancellationToken ct = default)
    {
        SupportLinkProjection? projection = await _dbContext.SupportLinkProjections.AsTracking()
            .FirstOrDefaultAsync(p => p.LinkId == linkId, ct);
        return projection is null
            ? Result.Failure<SupportLinkProjection>(Error.NotFound("SupportLinkProjection.NotFound", "No projection for this link."))
            : Result.Success(projection);
    }

    public async Task AddAsync(SupportLinkProjection projection, CancellationToken ct = default) =>
        await _dbContext.SupportLinkProjections.AddAsync(projection, ct);

    public async Task<Result> SaveChangesAsync(CancellationToken ct = default)
    {
        try
        {
            await _dbContext.SaveChangesAsync(ct);
            return Result.Success();
        }
        catch (DbUpdateException ex)
        {
            // Concurrent first insert of the same LinkId: the retry policy re-runs and then updates.
            _logger.LogWarning(ex, "SupportLinkProjection save failed with a database conflict");
            return Result.Failure(Error.Conflict("SupportLinkProjection.Conflict", "Concurrent projection update."));
        }
    }

    public Task<bool> HasActiveLinkAsync(Guid supporterId, Guid learnerId, CancellationToken ct = default) =>
        _dbContext.SupportLinkProjections.AnyAsync(p => p.SupporterId == supporterId && p.LearnerId == learnerId && p.IsActive, ct);

    public Task<bool> HasActiveSupporterAsync(Guid learnerId, CancellationToken ct = default) =>
        _dbContext.SupportLinkProjections.AnyAsync(p => p.LearnerId == learnerId && p.IsActive, ct);

    public async Task<IReadOnlySet<Guid>> GetLearnersWithActiveLinkAsync(Guid supporterId, IReadOnlyCollection<Guid> learnerIds, CancellationToken ct = default) =>
        (await _dbContext.SupportLinkProjections
            .Where(p => p.SupporterId == supporterId && p.IsActive && learnerIds.Contains(p.LearnerId))
            .Select(p => p.LearnerId)
            .ToListAsync(ct)).ToHashSet();
}
