using Microsoft.Extensions.Logging;
using WordBuddy.Content.Application.Abstractions;
using WordBuddy.Content.Application.DTOs;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Application.Features.Media.Queries.GetMediaAsset;

public sealed class GetMediaAssetQueryHandler : IQueryHandler<GetMediaAssetQuery, MediaAssetDto>
{
    private readonly IMediaAssetRepository _mediaAssetRepository;
    private readonly ILogger<GetMediaAssetQueryHandler> _logger;

    public GetMediaAssetQueryHandler(IMediaAssetRepository mediaAssetRepository, ILogger<GetMediaAssetQueryHandler> logger)
    {
        _mediaAssetRepository = mediaAssetRepository;
        _logger = logger;
    }

    public async Task<Result<MediaAssetDto>> HandleAsync(GetMediaAssetQuery query, CancellationToken ct = default)
    {
        Result<MediaAsset> assetResult = await _mediaAssetRepository.GetByIdAsync(query.MediaAssetId, ct);
        if (assetResult.IsFailure)
        {
            _logger.LogWarning("GetMediaAssetQuery not found: MediaAssetId={MediaAssetId}", query.MediaAssetId);
            return Result.Failure<MediaAssetDto>(assetResult.Error);
        }

        MediaAsset asset = assetResult.Value;
        return Result.Success(new MediaAssetDto(asset.Id, asset.Type, asset.Url));
    }
}
