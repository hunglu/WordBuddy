using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Content.Domain;
using WordBuddy.Content.Infrastructure.Persistence;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Infrastructure.Repositories;

internal sealed class MediaAssetRepository : IMediaAssetRepository
{
    private readonly ContentDbContext _dbContext;
    private readonly ILogger<MediaAssetRepository> _logger;

    public MediaAssetRepository(ContentDbContext dbContext, ILogger<MediaAssetRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<MediaAsset>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogDebug("Querying MediaAsset by Id={MediaAssetId}", id);

        MediaAsset? asset = await _dbContext.MediaAssets.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id, ct);
        if (asset is null)
        {
            _logger.LogWarning("MediaAsset not found: MediaAssetId={MediaAssetId}", id);
            return Result.Failure<MediaAsset>(Error.NotFound("MediaAsset.NotFound", $"Media asset {id} was not found."));
        }

        return Result.Success(asset);
    }

    public async Task<Result> AddAsync(MediaAsset asset, CancellationToken ct = default)
    {
        _logger.LogDebug("Adding MediaAsset with Id={MediaAssetId}", asset.Id);

        await _dbContext.MediaAssets.AddAsync(asset, ct);
        await _dbContext.SaveChangesAsync(ct);

        return Result.Success();
    }
}
