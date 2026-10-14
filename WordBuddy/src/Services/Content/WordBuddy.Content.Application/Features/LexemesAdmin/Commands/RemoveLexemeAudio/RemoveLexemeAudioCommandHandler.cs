using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using WordBuddy.Content.Application.Caching;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Application.Abstractions;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Application.Features.LexemesAdmin.Commands.RemoveLexemeAudio;

public sealed class RemoveLexemeAudioCommandHandler : ICommandHandler<RemoveLexemeAudioCommand>
{
    private readonly ILexemeAdminRepository _repository;
    private readonly IFileStorageService _fileStorage;
    private readonly IDistributedCache _cache;
    private readonly IValidator<RemoveLexemeAudioCommand> _validator;
    private readonly ILogger<RemoveLexemeAudioCommandHandler> _logger;

    public RemoveLexemeAudioCommandHandler(
        ILexemeAdminRepository repository,
        IFileStorageService fileStorage,
        IDistributedCache cache,
        IValidator<RemoveLexemeAudioCommand> validator,
        ILogger<RemoveLexemeAudioCommandHandler> logger)
    {
        _repository = repository;
        _fileStorage = fileStorage;
        _cache = cache;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result> HandleAsync(RemoveLexemeAudioCommand command, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "RemoveLexemeAudioCommand started: LexemeId={LexemeId}, Locale={Locale}", command.LexemeId, command.Locale);

        ValidationResult validation = await _validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("RemoveLexemeAudioCommand validation failed: {Errors}", validation.ToString());
            return Result.Failure(Error.Validation("RemoveLexemeAudio.Validation", validation.ToString()));
        }

        Result<Lexeme> lexemeResult = await _repository.GetTrackedAsync(command.LexemeId, ct);
        if (lexemeResult.IsFailure)
        {
            _logger.LogWarning(
                "RemoveLexemeAudioCommand lexeme lookup failed: {ErrorCode} — {ErrorDescription}",
                lexemeResult.Error.Code, lexemeResult.Error.Description);
            return Result.Failure(lexemeResult.Error);
        }

        Lexeme lexeme = lexemeResult.Value;

        Result<Guid?> detached = lexeme.DetachAudio(command.Locale);
        if (detached.IsFailure)
        {
            _logger.LogWarning(
                "RemoveLexemeAudioCommand rejected by lexeme: {ErrorCode} — {ErrorDescription}",
                detached.Error.Code, detached.Error.Description);
            return Result.Failure(detached.Error);
        }

        if (detached.Value is null)
        {
            _logger.LogInformation(
                "RemoveLexemeAudioCommand succeeded (nothing linked): LexemeId={LexemeId}, Locale={Locale}", lexeme.Id, command.Locale);
            return Result.Success();
        }

        Guid assetId = detached.Value.Value;

        // DB first (unlink + guarded row delete in one transaction), then the file.
        Result<string?> unlinked = await _repository.UnlinkAndDeleteAssetIfUnusedAsync(lexeme, assetId, ct);
        if (unlinked.IsFailure)
        {
            _logger.LogWarning(
                "RemoveLexemeAudioCommand failed to persist: {ErrorCode} — {ErrorDescription}",
                unlinked.Error.Code, unlinked.Error.Description);
            return Result.Failure(unlinked.Error);
        }

        if (unlinked.Value is not null)
        {
            await LexemeAudioCleanup.DeleteFileIfUnusedAsync(unlinked.Value, assetId, _repository, _fileStorage, _logger, ct);
        }

        await LexemeCacheInvalidator.InvalidateAsync(_cache, lexeme.NormalizedLemma, ct);

        _logger.LogInformation(
            "RemoveLexemeAudioCommand succeeded: LexemeId={LexemeId}, Locale={Locale}, AssetRowDeleted={AssetRowDeleted}",
            lexeme.Id, command.Locale, unlinked.Value is not null);
        return Result.Success();
    }
}
