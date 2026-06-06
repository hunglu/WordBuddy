using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using WordBuddy.Application.DTOs;
using WordBuddy.Application.Interfaces;
using WordBuddy.Domain.Common;
using WordBuddy.Domain.Entities;

namespace WordBuddy.Application.Features.Media.Queries.GetMediaAsset;

/// <summary>Handles <see cref="GetMediaAssetQuery"/> — retrieves a MediaAsset record by identifier.</summary>
public sealed class GetMediaAssetQueryHandler
{
    private readonly IMediaAssetRepository _mediaAssetRepository;
    private readonly IValidator<GetMediaAssetQuery> _validator;
    private readonly ILogger<GetMediaAssetQueryHandler> _logger;

    /// <summary>Initializes a new <see cref="GetMediaAssetQueryHandler"/>.</summary>
    public GetMediaAssetQueryHandler(
        IMediaAssetRepository mediaAssetRepository,
        IValidator<GetMediaAssetQuery> validator,
        ILogger<GetMediaAssetQueryHandler> logger)
    {
        _mediaAssetRepository = mediaAssetRepository;
        _validator = validator;
        _logger = logger;
    }

    /// <summary>Validates then fetches the MediaAsset from the repository.</summary>
    public async Task<Result<MediaAssetDto>> HandleAsync(GetMediaAssetQuery query, CancellationToken ct = default)
    {
        _logger.LogInformation("GetMediaAssetQuery started: AssetId={AssetId}", query.AssetId);

        ValidationResult validation = await _validator.ValidateAsync(query, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("GetMediaAssetQuery validation failed: {Errors}", validation.ToString());
            return Result<MediaAssetDto>.Failure(
                Error.Validation("GetMediaAsset.Validation", validation.ToString()));
        }

        Result<MediaAsset> result = await _mediaAssetRepository.GetByIdAsync(query.AssetId, ct);
        if (result.IsFailure)
        {
            _logger.LogWarning(
                "GetMediaAssetQuery: asset not found: AssetId={AssetId}, ErrorCode={ErrorCode}",
                query.AssetId, result.Error.Code);
            return Result<MediaAssetDto>.Failure(result.Error);
        }

        MediaAsset asset = result.Value;
        return Result<MediaAssetDto>.Success(new MediaAssetDto(
            asset.Id,
            asset.FileName,
            asset.StorageUrl,
            asset.Type,
            asset.FileSizeBytes,
            asset.MimeType,
            asset.UploadedAt));
    }
}
