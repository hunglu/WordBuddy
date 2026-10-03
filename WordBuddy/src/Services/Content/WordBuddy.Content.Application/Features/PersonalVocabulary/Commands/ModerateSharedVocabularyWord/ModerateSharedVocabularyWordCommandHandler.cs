using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using WordBuddy.Content.Application.Abstractions;
using WordBuddy.Content.Application.Caching;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Application.Features.PersonalVocabulary.Commands.ModerateSharedVocabularyWord;

public sealed class ModerateSharedVocabularyWordCommandHandler : ICommandHandler<ModerateSharedVocabularyWordCommand>
{
    private readonly IVocabularyWordRepository _repository;
    private readonly IValidator<ModerateSharedVocabularyWordCommand> _validator;
    private readonly IDistributedCache _cache;
    private readonly ILogger<ModerateSharedVocabularyWordCommandHandler> _logger;

    public ModerateSharedVocabularyWordCommandHandler(
        IVocabularyWordRepository repository,
        IValidator<ModerateSharedVocabularyWordCommand> validator,
        IDistributedCache cache,
        ILogger<ModerateSharedVocabularyWordCommandHandler> logger)
    {
        _repository = repository;
        _validator = validator;
        _cache = cache;
        _logger = logger;
    }

    public async Task<Result> HandleAsync(ModerateSharedVocabularyWordCommand command, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "ModerateSharedVocabularyWordCommand started: WordId={WordId}, Approve={Approve}, ModeratorUserId={ModeratorUserId}",
            command.WordId, command.Approve, command.ModeratorUserId);

        ValidationResult validation = await _validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("ModerateSharedVocabularyWordCommand validation failed: {Errors}", validation.ToString());
            return Result.Failure(Error.Validation("ModerateSharedVocabularyWord.Validation", validation.ToString()));
        }

        Result<VocabularyWord> wordResult = await _repository.GetByIdAsync(command.WordId, ct);
        if (wordResult.IsFailure)
        {
            _logger.LogWarning("ModerateSharedVocabularyWordCommand word not found: WordId={WordId}", command.WordId);
            return Result.Failure(wordResult.Error);
        }

        Result moderationResult = command.Approve
            ? wordResult.Value.Approve(command.VisibleToChildren, command.ModeratorUserId)
            : wordResult.Value.Reject(command.ModeratorUserId);

        if (moderationResult.IsFailure)
        {
            _logger.LogWarning(
                "ModerateSharedVocabularyWordCommand invalid transition: {ErrorCode} — {ErrorDescription}",
                moderationResult.Error.Code, moderationResult.Error.Description);
            return moderationResult;
        }

        Result updateResult = await _repository.UpdateAsync(wordResult.Value, ct);
        if (updateResult.IsFailure)
        {
            _logger.LogWarning(
                "ModerateSharedVocabularyWordCommand failed to persist: {ErrorCode} — {ErrorDescription}",
                updateResult.Error.Code, updateResult.Error.Description);
            return updateResult;
        }

        // Cache-aside invalidation — delete both childSafeOnly variants rather than trying to
        // patch either in place.
        await _cache.RemoveAsync(SharedVocabularyCacheKeys.ChildSafe, ct);
        await _cache.RemoveAsync(SharedVocabularyCacheKeys.All, ct);

        _logger.LogInformation("ModerateSharedVocabularyWordCommand succeeded: WordId={WordId}, Approve={Approve}", command.WordId, command.Approve);
        return Result.Success();
    }
}
