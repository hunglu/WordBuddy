using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using WordBuddy.Content.Application.Abstractions;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Application.Features.PersonalVocabulary.Commands.RequestShareVocabularyWord;

/// <summary>Submits a word from the caller's list for moderation. Only the author may do this —
/// an adopted or system word returns <c>PersonalVocabularyWord.InvalidShareRequest</c> (409), so
/// adopted words can't re-enter the pool as duplicates. Not-found when the caller has no link.</summary>
public sealed class RequestShareVocabularyWordCommandHandler : ICommandHandler<RequestShareVocabularyWordCommand>
{
    private readonly IVocabularyWordRepository _repository;
    private readonly IValidator<RequestShareVocabularyWordCommand> _validator;
    private readonly ILogger<RequestShareVocabularyWordCommandHandler> _logger;

    public RequestShareVocabularyWordCommandHandler(
        IVocabularyWordRepository repository,
        IValidator<RequestShareVocabularyWordCommand> validator,
        ILogger<RequestShareVocabularyWordCommandHandler> logger)
    {
        _repository = repository;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result> HandleAsync(RequestShareVocabularyWordCommand command, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "RequestShareVocabularyWordCommand started: WordId={WordId}, RequestingUserId={RequestingUserId}",
            command.WordId, command.RequestingUserId);

        ValidationResult validation = await _validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("RequestShareVocabularyWordCommand validation failed: {Errors}", validation.ToString());
            return Result.Failure(Error.Validation("RequestShareVocabularyWord.Validation", validation.ToString()));
        }

        Result<LearnerWord> linkResult = await _repository.GetLinkAsync(command.RequestingUserId, command.WordId, ct);
        if (linkResult.IsFailure)
        {
            _logger.LogWarning(
                "RequestShareVocabularyWordCommand word not in caller's list: WordId={WordId}, RequestingUserId={RequestingUserId}",
                command.WordId, command.RequestingUserId);
            return Result.Failure(linkResult.Error);
        }

        if (!linkResult.Value.IsAuthor)
        {
            _logger.LogWarning(
                "RequestShareVocabularyWordCommand caller is not the author: WordId={WordId}, RequestingUserId={RequestingUserId}",
                command.WordId, command.RequestingUserId);
            return Result.Failure(Error.Conflict(
                "PersonalVocabularyWord.InvalidShareRequest",
                $"Word {command.WordId} can only be submitted for review by its author."));
        }

        Result<Sense> wordResult = await _repository.GetByIdAsync(command.WordId, ct);
        if (wordResult.IsFailure)
        {
            _logger.LogWarning("RequestShareVocabularyWordCommand word not found: WordId={WordId}", command.WordId);
            return Result.Failure(wordResult.Error);
        }

        Result shareResult = wordResult.Value.RequestShare();
        if (shareResult.IsFailure)
        {
            _logger.LogWarning(
                "RequestShareVocabularyWordCommand invalid transition: {ErrorCode} — {ErrorDescription}",
                shareResult.Error.Code, shareResult.Error.Description);
            return shareResult;
        }

        Result updateResult = await _repository.UpdateAsync(wordResult.Value, ct);
        if (updateResult.IsFailure)
        {
            _logger.LogWarning(
                "RequestShareVocabularyWordCommand failed to persist: {ErrorCode} — {ErrorDescription}",
                updateResult.Error.Code, updateResult.Error.Description);
            return updateResult;
        }

        _logger.LogInformation("RequestShareVocabularyWordCommand succeeded: WordId={WordId}", command.WordId);
        return Result.Success();
    }
}
