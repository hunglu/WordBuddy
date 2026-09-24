using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using WordBuddy.Content.Application.Abstractions;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Application.Features.PersonalVocabulary.Commands.RequestShareVocabularyWord;

public sealed class RequestShareVocabularyWordCommandHandler : ICommandHandler<RequestShareVocabularyWordCommand>
{
    private readonly IPersonalVocabularyWordRepository _repository;
    private readonly IValidator<RequestShareVocabularyWordCommand> _validator;
    private readonly ILogger<RequestShareVocabularyWordCommandHandler> _logger;

    public RequestShareVocabularyWordCommandHandler(
        IPersonalVocabularyWordRepository repository,
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

        Result<PersonalVocabularyWord> wordResult = await _repository.GetOwnedByIdAsync(command.WordId, command.RequestingUserId, ct);
        if (wordResult.IsFailure)
        {
            _logger.LogWarning(
                "RequestShareVocabularyWordCommand word not found or not owned: WordId={WordId}, RequestingUserId={RequestingUserId}",
                command.WordId, command.RequestingUserId);
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
