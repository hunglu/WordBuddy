using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using WordBuddy.Content.Application.Caching;
using WordBuddy.Content.Application.DTOs;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Application.Abstractions;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Application.Features.LexemesAdmin.Commands.UploadLexemeAudio;

public sealed class UploadLexemeAudioCommandHandler : ICommandHandler<UploadLexemeAudioCommand, MediaAssetDto>
{
    private readonly ILexemeAdminRepository _repository;
    private readonly IFileStorageService _fileStorage;
    private readonly IDistributedCache _cache;
    private readonly IValidator<UploadLexemeAudioCommand> _validator;
    private readonly ILogger<UploadLexemeAudioCommandHandler> _logger;

    public UploadLexemeAudioCommandHandler(
        ILexemeAdminRepository repository,
        IFileStorageService fileStorage,
        IDistributedCache cache,
        IValidator<UploadLexemeAudioCommand> validator,
        ILogger<UploadLexemeAudioCommandHandler> logger)
    {
        _repository = repository;
        _fileStorage = fileStorage;
        _cache = cache;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<MediaAssetDto>> HandleAsync(UploadLexemeAudioCommand command, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "UploadLexemeAudioCommand started: LexemeId={LexemeId}, Locale={Locale}, Length={Length}",
            command.LexemeId, command.Locale, command.Length);

        ValidationResult validation = await _validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("UploadLexemeAudioCommand validation failed: {Errors}", validation.ToString());
            return Result.Failure<MediaAssetDto>(Error.Validation("UploadLexemeAudio.Validation", validation.ToString()));
        }

        Result<Lexeme> lexemeResult = await _repository.GetTrackedAsync(command.LexemeId, ct);
        if (lexemeResult.IsFailure)
        {
            _logger.LogWarning(
                "UploadLexemeAudioCommand lexeme lookup failed: {ErrorCode} — {ErrorDescription}",
                lexemeResult.Error.Code, lexemeResult.Error.Description);
            return Result.Failure<MediaAssetDto>(lexemeResult.Error);
        }

        Lexeme lexeme = lexemeResult.Value;

        // The fixed name overwrites the old file in place, so a replace leaves no orphan file.
        string path = await _fileStorage.SaveNamedAsync(
            command.Content, Lexeme.AudioFileName(lexeme.Id, command.Locale), UploadLexemeAudioCommand.AllowedContentType, ct);

        bool isUk = command.Locale == Lexeme.UkLocale;
        MediaAsset? current = isUk ? lexeme.UkAudio : lexeme.UsAudio;
        Guid? currentId = isUk ? lexeme.UkAudioAssetId : lexeme.UsAudioAssetId;

        if (current is not null && current.Url == path)
        {
            await LexemeCacheInvalidator.InvalidateAsync(_cache, lexeme.NormalizedLemma, ct);
            _logger.LogInformation(
                "UploadLexemeAudioCommand succeeded (file replaced in place): LexemeId={LexemeId}, MediaAssetId={MediaAssetId}",
                lexeme.Id, current.Id);
            return Result.Success(new MediaAssetDto(current.Id, current.Type, current.Url));
        }

        MediaAsset asset = new(Guid.NewGuid(), MediaAssetType.Audio, path);
        _repository.AddMediaAsset(asset);

        Result attach = lexeme.AttachAudio(command.Locale, asset.Id);
        if (attach.IsFailure)
        {
            return Result.Failure<MediaAssetDto>(attach.Error);
        }

        if (currentId is not null)
        {
            // The old asset points to a different path: unlink it and drop it when unused.
            Result<string?> replaced = await _repository.UnlinkAndDeleteAssetIfUnusedAsync(lexeme, currentId, ct);
            if (replaced.IsFailure)
            {
                _logger.LogWarning(
                    "UploadLexemeAudioCommand failed to persist: {ErrorCode} — {ErrorDescription}",
                    replaced.Error.Code, replaced.Error.Description);
                return Result.Failure<MediaAssetDto>(replaced.Error);
            }

            if (replaced.Value is not null)
            {
                await LexemeAudioCleanup.DeleteFileIfUnusedAsync(replaced.Value, currentId.Value, _repository, _fileStorage, _logger, ct);
            }
        }
        else
        {
            Result save = await _repository.SaveChangesAsync(ct);
            if (save.IsFailure)
            {
                _logger.LogWarning(
                    "UploadLexemeAudioCommand failed to persist: {ErrorCode} — {ErrorDescription}",
                    save.Error.Code, save.Error.Description);
                return Result.Failure<MediaAssetDto>(save.Error);
            }
        }

        await LexemeCacheInvalidator.InvalidateAsync(_cache, lexeme.NormalizedLemma, ct);

        _logger.LogInformation(
            "UploadLexemeAudioCommand succeeded: LexemeId={LexemeId}, MediaAssetId={MediaAssetId}", lexeme.Id, asset.Id);
        return Result.Success(new MediaAssetDto(asset.Id, asset.Type, asset.Url));
    }
}
