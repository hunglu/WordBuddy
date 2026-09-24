using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using WordBuddy.Content.Application.Abstractions;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Application.Features.PersonalVocabulary.Commands.DeletePersonalVocabularyWord;

public sealed class DeletePersonalVocabularyWordCommandHandler : ICommandHandler<DeletePersonalVocabularyWordCommand>
{
    private readonly IPersonalVocabularyWordRepository _repository;
    private readonly IValidator<DeletePersonalVocabularyWordCommand> _validator;
    private readonly ILogger<DeletePersonalVocabularyWordCommandHandler> _logger;

    public DeletePersonalVocabularyWordCommandHandler(
        IPersonalVocabularyWordRepository repository,
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

        Result<PersonalVocabularyWord> wordResult = await _repository.GetOwnedByIdAsync(command.WordId, command.RequestingUserId, ct);
        if (wordResult.IsFailure)
        {
            _logger.LogWarning(
                "DeletePersonalVocabularyWordCommand word not found or not owned: WordId={WordId}, RequestingUserId={RequestingUserId}",
                command.WordId, command.RequestingUserId);
            return Result.Failure(wordResult.Error);
        }

        Result deleteResult = await _repository.DeleteAsync(wordResult.Value, ct);
        if (deleteResult.IsFailure)
        {
            _logger.LogWarning(
                "DeletePersonalVocabularyWordCommand failed to delete: {ErrorCode} — {ErrorDescription}",
                deleteResult.Error.Code, deleteResult.Error.Description);
            return deleteResult;
        }

        _logger.LogInformation("DeletePersonalVocabularyWordCommand succeeded: WordId={WordId}", command.WordId);
        return Result.Success();
    }
}
