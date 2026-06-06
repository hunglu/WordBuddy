using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using WordBuddy.Application.DTOs;
using WordBuddy.Application.Interfaces;
using WordBuddy.Domain.Common;
using WordBuddy.Domain.Entities;
using WordBuddy.Domain.Enums;

namespace WordBuddy.Application.Features.Media.Commands.UploadMedia;

/// <summary>Handles <see cref="UploadMediaCommand"/> — saves the file then persists a MediaAsset record.</summary>
public sealed class UploadMediaCommandHandler
{
    private readonly IMediaAssetRepository _mediaAssetRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly IValidator<UploadMediaCommand> _validator;
    private readonly ILogger<UploadMediaCommandHandler> _logger;

    /// <summary>Initializes a new <see cref="UploadMediaCommandHandler"/>.</summary>
    public UploadMediaCommandHandler(
        IMediaAssetRepository mediaAssetRepository,
        IFileStorageService fileStorageService,
        IValidator<UploadMediaCommand> validator,
        ILogger<UploadMediaCommandHandler> logger)
    {
        _mediaAssetRepository = mediaAssetRepository;
        _fileStorageService = fileStorageService;
        _validator = validator;
        _logger = logger;
    }

    /// <summary>Validates, stores the file, and creates the MediaAsset record.</summary>
    public async Task<Result<MediaAssetDto>> HandleAsync(UploadMediaCommand command, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "UploadMediaCommand started: FileName={FileName}, ContentType={ContentType}, FileSizeBytes={FileSizeBytes}",
            command.FileName, command.ContentType, command.FileSizeBytes);

        ValidationResult validation = await _validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("UploadMediaCommand validation failed: {Errors}", validation.ToString());
            return Result<MediaAssetDto>.Failure(
                Error.Validation("UploadMedia.Validation", validation.ToString()));
        }

        string relativePath;
        try
        {
            relativePath = await _fileStorageService.SaveAsync(
                command.FileStream, command.FileName, command.ContentType, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "UploadMediaCommand: file storage failed: FileName={FileName}", command.FileName);
            return Result<MediaAssetDto>.Failure(
                Error.Failure("UploadMedia.StorageFailed", "Failed to save the file. Please try again."));
        }

        MediaAssetType assetType = MapContentTypeToAssetType(command.ContentType);
        MediaAsset asset = new(
            Guid.NewGuid(),
            command.FileName,
            relativePath,
            assetType,
            command.FileSizeBytes,
            command.ContentType,
            DateTime.UtcNow);

        Result addResult = await _mediaAssetRepository.AddAsync(asset, ct);
        if (addResult.IsFailure)
        {
            _logger.LogWarning(
                "UploadMediaCommand: database persist failed: {ErrorCode} — {ErrorDescription}",
                addResult.Error.Code, addResult.Error.Description);
            await TryDeleteFileAsync(relativePath, ct);
            return Result<MediaAssetDto>.Failure(addResult.Error);
        }

        _logger.LogInformation(
            "UploadMediaCommand succeeded: AssetId={AssetId}, StoragePath={StoragePath}",
            asset.Id, relativePath);

        return Result<MediaAssetDto>.Success(new MediaAssetDto(
            asset.Id,
            asset.FileName,
            asset.StorageUrl,
            asset.Type,
            asset.FileSizeBytes,
            asset.MimeType,
            asset.UploadedAt));
    }

    private async Task TryDeleteFileAsync(string relativePath, CancellationToken ct)
    {
        try
        {
            await _fileStorageService.DeleteAsync(relativePath, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "UploadMediaCommand: failed to clean up orphaned file after DB failure: Path={RelativePath}",
                relativePath);
        }
    }

    private static MediaAssetType MapContentTypeToAssetType(string contentType) =>
        contentType switch
        {
            "image/jpeg" or "image/png" => MediaAssetType.Image,
            "audio/mpeg" or "audio/wav" => MediaAssetType.Audio,
            "video/mp4"                 => MediaAssetType.Video,
            _                           => MediaAssetType.Image,
        };
}
