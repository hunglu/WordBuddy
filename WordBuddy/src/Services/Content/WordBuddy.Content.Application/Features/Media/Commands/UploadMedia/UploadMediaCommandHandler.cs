using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using WordBuddy.Content.Application.Abstractions;
using WordBuddy.Content.Application.DTOs;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Application.Features.Media.Commands.UploadMedia;

public sealed class UploadMediaCommandHandler : ICommandHandler<UploadMediaCommand, MediaAssetDto>
{
    private readonly IFileStorageService _fileStorageService;
    private readonly IMediaAssetRepository _mediaAssetRepository;
    private readonly IValidator<UploadMediaCommand> _validator;
    private readonly ILogger<UploadMediaCommandHandler> _logger;

    public UploadMediaCommandHandler(
        IFileStorageService fileStorageService,
        IMediaAssetRepository mediaAssetRepository,
        IValidator<UploadMediaCommand> validator,
        ILogger<UploadMediaCommandHandler> logger)
    {
        _fileStorageService = fileStorageService;
        _mediaAssetRepository = mediaAssetRepository;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<MediaAssetDto>> HandleAsync(UploadMediaCommand command, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "UploadMediaCommand started: FileName={FileName}, ContentType={ContentType}",
            command.FileName, command.ContentType);

        ValidationResult validation = await _validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("UploadMediaCommand validation failed: {Errors}", validation.ToString());
            return Result.Failure<MediaAssetDto>(Error.Validation("UploadMedia.Validation", validation.ToString()));
        }

        string relativePath = await _fileStorageService.SaveAsync(command.Content, command.FileName, command.ContentType, ct);
        MediaAssetType type = ToMediaAssetType(command.ContentType);

        MediaAsset asset = new(Guid.NewGuid(), type, relativePath);

        Result addResult = await _mediaAssetRepository.AddAsync(asset, ct);
        if (addResult.IsFailure)
        {
            _logger.LogWarning(
                "UploadMediaCommand failed to persist media asset: {ErrorCode} — {ErrorDescription}",
                addResult.Error.Code, addResult.Error.Description);
            return Result.Failure<MediaAssetDto>(addResult.Error);
        }

        _logger.LogInformation("UploadMediaCommand succeeded: MediaAssetId={MediaAssetId}", asset.Id);
        return Result.Success(new MediaAssetDto(asset.Id, asset.Type, asset.Url));
    }

    private static MediaAssetType ToMediaAssetType(string contentType) => contentType switch
    {
        "image/jpeg" or "image/png" => MediaAssetType.Image,
        "audio/mpeg" or "audio/wav" => MediaAssetType.Audio,
        "video/mp4" => MediaAssetType.Video,
        _ => MediaAssetType.Text,
    };
}
