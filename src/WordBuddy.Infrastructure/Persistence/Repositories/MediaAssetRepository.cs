using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WordBuddy.Application.Interfaces;
using WordBuddy.Domain.Common;
using WordBuddy.Domain.Entities;
using WordBuddy.Domain.Enums;
using WordBuddy.Infrastructure.Persistence;

namespace WordBuddy.Infrastructure.Persistence.Repositories;

/// <summary>EF Core implementation of <see cref="IMediaAssetRepository"/>.</summary>
internal sealed class MediaAssetRepository : IMediaAssetRepository
{
    private readonly WordBuddyDbContext _context;
    private readonly ILogger<MediaAssetRepository> _logger;

    /// <summary>Initializes a new <see cref="MediaAssetRepository"/>.</summary>
    public MediaAssetRepository(WordBuddyDbContext context, ILogger<MediaAssetRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<Result<MediaAsset>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogDebug("Fetching MediaAsset: Id={AssetId}", id);
        MediaAsset? asset = await _context.MediaAssets.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id, ct);
        if (asset is null)
        {
            _logger.LogWarning("MediaAsset not found: Id={AssetId}", id);
            return Result<MediaAsset>.Failure(
                Error.NotFound("MediaAsset.NotFound", $"Media asset {id} was not found."));
        }
        return Result<MediaAsset>.Success(asset);
    }

    /// <inheritdoc/>
    public async Task<Result<IReadOnlyList<MediaAsset>>> GetAllAsync(CancellationToken ct = default)
    {
        _logger.LogDebug("Fetching all MediaAssets");
        List<MediaAsset> assets = await _context.MediaAssets.AsNoTracking().ToListAsync(ct);
        return Result<IReadOnlyList<MediaAsset>>.Success(assets);
    }

    /// <inheritdoc/>
    public async Task<Result> AddAsync(MediaAsset entity, CancellationToken ct = default)
    {
        _logger.LogDebug("Adding MediaAsset: Id={AssetId}, FileName={FileName}", entity.Id, entity.FileName);
        await _context.MediaAssets.AddAsync(entity, ct);
        await _context.SaveChangesAsync(ct);
        return Result.Success;
    }

    /// <inheritdoc/>
    public async Task<Result> UpdateAsync(MediaAsset entity, CancellationToken ct = default)
    {
        _logger.LogDebug("Updating MediaAsset: Id={AssetId}", entity.Id);
        _context.MediaAssets.Update(entity);
        await _context.SaveChangesAsync(ct);
        return Result.Success;
    }

    /// <inheritdoc/>
    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogDebug("Deleting MediaAsset: Id={AssetId}", id);
        int affected = await _context.MediaAssets
            .Where(a => a.Id == id)
            .ExecuteDeleteAsync(ct);
        if (affected == 0)
        {
            _logger.LogWarning("Delete skipped — MediaAsset not found: Id={AssetId}", id);
            return Result.Failure(
                Error.NotFound("MediaAsset.NotFound", $"Media asset {id} was not found."));
        }
        return Result.Success;
    }

    /// <inheritdoc/>
    public async Task<Result<IReadOnlyList<MediaAsset>>> GetByTypeAsync(MediaAssetType type, CancellationToken ct = default)
    {
        _logger.LogDebug("Fetching MediaAssets: Type={AssetType}", type);
        List<MediaAsset> assets = await _context.MediaAssets.AsNoTracking()
            .Where(a => a.Type == type)
            .ToListAsync(ct);
        return Result<IReadOnlyList<MediaAsset>>.Success(assets);
    }
}
