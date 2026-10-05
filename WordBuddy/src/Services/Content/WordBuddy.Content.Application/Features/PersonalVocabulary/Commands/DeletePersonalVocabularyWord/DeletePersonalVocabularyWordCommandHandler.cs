using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using WordBuddy.Content.Application.Abstractions;
using WordBuddy.Content.Application.Caching;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Application.Features.PersonalVocabulary.Commands.DeletePersonalVocabularyWord;

/// <summary>Removes a word from the caller's list by deleting their link. The word itself is
/// deleted only when the caller wrote it, it isn't shared, and nobody else links to it — so other
/// learners' lists and the shared pool stay intact. Not-found when the caller has no link.
/// <para>An author deleting a <c>Shared</c> or <c>PendingReview</c> word must confirm (409 otherwise).
/// Confirmed <c>Shared</c>: the word is handed over to the system owner and stays in the pool.
/// Confirmed <c>PendingReview</c>: the share request is cancelled, then the normal delete runs.</para></summary>
public sealed class DeletePersonalVocabularyWordCommandHandler : ICommandHandler<DeletePersonalVocabularyWordCommand>
{
    private readonly IVocabularyWordRepository _repository;
    private readonly IValidator<DeletePersonalVocabularyWordCommand> _validator;
    private readonly IDistributedCache _cache;
    private readonly ILogger<DeletePersonalVocabularyWordCommandHandler> _logger;

    public DeletePersonalVocabularyWordCommandHandler(
        IVocabularyWordRepository repository,
        IValidator<DeletePersonalVocabularyWordCommand> validator,
        IDistributedCache cache,
        ILogger<DeletePersonalVocabularyWordCommandHandler> logger)
    {
        _repository = repository;
        _validator = validator;
        _cache = cache;
        _logger = logger;
    }

    public async Task<Result> HandleAsync(DeletePersonalVocabularyWordCommand command, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "DeletePersonalVocabularyWordCommand started: WordId={WordId}, RequestingUserId={RequestingUserId}",
            command.WordId, command.RequestingUserId);

        ValidationResult validation = await _validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("DeletePersonalVocabularyWordCommand validation failed: {Errors}", validation.ToString());
            return Result.Failure(Error.Validation("DeletePersonalVocabularyWord.Validation", validation.ToString()));
        }

        Result<LearnerWord> linkResult = await _repository.GetLinkAsync(command.RequestingUserId, command.WordId, ct);
        if (linkResult.IsFailure)
        {
            _logger.LogWarning(
                "DeletePersonalVocabularyWordCommand word not in caller's list: WordId={WordId}, RequestingUserId={RequestingUserId}",
                command.WordId, command.RequestingUserId);
            return Result.Failure(linkResult.Error);
        }

        LearnerWord link = linkResult.Value;

        VocabularyShareStatus? authorShareStatus = null;
        if (link.IsAuthor)
        {
            Result<Sense> wordResult = await _repository.GetByIdAsync(command.WordId, ct);
            if (wordResult.IsFailure)
            {
                _logger.LogWarning("DeletePersonalVocabularyWordCommand word not found: WordId={WordId}", command.WordId);
                return Result.Failure(wordResult.Error);
            }

            Sense word = wordResult.Value;
            if (word.ShareStatus is VocabularyShareStatus.Shared or VocabularyShareStatus.PendingReview)
            {
                if (!command.Confirm)
                {
                    _logger.LogInformation(
                        "DeletePersonalVocabularyWordCommand needs confirmation: WordId={WordId}, ShareStatus={ShareStatus}",
                        command.WordId, word.ShareStatus);
                    return Result.Failure(Error.Conflict(
                        "PersonalVocabularyWord.DeleteConfirmationRequired",
                        $"Deleting a word with status {word.ShareStatus} needs confirmation. Retry with confirm=true."));
                }

                authorShareStatus = word.ShareStatus;

                // Tracked entity: the change is saved by UnlinkAsync's SaveChanges, together with the unlink.
                Result transitionResult = word.ShareStatus == VocabularyShareStatus.Shared
                    ? word.TransferToSystem()
                    : word.CancelShareRequest();

                if (transitionResult.IsFailure)
                {
                    _logger.LogWarning(
                        "DeletePersonalVocabularyWordCommand invalid transition: {ErrorCode} — {ErrorDescription}",
                        transitionResult.Error.Code, transitionResult.Error.Description);
                    return transitionResult;
                }
            }
        }

        Result unlinkResult = await _repository.UnlinkAsync(link, ct);
        if (unlinkResult.IsFailure)
        {
            _logger.LogWarning(
                "DeletePersonalVocabularyWordCommand failed to unlink: {ErrorCode} — {ErrorDescription}",
                unlinkResult.Error.Code, unlinkResult.Error.Description);
            return unlinkResult;
        }

        if (authorShareStatus == VocabularyShareStatus.Shared)
        {
            // The word stays in the pool under a new owner — drop both pool variants so IsMine
            // and ownership are fresh at once. The handover is already saved, so a cache failure
            // must not fail the request — the entries expire on their own.
            try
            {
                await _cache.RemoveAsync(SharedVocabularyCacheKeys.ChildSafe, ct);
                await _cache.RemoveAsync(SharedVocabularyCacheKeys.All, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(
                    ex,
                    "DeletePersonalVocabularyWordCommand could not clear shared pool cache after transfer: WordId={WordId}",
                    command.WordId);
            }

            _logger.LogInformation("DeletePersonalVocabularyWordCommand succeeded: WordId={WordId}, TransferredToSystem={TransferredToSystem}", command.WordId, true);
            return Result.Success();
        }

        bool wordDeleted = false;
        if (link.IsAuthor)
        {
            Result<bool> deleteResult = await _repository.DeleteIfOrphanedAsync(command.WordId, ct);
            if (deleteResult.IsFailure)
            {
                _logger.LogWarning(
                    "DeletePersonalVocabularyWordCommand failed to delete orphaned word: {ErrorCode} — {ErrorDescription}",
                    deleteResult.Error.Code, deleteResult.Error.Description);
                return Result.Failure(deleteResult.Error);
            }

            wordDeleted = deleteResult.Value;
        }

        _logger.LogInformation(
            "DeletePersonalVocabularyWordCommand succeeded: WordId={WordId}, WordDeleted={WordDeleted}",
            command.WordId, wordDeleted);
        return Result.Success();
    }
}
