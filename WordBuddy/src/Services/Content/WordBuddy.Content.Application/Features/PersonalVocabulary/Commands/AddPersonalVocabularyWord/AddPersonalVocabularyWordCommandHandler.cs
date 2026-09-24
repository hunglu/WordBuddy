using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using WordBuddy.Content.Application.Abstractions;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Application.Features.PersonalVocabulary.Commands.AddPersonalVocabularyWord;

public sealed class AddPersonalVocabularyWordCommandHandler : ICommandHandler<AddPersonalVocabularyWordCommand, Guid>
{
    private readonly IPersonalVocabularyWordRepository _repository;
    private readonly IValidator<AddPersonalVocabularyWordCommand> _validator;
    private readonly ILogger<AddPersonalVocabularyWordCommandHandler> _logger;

    public AddPersonalVocabularyWordCommandHandler(
        IPersonalVocabularyWordRepository repository,
        IValidator<AddPersonalVocabularyWordCommand> validator,
        ILogger<AddPersonalVocabularyWordCommandHandler> logger)
    {
        _repository = repository;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<Guid>> HandleAsync(AddPersonalVocabularyWordCommand command, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "AddPersonalVocabularyWordCommand started: OwnerUserId={OwnerUserId}, Word={Word}",
            command.OwnerUserId, command.Word);

        ValidationResult validation = await _validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("AddPersonalVocabularyWordCommand validation failed: {Errors}", validation.ToString());
            return Result.Failure<Guid>(Error.Validation("AddPersonalVocabularyWord.Validation", validation.ToString()));
        }

        Guid id = Guid.NewGuid();
        PersonalVocabularyWord word = new(
            id,
            command.OwnerUserId,
            command.OwnerAgeGroup,
            command.Word,
            command.Definition,
            command.Example);

        Result addResult = await _repository.AddAsync(word, ct);
        if (addResult.IsFailure)
        {
            _logger.LogWarning(
                "AddPersonalVocabularyWordCommand failed to persist word: {ErrorCode} — {ErrorDescription}",
                addResult.Error.Code, addResult.Error.Description);
            return Result.Failure<Guid>(addResult.Error);
        }

        _logger.LogInformation("AddPersonalVocabularyWordCommand succeeded: WordId={WordId}", id);
        return Result.Success(id);
    }
}
