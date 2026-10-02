using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using WordBuddy.Content.Application.Abstractions;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Application.Features.PersonalVocabulary.Commands.DeletePersonalVocabularyWord;

/// <summary>Removes a word from the caller's list by deleting their link. The word itself is
/// deleted only when the caller wrote it, it isn't shared, and nobody else links to it — so other
/// learners' lists and the shared pool stay intact. Not-found when the caller has no link.</summary>
public sealed class DeletePersonalVocabularyWordCommandHandler : ICommandHandler<DeletePersonalVocabularyWordCommand>
{
    private readonly IVocabularyWordRepository _repository;
    private readonly IValidator<DeletePersonalVocabularyWordCommand> _validator;
    private readonly ILogger<DeletePersonalVocabularyWordCommandHandler> _logger;

    public DeletePersonalVocabularyWordCommandHandler(
        IVocabularyWordRepository repository,
        IValidator<DeletePersonalVocabularyWordCommand> validator,
        ILogger<DeletePersonalVocabularyWordCommandHandler> logger)
    {
        _repository = repository;
        _validator = validator;
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

        Result<UserVocabularyWord> linkResult = await _repository.GetLinkAsync(command.RequestingUserId, command.WordId, ct);
        if (linkResult.IsFailure)
        {
            _logger.LogWarning(
                "DeletePersonalVocabularyWordCommand word not in caller's list: WordId={WordId}, RequestingUserId={RequestingUserId}",
                command.WordId, command.RequestingUserId);
            return Result.Failure(linkResult.Error);
        }

        UserVocabularyWord link = linkResult.Value;

        Result unlinkResult = await _repository.UnlinkAsync(link, ct);
        if (unlinkResult.IsFailure)
        {
            _logger.LogWarning(
                "DeletePersonalVocabularyWordCommand failed to unlink: {ErrorCode} — {ErrorDescription}",
                unlinkResult.Error.Code, unlinkResult.Error.Description);
            return unlinkResult;
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
